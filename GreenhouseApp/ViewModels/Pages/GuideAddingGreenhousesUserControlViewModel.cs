using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using Serilog;

namespace GreenhouseApp.ViewModels.Pages;

public partial class GuideAddingGreenhousesUserControlViewModel : ViewModelBase, IDisposable
{
    public GuideAddingGreenhousesUserControlViewModel()
    {
        Log.Information("Starting guide adding greenhouses initialization");

        IsActive = true;
    }

    [RelayCommand]
    public void OpenImage(string imageName)
    {
        try
        {
            var uri = new Uri($"avares://GreenhouseApp/Assets/guide/{imageName}.png");
            using var stream = AssetLoader.Open(uri);

            var bitmap = new Bitmap(stream);

            var title = imageName switch
            {
                "arduino-ide" => "Arduino IDE: Настройки платы, COM-порта и прошивка ESP32",
                "scheme" => "Схема подключения датчиков и исполнителей к ESP32",
                "server-response" => "Регистрация устройства на сервере и получение api_key",
                "data-page" => "Экран теплицы: показания датчиков, графики и лог событий",
                _ => "Просмотр изображения"
            };

            var fitZoom = imageName switch
            {
                "arduino-ide" => 0.8,
                "scheme" => 0.75,
                "server-response" => 0.85,
                "data-page" => 0.65,
                _ => 1.0
            };

            WeakReferenceMessenger.Default.Send(new OpenOrCloseViewImageUserControlMessage(bitmap, title, fitZoom));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load guide image: {Name}", imageName);
        }
    }
    
    public void Dispose()
    {
        IsActive = false;
        
        WeakReferenceMessenger.Default.UnregisterAll(this);
        
        GC.SuppressFinalize(this);
    }
}