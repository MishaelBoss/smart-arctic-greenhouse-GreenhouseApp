using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GreenhouseApp.ViewModels.Components;

namespace GreenhouseApp.Views.Components;

public partial class ViewImageUserControlView : UserControl
{
    public ViewImageUserControlView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape || DataContext is not ViewImageUserControlViewModel vm) return;
        
        vm.CloseLightbox();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not ViewImageUserControlViewModel vm) return;
        
        switch (e.Delta.Y)
        {
            case > 0:
                vm.ZoomIn();
                break;
            case < 0:
                vm.ZoomOut();
                break;
        }

        e.Handled = true;
    }
}