using Avalonia.Controls;
using GreenhouseApp.ViewModels;

namespace GreenhouseApp.Views;

public partial class SettingsDialogWindow : Window
{
    public SettingsDialogWindow()
    {
        InitializeComponent();
        DataContext = new SettingsDialogWindowViewModel();
    }
}