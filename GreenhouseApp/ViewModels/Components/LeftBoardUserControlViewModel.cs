using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Infrastructure;
using GreenhouseApp.Messages;
using GreenhouseApp.Services;
using GreenhouseApp.Views;
using Serilog;

namespace GreenhouseApp.ViewModels.Components;

public partial class LeftBoardUserControlViewModel : ViewModelBase
{
    private readonly ApiClient _api = new();
    private CancellationTokenSource? _cts;
    
    [ObservableProperty] public partial ObservableCollection<DashboardButtonViewModel> Buttons { get; set; } = [];
    [ObservableProperty] public partial int SelectedDeviceId { get; set; } = -1;
    
    public LeftBoardUserControlViewModel() 
    {
        Log.Information("Starting dashboard buttons initialization.");

        IsActive = true;
        
        _ = LoadDevicesAsync();

        _cts = UiTimerManager.UpdateUiAsync(PollDevicesStatusAsync, TimeSpan.FromSeconds(2));
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            Buttons.Clear();

            var devices = await _api.GetDevicesAsync();

            if (devices.Count == 0)
            {
                Log.Warning("LeftBoard: devices not found on server");
                return;
            }

            foreach (var device in devices)
            {
                var capturedId = device.Id;
                var capturedName = device.Name;
                var connectionType = device.ConnectionType ?? "wifi";
                
                var btn = new DashboardButtonViewModel(
                    capturedId,
                    capturedName,
                    new AsyncRelayCommand(() => SelectDeviceAsync(capturedId, capturedName, connectionType)),
                    connectionType.Equals("usb", StringComparison.OrdinalIgnoreCase) ? "usb.svg" : "wifi.svg",
                    connectionType
                );

                if (SelectedDeviceId == capturedId)
                {
                    btn.IsSelected = true;
                }

                Buttons.Add(btn);
            }

            Log.Information("LeftBoard: loaded {Count} devices", Buttons.Count);
            await PollDevicesStatusAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LeftBoard: error loading devices");
        }
    }

    private async Task PollDevicesStatusAsync()
    {
        foreach (var btn in Buttons)
        {
            if (btn.ConnectionType.Equals("usb", StringComparison.OrdinalIgnoreCase))
            {
                var ports = SerialClient.GetAvailablePorts();
                btn.IsOnline = ports.Length > 0;
            }
            else
            {
                var latest = await _api.GetLatestAsync(btn.DeviceId);
                btn.IsOnline = latest is { IsFresh: true };
            }
        }
    }

    private Task SelectDeviceAsync(int deviceId, string deviceName, string connectionType = "wifi")
    {
        SelectedDeviceId = deviceId;
        foreach (var btn in Buttons)
        {
            btn.IsSelected = (btn.DeviceId == deviceId && btn.ConnectionType.Equals(connectionType, StringComparison.OrdinalIgnoreCase));
        }
        WeakReferenceMessenger.Default.Send(new OpenDetailPageMessage(deviceId, deviceName, connectionType));
        Log.Information("LeftBoard: device selected {Id} ({Name}) [{Type}]", deviceId, deviceName, connectionType);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void RefreshDevices() => _ = LoadDevicesAsync();

    [RelayCommand]
    public void OpenGuideAddingGreenhouses()
    {
        SelectedDeviceId = -1;
        foreach (var btn in Buttons)
        {
            btn.IsSelected = false;
        }
        WeakReferenceMessenger.Default.Send(new OpenGuideAddingGreenhouses());
    }
    
    [RelayCommand]
    public void OpenSettings()
    {
        var dialog = new SettingsDialogWindow();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        var mainWindow = desktop.MainWindow;

        if (mainWindow != null)
            dialog.ShowDialog(mainWindow);
    }
    
    public void Dispose()
    {
        IsActive = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
