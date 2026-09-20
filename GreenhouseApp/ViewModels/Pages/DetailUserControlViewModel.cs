using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Infrastructure;
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
    [ObservableProperty] private double _sensor1Moisture;
    [ObservableProperty] private int    _sensor1Raw;
    [ObservableProperty] private double _sensor2Moisture;
    [ObservableProperty] private int    _sensor2Raw;
    [ObservableProperty] private double _temperature1;
    [ObservableProperty] private double _humidity1;
    [ObservableProperty] private double _temperature2;
    [ObservableProperty] private double _humidity2;
    [ObservableProperty] private string _status = "Ожидание данных...";
    [ObservableProperty] private string _connectionStatus = "● Подключение...";
    [ObservableProperty] private string _lastUpdate = "--:--:--";
    [ObservableProperty] private string _connectionColor = "#4CAF50";
    [ObservableProperty] private string _deviceName = "Теплица";
    [ObservableProperty] private bool   _isChartPaused;
    [ObservableProperty] private bool   _isChartResumed = true;

    [ObservableProperty] private bool _isSoilChart = true;
    [ObservableProperty] private bool _isTemperatureChart;
    [ObservableProperty] private bool _isHumidityChart;
    [ObservableProperty] private string _chartTitle = "ИСТОРИЯ ВЛАЖНОСТИ ПОЧВЫ";
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _xAxes = [];
    [ObservableProperty] private Axis[] _yAxes = [];

    private readonly ObservableCollection<DateTimePoint> _soil1Points = [];
    private readonly ObservableCollection<DateTimePoint> _soil2Points = [];
    private readonly ObservableCollection<DateTimePoint> _temperature1Points = [];
    private readonly ObservableCollection<DateTimePoint> _temperature2Points = [];
    private readonly ObservableCollection<DateTimePoint> _humidity1Points = [];
    private readonly ObservableCollection<DateTimePoint> _humidity2Points = [];

    private readonly ISeries[] _soilSeries;
    private readonly ISeries[] _temperatureSeries;
    private readonly ISeries[] _humiditySeries;
    private readonly Axis[] _soilYAxes;
    private readonly Axis[] _temperatureYAxes;
    private readonly Axis[] _humidityYAxes;

    public int DeviceId { get; set; } = 1;

    public DetailUserControlViewModel()
    {
        Log.Information("Starting main windows initialization.");

        IsActive = true;

        _soilSeries =
        [
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 1",
                Values = _soil1Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#4FC3F7"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 2",
                Values = _soil2Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#4CAF50"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            }
        ];

        _temperatureSeries =
        [
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 1 · Температура",
                Values = _temperature1Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#FFB74D"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 2 · Температура",
                Values = _temperature2Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#F06292"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            }
        ];

        _humiditySeries =
        [
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 1 · Влажность",
                Values = _humidity1Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#81D4FA"), 3),
                GeometrySize = 8,
                LineSmoothness = 0.5
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Датчик 2 · Влажность",
                Values = _humidity2Points,
                Fill = null,
                Stroke = new SolidColorPaint(SKColor.Parse("#AED581"), 3),
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

        _soilYAxes = CreateYAxes(max: 100, labeler: value => $"{value:F0}%");

        _temperatureYAxes = CreateYAxes(max: 50, labeler: value => $"{value:F0}°C");

        _humidityYAxes = CreateYAxes(max: 100, labeler: value => $"{value:F0}%");

        Series = _soilSeries;
        YAxes = _soilYAxes;

        _cts = UiTimerManager.UpdateUiAsync(RefreshAsync, TimeSpan.FromSeconds(2));
        
    }

    private static Axis[] CreateYAxes(double max, Func<double, string> labeler)
    {
        return
        [
            new Axis
            {
                MinLimit = 0,
                MaxLimit = max,
                ForceStepToMin = true,
                MinStep = 10,
                Labeler = labeler,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#7FB3D5")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1A3A5C")),
                TextSize = 11
            }
        ];
    }

    public void SetDevice(int deviceId, string deviceName, string connectionType = "wifi")
    {
        DeviceId = deviceId;
        DeviceName = deviceName;

        var newType = connectionType.ToLower();

        if (newType == "usb" && _currentConnectionType == "usb" && _serial is { IsOpen: true })
        {
            Log.Information("USB mode: reusing existing port {Port}", _serial.PortName);
        }
        else
        {
            _serial?.Dispose();
            _serial = null;
            _currentConnectionType = newType;

            if (_currentConnectionType == "usb")
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

        Sensor1Moisture = 0;
        Sensor1Raw = 0;
        Sensor2Moisture = 0;
        Sensor2Raw = 0;
        Temperature1 = 0;
        Humidity1 = 0;
        Temperature2 = 0;
        Humidity2 = 0;
        LastUpdate = "--:--:--";

        _soil1Points.Clear();
        _soil2Points.Clear();
        _temperature1Points.Clear();
        _temperature2Points.Clear();
        _humidity1Points.Clear();
        _humidity2Points.Clear();

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
            Status = (latest.Sensor1Moisture, latest.Sensor2Moisture) switch
            {
                ( < 30, < 70) => "⚠ Верхний сухой — полив включён",
                ( < 30, >= 70) => "💧 Вода дошла до корней",
                ( >= 60, _) => "✓ Влажность в норме",
                _ => "⏳ Ожидание"
            };
        }
        
        Sensor1Moisture = latest.Sensor1Moisture;
        Sensor1Raw = latest.Sensor1Raw;
        Sensor2Moisture = latest.Sensor2Moisture;
        Sensor2Raw = latest.Sensor2Raw;

        Temperature1 = latest.Temperature1 ?? 0;
        Humidity1 = latest.Humidity1 ?? 0;
        Temperature2 = latest.Temperature2 ?? 0;
        Humidity2 = latest.Humidity2 ?? 0;
        LastUpdate = DateTime.Now.ToString("HH:mm:ss");

        if (!IsChartPaused && latest.IsFresh)
        {
            var time = DateTime.Now;
            _soil1Points.Add(new DateTimePoint(time, latest.Sensor1Moisture));
            _soil2Points.Add(new DateTimePoint(time, latest.Sensor2Moisture));

            if (latest.Temperature1.HasValue)
                _temperature1Points.Add(new DateTimePoint(time, latest.Temperature1.Value));
            if (latest.Temperature2.HasValue)
                _temperature2Points.Add(new DateTimePoint(time, latest.Temperature2.Value));

            if (latest.Humidity1.HasValue)
                _humidity1Points.Add(new DateTimePoint(time, latest.Humidity1.Value));
            if (latest.Humidity2.HasValue)
                _humidity2Points.Add(new DateTimePoint(time, latest.Humidity2.Value));

            while (_soil1Points.Count > 60) _soil1Points.RemoveAt(0);
            while (_soil2Points.Count > 60) _soil2Points.RemoveAt(0);
            while (_temperature1Points.Count > 60) _temperature1Points.RemoveAt(0);
            while (_temperature2Points.Count > 60) _temperature2Points.RemoveAt(0);
            while (_humidity1Points.Count > 60) _humidity1Points.RemoveAt(0);
            while (_humidity2Points.Count > 60) _humidity2Points.RemoveAt(0);

            Log.Debug("Chart updated for {DeviceId}. S1: {Soil1:F1}%, S2: {Soil2:F1}%, Temp1: {T1:F1}°C, Hum1: {H1:F1}%, Points count: {Count}",
                DeviceId, latest.Sensor1Moisture, latest.Sensor2Moisture,
                latest.Temperature1 ?? 0, latest.Humidity1 ?? 0, _soil1Points.Count);
        }
    }

    private void ShowChart(string title, bool soil, bool temperature, bool humidity,
        ISeries[] series, Axis[] yAxes)
    {
        ChartTitle = title;
        IsSoilChart = soil;
        IsTemperatureChart = temperature;
        IsHumidityChart = humidity;
        Series = series;
        YAxes = yAxes;
        Log.Information("Chart switched to: {Title}", title);
    }

    [RelayCommand]
    private void ShowSoilChart() =>
        ShowChart("ИСТОРИЯ ВЛАЖНОСТИ ПОЧВЫ", true, false, false, _soilSeries, _soilYAxes);

    [RelayCommand]
    private void ShowTemperatureChart() =>
        ShowChart("ИСТОРИЯ ТЕМПЕРАТУРЫ", false, true, false, _temperatureSeries, _temperatureYAxes);

    [RelayCommand]
    private void ShowHumidityChart() =>
        ShowChart("ИСТОРИЯ ВЛАЖНОСТИ ВОЗДУХА", false, false, true, _humiditySeries, _humidityYAxes);

    [RelayCommand] private async Task RefreshChart() => await RefreshAsync();

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
        _temperature1Points.Clear();
        _temperature2Points.Clear();
        _humidity1Points.Clear();
        _humidity2Points.Clear();
        Log.Information("Chart cleared");
    }
    
    private async Task SendCommand(string action)
    {
        if (_currentConnectionType == "usb" && _serial != null)
            _serial.SendCommand(action);
        else
            await _api.SendCommandAsync(DeviceId, action);
        
        Log.Information("Device: {DeviceId} Command sent: {Action}", DeviceId, action);
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