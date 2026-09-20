using System;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GreenhouseApp.Models;
using Serilog;

namespace GreenhouseApp.Services;

public class SerialClient : IDisposable
{
    private SerialPort? _port;
    private Telemetry? _lastTelemetry;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;

    public bool IsOpen => _port?.IsOpen == true;
    public string PortName => _port?.PortName ?? "(none)";

    public SerialClient(string portName, int baudRate = 115200)
    {
        try
        {
            _port = new SerialPort(portName, baudRate)
            {
                ReadTimeout = 1500,
                NewLine = "\n",
                DtrEnable = false,
                RtsEnable = false
            };
            _port.Open();

            Log.Information("Serial opened: {Port} @ {Baud}", portName, baudRate);

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReadLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open serial port {Port}", portName);
            _port = null;
        }
    }

    private void ReadLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _port?.IsOpen == true)
        {
            try
            {
                var line = _port.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith("{") && line.EndsWith("}"))
                {
                    try
                    {
                        var t = JsonSerializer.Deserialize<Telemetry>(line);
                        if (t == null) continue;
                        t.Timestamp = DateTime.UtcNow;
                        lock (_lock) _lastTelemetry = t;
                    }
                    catch (JsonException)
                    {
                        Log.Debug("Ignored non-JSON or partial serial line: {Line}", line);
                    }
                }
            }
            catch (TimeoutException) { /* нормально, ждём дальше */ }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
            {
                // Порт закрыт/освобождён — выход из цикла, чтобы не спамить предупреждениями
                return;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Serial read error");
                Thread.Sleep(200);
            }
        }
    }

    /// <summary>
    /// Возвращает последнее принятое измерение или null.
    /// </summary>
    public Telemetry? GetLatest()
    {
        lock (_lock)
        {
            if (_lastTelemetry == null) return null;

            // Помечаем как свежие, если не старше 10 секунд
            _lastTelemetry.IsFresh = (DateTime.UtcNow - _lastTelemetry.Timestamp).TotalSeconds < 10;
            _lastTelemetry.AgeSeconds = (DateTime.UtcNow - _lastTelemetry.Timestamp).TotalSeconds;

            return _lastTelemetry;
        }
    }

    /// <summary>
    /// Отправляет команду в ESP32 через Serial (например, "pump_on\n").
    /// </summary>
    public void SendCommand(string command)
    {
        if (_port?.IsOpen == true)
        {
            _port.WriteLine(command);
            Log.Information("Serial sent: {Cmd}", command);
        }
    }

    /// <summary>
    /// Возвращает список доступных последовательных портов,
    /// отфильтрованных под USB-Serial (ESP32).
    /// </summary>
    public static string[] GetAvailablePorts()
    {
        var all = SerialPort.GetPortNames().OrderBy(p => p).ToArray();

        Log.Information("Serial: all ports on {OS}: [{Ports}]",
            RuntimeInformation.OSDescription,
            string.Join(", ", all));

        // Определяем ОС
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: ESP32 = "COMx"
            return all
                .Where(p => p.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: ESP32 = /dev/cu.usbserial-* или /dev/cu.SLAB_USBtoUART
            return all
                .Where(p =>
                    p.Contains("cu.usbserial",   StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("cu.SLAB",        StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("cu.wchusbserial",StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("cu.usbmodem",    StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux: ESP32 = /dev/ttyUSB0 или /dev/ttyACM0
            return all
                .Where(p =>
                    p.Contains("ttyUSB", StringComparison.OrdinalIgnoreCase) ||
                    p.Contains("ttyACM", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        // Неизвестная ОС — вернуть всё
        return all;
    }
    
    /// <summary>
    /// Пробует каждый порт и ищет тот, где ESP32 отвечает "pong" на "ping".
    /// </summary>
    public static string? FindEsp32Port(int baudRate = 115200, int timeoutMs = 3000)
    {
        var candidates = GetAvailablePorts();
        Log.Information("Handshake: probing {Count} ports", candidates.Length);

        foreach (var portName in candidates)
        {
            if (portName.Equals("COM1", StringComparison.OrdinalIgnoreCase) ||
                portName.Equals("COM2", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Skipping system port {Port}", portName);
                continue;
            }

            try
            {
                using var port = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 300,
                    WriteTimeout = 1000,
                    NewLine = "\n",
                    DtrEnable = false,      // ← НЕ сбрасывать ESP32
                    RtsEnable = false       // ← НЕ сбрасывать ESP32
                };

                port.Open();
                Log.Debug("Port {Port} opened, waiting for ESP32 to be ready...", portName);
                Thread.Sleep(2000); 
                port.DiscardInBuffer();

                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                var found = false;

                while (DateTime.UtcNow < deadline && !found)
                {
                    try { port.WriteLine("ping"); } catch { }
                    
                    var readDeadline = DateTime.UtcNow.AddMilliseconds(400);
                    while (DateTime.UtcNow < readDeadline)
                    {
                        try
                        {
                            var line = port.ReadLine()?.Trim();
                            if (string.IsNullOrEmpty(line)) continue;
                            
                            // Пропускаем JSON и лог-строки
                            if (line.Contains("\"soil1_raw\"")) continue;
                            if (line.StartsWith("[NET]") || line.StartsWith("[AUTO]") ||
                                line.StartsWith("[SERIAL") || line.StartsWith("[MANUAL]")) continue;

                            if (line != "pong") continue;
                            found = true;
                            break;
                        }
                        catch (TimeoutException) { break; }
                    }
                }

                if (found)
                {
                    Log.Information("Handshake OK on {Port}", portName);
                    return portName;
                }

                port.Close();
                Log.Debug("Port {Port} did not respond to ping", portName);
            }
            catch (UnauthorizedAccessException)
            {
                Log.Warning("Port {Port} is busy (Arduino Serial Monitor открыт?)", portName);
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Port {Port} failed", portName);
            }
        }

        Log.Warning("Handshake: no ESP32 found on any port");
        return null;
    }
    
    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            if (_port?.IsOpen == true) _port.Close();
            _port?.Dispose();
        }
        catch { /* ignore */ }
    }
}