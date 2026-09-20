using GreenhouseApp.ViewModels;

namespace GreenhouseApp.Services.Interfaces;

internal interface ISettingsPageItem
{
    public string Title { get; set; }
    public ViewModelBase ViewModel { get; set; }
}
