using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using Serilog;

namespace GreenhouseApp.ViewModels.Components;

public partial class ViewImageUserControlViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty] private Bitmap? _lightboxImage;
    [ObservableProperty] private string _lightboxTitle;
    [ObservableProperty] private double _zoomScale;

    private readonly double _fitZoom;

    public string ZoomPercentText => $"{(int)Math.Round(ZoomScale * 100)}%";

    partial void OnZoomScaleChanged(double value)
    {
        OnPropertyChanged(nameof(ZoomPercentText));
    }

    public ViewImageUserControlViewModel(Bitmap lightboxImage, string lightboxTitle, double fitZoom)
    {
        Log.Information("Open image {@LightboxImage}, title: {LightboxTitle}, size: {FitZoom}", lightboxImage, lightboxTitle, fitZoom);

        _lightboxTitle = "";
        _lightboxImage = lightboxImage;
        _lightboxTitle = lightboxTitle;
        _fitZoom = fitZoom;
        _zoomScale = fitZoom;

        IsActive = true;
    }

    [RelayCommand]
    public void CloseLightbox()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseViewImageUserControlMessage());
    }

    [RelayCommand]
    public void ZoomIn()
    {
        ZoomScale = Math.Min(3.0, Math.Round(ZoomScale + 0.15, 2));
    }

    [RelayCommand]
    public void ZoomOut()
    {
        ZoomScale = Math.Max(0.3, Math.Round(ZoomScale - 0.15, 2));
    }

    [RelayCommand]
    public void ResetZoom()
    {
        ZoomScale = 1.0;
    }

    [RelayCommand]
    public void FitToWindow()
    {
        ZoomScale = _fitZoom;
    }

    public void Dispose()
    {
        IsActive = false;
        LightboxImage?.Dispose();
        LightboxImage = null;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        GC.SuppressFinalize(this);
    }
}