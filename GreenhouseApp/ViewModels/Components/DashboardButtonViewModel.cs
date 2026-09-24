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
    
    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial string ButtonBackground { get; set; } = "Transparent";
    [ObservableProperty] public partial string BorderColor { get; set; } = "Transparent";

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

    partial void OnIsSelectedChanged(bool value)
    {
        ButtonBackground = value ? "#1E3A8A" : "Transparent";
        BorderColor = value ? "#4FC3F7" : "Transparent";
    }
}
