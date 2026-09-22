using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using GreenhouseApp.Models;
using GreenhouseApp.Services;
using Serilog;
using TextCopy;

namespace GreenhouseApp.ViewModels.Components;

public partial class AddOrEditDeviceUserControlViewModel : ViewModelBase, IDisposable
{
    private readonly ApiClient _api = new();
    private readonly int? _deviceId;

    [ObservableProperty] private string _title;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private bool _isWifi = true;
    [ObservableProperty] private bool _isUsb;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private bool _isCreated;
    [ObservableProperty] private string _createdApiKey = "";
    [ObservableProperty] private bool _isKeyCopied;

    public bool IsEditMode => _deviceId is not null;
    public string ConnectionType => IsUsb ? "usb" : "wifi";

    public AddOrEditDeviceUserControlViewModel(Device? device)
    {
        IsActive = true;

        if (device is not null)
        {
            _deviceId = device.Id;
            _title = "Изменить устройство";
            _name = device.Name;
            _isUsb = device.ConnectionType.Equals("usb", StringComparison.OrdinalIgnoreCase);
            _isWifi = !_isUsb;
        }
        else
        {
            _title = "Добавить устройство";
        }
    }

    [RelayCommand]
    public void SelectWifi()
    {
        IsWifi = true;
        IsUsb = false;
    }

    [RelayCommand]
    public void SelectUsb()
    {
        IsWifi = false;
        IsUsb = true;
    }

    [RelayCommand]
    public async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Введите название устройства";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (_deviceId is null)
            {
                var created = await _api.CreateDeviceAsync(Name.Trim(), ConnectionType);

                if (created is null)
                {
                    ErrorMessage = "Сервер не ответил — проверьте, что backend запущен";
                    return;
                }

                Log.Information("Device created: {Id} ({Name}) [{Type}]", created.Id, created.Name, created.ConnectionType);

                CreatedApiKey = created.ApiKey;
                IsCreated = true;

                WeakReferenceMessenger.Default.Send(new DevicesChangedMessage());
            }
            else
            {
                var updated = await _api.UpdateDeviceAsync(_deviceId.Value, Name.Trim(), ConnectionType);

                if (updated is null)
                {
                    ErrorMessage = "Сервер не ответил — проверьте, что backend запущен";
                    return;
                }

                Log.Information("Device updated: {Id} ({Name}) [{Type}]", updated.Id, updated.Name, updated.ConnectionType);

                WeakReferenceMessenger.Default.Send(new DevicesChangedMessage());
                WeakReferenceMessenger.Default.Send(new CloseAddOrEditDeviceMessage());
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyCreatedKey()
    {
        ClipboardService.SetText(CreatedApiKey);
        IsKeyCopied = true;
        await Task.Delay(2000);
        IsKeyCopied = false;
    }

    [RelayCommand]
    public void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new CloseAddOrEditDeviceMessage());
    }

    public void Dispose()
    {
        IsActive = false;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        GC.SuppressFinalize(this);
    }
}