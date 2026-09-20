using GreenhouseApp.Services.Interfaces;
using GreenhouseApp.ViewModels;

namespace GreenhouseApp.Models;

public class SettingsPageItem(string title, ViewModelBase viewModelBase) : ISettingsPageItem
{
    public string Title { get; set; } = title;
    public ViewModelBase ViewModel { get; set; } = viewModelBase;
}
