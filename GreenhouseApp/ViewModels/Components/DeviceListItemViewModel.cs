using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GreenhouseApp.Models;
using TextCopy;

namespace GreenhouseApp.ViewModels.Components;

public partial class DeviceListItemViewModel : ViewModelBase
{
    public Device Device { get; }

    public int Id => Device.Id;

    [ObservableProperty] private string _name;
    [ObservableProperty] private string _connectionType;
    [ObservableProperty] private string _apiKey;
    [ObservableProperty] private DateTime? _lastSeen;
    [ObservableProperty] private bool _isConfirmingDelete;
    [ObservableProperty] private bool _isKeyCopied;

    public string IconPath => IsUsb ? "/Assets/usb.svg" : "/Assets/wifi.svg";
    public string TypeLabel => IsUsb ? "USB" : "Wi-Fi";

    public bool IsUsb => ConnectionType.Equals("usb", StringComparison.OrdinalIgnoreCase);

    public string ApiKeyShort => ApiKey.Length > 14 ? $"{ApiKey[..14]}…" : ApiKey;

    public string KeyDisplay => IsKeyCopied ? "Скопировано" : ApiKeyShort;

    public string LastSeenText => LastSeen?.ToString("dd.MM.yyyy HH:mm") ?? "—";

    [RelayCommand]
    private async Task CopyKey()
    {
        ClipboardService.SetText(ApiKey);
        IsKeyCopied = true;
        await Task.Delay(2000);
        IsKeyCopied = false;
    }

    partial void OnIsKeyCopiedChanged(bool value)
    {
        OnPropertyChanged(nameof(KeyDisplay));
    }

    public DeviceListItemViewModel(Device device)
    {
        Device = device;
        _name = device.Name;
        _connectionType = device.ConnectionType;
        _apiKey = device.ApiKey;
        _lastSeen = device.LastSeen;
    }

    partial void OnConnectionTypeChanged(string value)
    {
        OnPropertyChanged(nameof(IconPath));
        OnPropertyChanged(nameof(TypeLabel));
    }
}