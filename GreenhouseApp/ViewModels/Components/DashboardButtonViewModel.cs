using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace GreenhouseApp.ViewModels.Components;

public partial class DashboardButtonViewModel : ViewModelBase
{
    [ObservableProperty] public partial string ButtonText { get; set; }

    [ObservableProperty] public partial bool IsButtonVisible { get; set; } = true;

    public ICommand Command { get; }
    public string IconPath { get; }
    
    public DashboardButtonViewModel(string buttonText, ICommand command, string iconPath)
    {
        ButtonText = buttonText;
        Command = command;
        IconPath = "/Assets/" + iconPath;
    }
}
