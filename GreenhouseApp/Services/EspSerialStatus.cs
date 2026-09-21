using System;

namespace GreenhouseApp.Services;

/// <summary>
/// Следит за последней активностью USB-соединения с ESP32,
/// чтобы интерфейс показывал честный статус «онлайн».
/// </summary>
public static class EspSerialStatus
{
    private static readonly object Lock = new();
    private static DateTime? _lastActivityUtc;

    public static void MarkActivity()
    {
        lock (Lock) _lastActivityUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// true, если JSON от ESP32 по Serial приходил недавно (в пределах окна).
    /// </summary>
    public static bool IsConnectedRecent(TimeSpan window)
    {
        lock (Lock)
        {
            return _lastActivityUtc.HasValue &&
                   DateTime.UtcNow - _lastActivityUtc.Value < window;
        }
    }
}