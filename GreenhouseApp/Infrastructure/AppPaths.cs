using System;
using System.Diagnostics;
using System.IO;

namespace GreenhouseApp.Infrastructure;

public abstract class AppPaths
{
    private static string LauncherDir => AppDomain.CurrentDomain.BaseDirectory;
    private static string AppName => Process.GetCurrentProcess().ProcessName;

    public static string UserDataDir
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return path;
        }
    }
}