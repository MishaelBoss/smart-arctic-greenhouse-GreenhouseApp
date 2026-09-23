using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GreenhouseApp.ViewModels.Components;

namespace GreenhouseApp.Views.Components;

public partial class AddOrEditDeviceUserControlView : UserControl
{
    public AddOrEditDeviceUserControlView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not AddOrEditDeviceUserControlViewModel vm) return;

        if (e.Key == Key.Escape)
        {
            vm.Cancel();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && !vm.IsCreated)
        {
            _ = vm.Save();
            e.Handled = true;
        }
    }
}