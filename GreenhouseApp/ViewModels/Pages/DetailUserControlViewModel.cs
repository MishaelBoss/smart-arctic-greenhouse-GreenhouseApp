using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Infrastructure;
using GreenhouseApp.Messages;
using GreenhouseApp.Models;
using GreenhouseApp.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Serilog;
using SkiaSharp;

namespace GreenhouseApp.ViewModels.Pages;

public partial class DetailUserControlViewModel : ViewModelBase
{
    private readonly ApiClient _api = new();
    private SerialClient? _serial;
    
    private string _currentConnectionType = "wifi";
    private bool _disposed;
    private readonly CancellationTokenSource? _cts;
    
    [ObservableProperty] private double _soil1Moisture;
    [ObservableProperty] private int    _soil1Raw;
    [ObservableProperty] private double _soil2Moisture;
    [ObservableProperty] private int    _soil2Raw;
    [ObservableProperty] private double _temperature;
    [ObservableProperty] private double _humidity;
    [ObservableProperty] private string _status = "Ожидание данных...";
    [ObservableProperty] private string _connectionStatus = "● Подключение...";
    [ObservableProperty] private string _lastUpdate = "--:--:--";
    [ObservableProperty] private string _connectionColor = "#4CAF50";
    [ObservableProperty] private string _deviceName = "Теплица";
    [ObservableProperty] private bool   _isChartPaused;
    [ObservableProperty] private bool   _isChartResumed = true;
    
    private readonly ObservableCollection<DateTimePoint> _soil1Points = [];
    private readonly ObservableCollection<DateTimePoint> _soil2Points = [];

    public ISeries[] Series { get; }
    public Axis[] XAxes { get; }
    public Axis[] YAxes { get; }

    public int DeviceId { get; set; } = 1;

