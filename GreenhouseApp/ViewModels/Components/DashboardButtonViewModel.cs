using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GreenhouseApp.ViewModels.Components;

public partial class DashboardButtonViewModel : ViewModelBase
{
    public int DeviceId { get; }
    public string ConnectionType { get; }

    [ObservableProperty] public partial string ButtonText { get; set; }
    [ObservableProperty] public partial bool IsOnline { get; set; }
    [ObservableProperty] public partial string StatusColor { get; set; } = "#FF5252";

    public ICommand Command { get; }
    public string IconPath { get; }

    public DashboardButtonViewModel(int deviceId, string buttonText, ICommand command, string iconPath, string connectionType)
    {
        DeviceId = deviceId;
        ButtonText = buttonText;
        Command = command;
        IconPath = "/Assets/" + iconPath;
        ConnectionType = connectionType;
    }

    partial void OnIsOnlineChanged(bool value)
    {
        StatusColor = value ? "#4CAF50" : "#FF5252";
    }
}
