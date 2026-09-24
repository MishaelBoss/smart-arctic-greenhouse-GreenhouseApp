using System;
using CommunityToolkit.Mvvm.ComponentModel;
using GreenhouseApp.Infrastructure;
using GreenhouseApp.Services.Interfaces;

namespace GreenhouseApp.ViewModels.Settings;

public partial class ServerViewModel : ViewModelBase, ISettingsPage
{
    [ObservableProperty] private string _serverUrl = AppConfig.ServerUrl;

    public bool HasChanges =>
        !string.Equals(Normalize(ServerUrl), AppConfig.ServerUrl, StringComparison.OrdinalIgnoreCase);

    partial void OnServerUrlChanged(string value) => OnPropertyChanged(nameof(HasChanges));

    public void Save()
    {
        var url = Normalize(ServerUrl);
        if (url.Length == 0) return;

        AppConfig.SetServerUrl(url);
        ServerUrl = AppConfig.ServerUrl;
        OnPropertyChanged(nameof(HasChanges));
    }

    private static string Normalize(string? url) => (url ?? string.Empty).Trim().TrimEnd('/');
}