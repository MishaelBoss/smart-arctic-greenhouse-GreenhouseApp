using GreenhouseApp.Services;
using GreenhouseApp.ViewModels;
using GreenhouseApp.ViewModels.Components;
using GreenhouseApp.ViewModels.Pages;
using GreenhouseApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GreenhouseApp;

public static class AppServices
{
    private static ServiceProvider? _provider;

    public static ServiceProvider Provider =>
        _provider ??= Configure().BuildServiceProvider();

    private static ServiceCollection Configure()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<ApiClient>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<LeftBoardUserControlViewModel>();
        services.AddSingleton<DetailUserControlViewModel>();
        services.AddSingleton<GuideAddingGreenhousesUserControlViewModel>();
        services.AddSingleton<ListDeviceUserControlViewModel>();
        
        services.AddTransient<SettingsDialogWindow>();
        return services;
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}