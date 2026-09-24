using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using GreenhouseApp.Services;
using GreenhouseApp.ViewModels.Components;
using Serilog;

namespace GreenhouseApp.ViewModels.Pages;

public partial class ListDeviceUserControlViewModel : ViewModelBase, IDisposable, IRecipient<DevicesChangedMessage>
{
    private readonly ApiClient _api = new();

    [ObservableProperty] private ObservableCollection<DeviceListItemViewModel> _devices = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public ListDeviceUserControlViewModel()
    {
        Log.Information("Starting list device initialization");

        IsActive = true;

        _ = LoadDevicesAsync();
    }

    [RelayCommand]
    public async Task Refresh()
    {
        await LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var devices = await _api.GetDevicesAsync();

            Devices.Clear();

            foreach (var device in devices)
                Devices.Add(new DeviceListItemViewModel(device));

            Log.Information("ListDevice: loaded {Count} devices", Devices.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Не удалось загрузить список устройств";
            Log.Error(ex, "ListDevice: error loading devices");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void AddDevice()
    {
        WeakReferenceMessenger.Default.Send(new OpenAddOrEditDeviceMessage());
    }

    [RelayCommand]
    public void EditDevice(DeviceListItemViewModel? item)
    {
        if (item is null) return;
        WeakReferenceMessenger.Default.Send(new OpenAddOrEditDeviceMessage(item.Device));
    }

    [RelayCommand]
    public async Task DeleteDevice(DeviceListItemViewModel? item)
    {
        if (item is null) return;

        if (!item.IsConfirmingDelete)
        {
            item.IsConfirmingDelete = true;
            return;
        }

        ErrorMessage = null;

        var ok = await _api.DeleteDeviceAsync(item.Id);

        if (!ok)
        {
            item.IsConfirmingDelete = false;
            ErrorMessage = "Не удалось удалить устройство";
            Log.Warning("ListDevice: failed to delete device {Id}", item.Id);
            return;
        }

        Devices.Remove(item);
        Log.Information("ListDevice: deleted device {Id} ({Name})", item.Id, item.Name);

        WeakReferenceMessenger.Default.Send(new DevicesChangedMessage());
    }

    public void Receive(DevicesChangedMessage message)
    {
        _ = LoadDevicesAsync();
    }

    public void Dispose()
    {
        IsActive = false;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}