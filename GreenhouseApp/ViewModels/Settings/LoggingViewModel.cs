using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using GreenhouseApp.Infrastructure;

namespace GreenhouseApp.ViewModels.Settings;

public partial class LoggingViewModel : ViewModelBase
{
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