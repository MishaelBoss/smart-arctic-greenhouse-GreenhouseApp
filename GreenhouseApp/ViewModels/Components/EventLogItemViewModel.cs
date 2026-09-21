using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GreenhouseApp.ViewModels.Components;

public partial class EventLogItemViewModel : ObservableObject
{
    public string Timestamp { get; }
    public string Message { get; }
    public string Color { get; }

    public EventLogItemViewModel(string message, string color)
    {
        Timestamp = DateTime.Now.ToString("HH:mm:ss");
        Message = message;
        Color = color;
    }
}