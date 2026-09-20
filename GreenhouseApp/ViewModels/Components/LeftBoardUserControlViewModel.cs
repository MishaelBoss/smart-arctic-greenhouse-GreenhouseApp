using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using GreenhouseApp.Services;
using GreenhouseApp.Views;
using Serilog;

namespace GreenhouseApp.ViewModels.Components;

public partial class LeftBoardUserControlViewModel : ViewModelBase
{
    private readonly ApiClient _api = new();
    
    [ObservableProperty] public partial ObservableCollection<DashboardButtonViewModel> Buttons { get; set; } = [];
    [ObservableProperty] public partial int SelectedDeviceId { get; set; } = -1;
    
    public LeftBoardUserControlViewModel() 
    {
        Log.Information("Starting dashboard buttons initialization.");

        IsActive = true;
        
        _ = LoadDevicesAsync();
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
                
                var btn = new DashboardButtonViewModel(
                    buttonText: capturedName,
                    command: new AsyncRelayCommand(() =>
                        SelectDeviceAsync(capturedId, capturedName, device.ConnectionType ?? "wifi")),
                    iconPath: device.ConnectionType?.ToLower() == "usb" ? "usb.svg" : "wifi.svg"
                );

                Buttons.Add(btn);
            }

            Log.Information("LeftBoard: loaded {Count} devices", Buttons.Count);

            // if (Buttons.Count > 0)
            // {
            //     var first = devices[0];
            //     await SelectDeviceAsync(first.Id, first.Name);
            // }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "LeftBoard: error loading devices");
        }
    }

    private Task SelectDeviceAsync(int deviceId, string deviceName, string connectionType = "wifi")
    {
        SelectedDeviceId = deviceId;
        WeakReferenceMessenger.Default.Send(new OpenDetailPageMessage(deviceId, deviceName, connectionType));
        Log.Information("LeftBoard: device selected {Id} ({Name}) [{Type}]", deviceId, deviceName, connectionType);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public void RefreshDevices() => _ = LoadDevicesAsync();

    [RelayCommand]
    public void OpenGuideAddingGreenhouses() => WeakReferenceMessenger.Default.Send(new OpenGuideAddingGreenhouses());
    
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
        
        GC.SuppressFinalize(this);
    }
}