    public DetailUserControlViewModel()
    {
        Log.Information("Starting main windows initialization.");

        IsActive = true;

        Series =
        [
            new LineSeries<DateTimePoint>
            {
                Name = "Верхний датчик",
                Values = _soil1Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#4FC3F7"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Нижний датчик",
                Values = _soil2Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#4CAF50"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            }
        ];
        
        XAxes =
        [
            new DateTimeAxis(TimeSpan.FromSeconds(30), date => date.ToString("HH:mm:ss"))
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#7FB3D5")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1A3A5C")),
                TextSize = 11,
                MinStep = TimeSpan.FromSeconds(5).Ticks
            }
        ];

        YAxes =
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 100,
                ForceStepToMin = true, 
                MinStep = 20, 
                Labeler = value => $"{value:F0}%",
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#7FB3D5")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1A3A5C")),
                TextSize = 11
            }
        ];
        
        _cts = UiTimerManager.UpdateUiAsync(RefreshAsync, TimeSpan.FromSeconds(2));
        
    }

    public void SetDevice(int deviceId, string deviceName, string connectionType = "wifi")
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        _currentConnectionType = connectionType.ToLower();

        _serial?.Dispose();
        _serial = null;

        if (_currentConnectionType == "usb")
        {
            if (_serial is { IsOpen: true })
            {
                Log.Information("USB mode: reusing existing port {Port}", _serial.PortName);
            }
            else
            {
                var portName = SerialClient.FindEsp32Port();
                if (portName != null)
                {
                    _serial = new SerialClient(portName);
                    Log.Information("USB mode: connected to {Port}", portName);
                }
                else
                {
                    Log.Warning("USB mode: ESP32 not found");
                    ConnectionStatus = "● USB: устройство не найдено";
                    ConnectionColor = "#FF5252";
                    Status = "Проверьте USB и закройте Serial Monitor";
                }
            }
        }
        else
        {
            _serial?.Dispose();
            _serial = null;
        }
        
        Soil1Moisture = 0;
        Soil1Raw = 0;
        Soil2Moisture = 0;
        Soil2Raw = 0;
        Temperature = 0;
        Humidity = 0;
        LastUpdate = "--:--:--";

        _soil1Points.Clear();
        _soil2Points.Clear();

        ConnectionStatus = "● Подключение...";
        ConnectionColor = "#7FB3D5";
        Status = $"Загрузка {deviceName}...";

        _ = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        Telemetry? latest;

        if (_currentConnectionType == "usb")
        {
            if (_serial != null)
            {
                latest = _serial.GetLatest();
                if (latest == null)
                {
                    ConnectionStatus = "● USB: ожидание данных";
                    ConnectionColor = "#FFC107";
                    Status = "Ожидание JSON от ESP32 по USB...";
                    Log.Debug("USB connection active, but waiting for JSON data from ESP32");
                    return;
                }
            }
            else
            {
                ConnectionStatus = "● USB: порт не найден";
                ConnectionColor = "#FF5252";
                Status = "Не удалось найти порт ESP32 (проверьте кабель и Serial Monitor)";
                Log.Warning("USB mode: ESP32 port not found");
                return;
            }
        }
        else
        {
            latest = await _api.GetLatestAsync(DeviceId);
            if (latest == null)
            {
                ConnectionStatus = "● Нет данных";
                ConnectionColor = "#FF5252"; 
                Status = "Ожидание первого измерения";
                
                Log.Warning("Fetch latest telemetry failed. DeviceId: {DeviceId}", DeviceId);
                return;
            }
        }
        
        if (!latest.IsFresh)
        {
            ConnectionStatus = $"● Offline ({latest.AgeSeconds:F0} сек)";
            ConnectionColor = "#FFC107";
            Status = "⚠ Устройство не отвечает — данные устарели";
            
            Log.Warning("Device is offline. Age: {AgeSeconds:F1}s. Latest update timestamp: {Timestamp}", 
                latest.AgeSeconds, 
                latest.Timestamp);
        }
        else
        {
            ConnectionStatus = "● Онлайн";
            ConnectionColor = "#4CAF50";
            Status = (latest.Soil1Moisture, latest.Soil2Moisture) switch
            {
                ( < 30, < 70) => "⚠ Верхний сухой — полив включён",
                ( < 30, >= 70) => "💧 Вода дошла до корней",
                ( >= 60, _) => "✓ Влажность в норме",
                _ => "⏳ Ожидание"
            };
        }
        
        Soil1Moisture = latest.Soil1Moisture;
        Soil1Raw = latest.Soil1Raw;
        Soil2Moisture = latest.Soil2Moisture;
        Soil2Raw = latest.Soil2Raw;
        Temperature = latest.Temperature ?? 0;
        Humidity = latest.Humidity ?? 0;
        LastUpdate = DateTime.Now.ToString("HH:mm:ss");

        if (!IsChartPaused)
        {
            var time = DateTime.Now;
            _soil1Points.Add(new DateTimePoint(time, latest.Soil1Moisture));
            _soil2Points.Add(new DateTimePoint(time, latest.Soil2Moisture));

            while (_soil1Points.Count > 60) _soil1Points.RemoveAt(0);
            while (_soil2Points.Count > 60) _soil2Points.RemoveAt(0);
            
            Log.Debug("Chart updated for {DeviceId}. S1: {Soil1:F1}%, S2: {Soil2:F1}%, Points count: {Count}", 
                DeviceId, latest.Soil1Moisture, latest.Soil2Moisture, _soil1Points.Count);
        }
    }
    
    [RelayCommand]
    private void TogglePause()
    {
        IsChartPaused = !IsChartPaused;
        IsChartResumed = !IsChartPaused;
        Log.Information("Chart paused: {Paused}", IsChartPaused);
    }

    [RelayCommand]
    private void ClearChart()
    {
        _soil1Points.Clear();
        _soil2Points.Clear();
        Log.Information("Chart cleared");
    }
    
    private async Task SendCommand(string action)
    {
        if (_currentConnectionType == "usb" && _serial != null)
            _serial.SendCommand(action);
        else
            await _api.SendCommandAsync(DeviceId, action);
        
        Log.Information("Device: {deviceId} Command sent: {action}", DeviceId, action);
    }
    
    [RelayCommand] private async Task PumpOn()  => await SendCommand("pump_on");
    [RelayCommand] private async Task PumpOff() => await SendCommand("pump_off");
    [RelayCommand] private async Task RoofOpen() => await SendCommand("roof_open");
    [RelayCommand] private async Task RoofClose() => await SendCommand("roof_close");
    [RelayCommand] private async Task LightOn() => await SendCommand("light_on");
    [RelayCommand] private async Task LightOff() => await SendCommand("light_off");
    
    public void Dispose()
    {
        if (_disposed)  return;
        _disposed = true;

        IsActive = false;

        _cts?.Cancel();
        _cts?.Dispose();
        _serial?.Dispose();
        _serial = null;

        WeakReferenceMessenger.Default.UnregisterAll(this);
        
        GC.SuppressFinalize(this);
    }
}