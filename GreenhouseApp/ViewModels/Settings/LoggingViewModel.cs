using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using GreenhouseApp.Infrastructure;
using GreenhouseApp.Services.Interfaces;

namespace GreenhouseApp.ViewModels.Settings;

public partial class LoggingViewModel : ViewModelBase, ISettingsPage
{
    public bool HasChanges => false;
    
    public void Save()
    {
    }
    
    [RelayCommand]
    public void OpenLogsFolder()
    {
        var logsPath = Path.Combine(AppPaths.UserDataDir, "logs");

        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = logsPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }   
}