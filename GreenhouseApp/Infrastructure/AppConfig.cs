using System;
using System.IO;
using System.Text.Json;

namespace GreenhouseApp.Infrastructure;

public static class AppConfig
{
    private static readonly string ConfigPath = Path.Combine(AppPaths.UserDataDir, "config.json");
    private static readonly object Lock = new();

    public static string ServerUrl { get; private set; } = "http://localhost:8000";

    static AppConfig()
    {
        Load();
    }

    private static void Load()
    {
        try
        {
            if (!File.Exists(ConfigPath)) return;

            var config = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(ConfigPath));
            if (config?.ServerUrl is { Length: > 0 } url)
                ServerUrl = Normalize(url);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "AppConfig: failed to load {Path}", ConfigPath);
        }
    }

    public static void SetServerUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        ServerUrl = Normalize(url);

        lock (Lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                File.WriteAllText(ConfigPath,
                    JsonSerializer.Serialize(new ConfigFile { ServerUrl = ServerUrl },
                        new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "AppConfig: failed to save {Path}", ConfigPath);
            }
        }
    }

    private static string Normalize(string url) => url.Trim().TrimEnd('/');

    private sealed class ConfigFile
    {
        public string? ServerUrl { get; set; }
    }
}