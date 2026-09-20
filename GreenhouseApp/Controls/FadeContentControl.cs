using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;

namespace GreenhouseApp.Controls;

public class FadeContentControl : ContentControl
{
    private bool _first = true;

    private readonly DoubleTransition _fadeTransition = new()
    {
        Property = OpacityProperty,
        Duration = TimeSpan.FromSeconds(0.2)
    };

    public FadeContentControl()
    {
        Transitions = [];
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ContentProperty) return;
        if (_first)
        {
            _first = false;
            Opacity = 1;
            return;
        }

        var saved = Transitions;
        Transitions = null;
        Opacity = 0.0;
        Transitions = saved;

        if (Transitions is not null && !Transitions.Contains(_fadeTransition))
            Transitions.Add(_fadeTransition);

        Opacity = 1.0;
    }
}