using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using GreenhouseApp.Models;
using GreenhouseApp.Services;
using GreenhouseApp.ViewModels.Components;
using GreenhouseApp.ViewModels.Pages;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SkiaSharp;

namespace GreenhouseApp.ViewModels;

public partial class MainViewModel : ViewModelBase, IRecipient<OpenDetailPageMessage>, IRecipient<OpenGuideAddingGreenhouses>
{
    [ObservableProperty] public partial ViewModelBase? CurrentPage { get; set; }

    private readonly Lazy<DetailUserControlViewModel> _detail;
    private readonly Lazy<GuideAddingGreenhousesAddniUserControlViewModel> _guideAddingGreenhouses;

    public LeftBoardUserControlViewModel LeftBoardUserControlViewModel { get; }

    public MainViewModel()
    {
        var sp = AppServices.Provider;

        _detail = new Lazy<DetailUserControlViewModel>(() => sp.GetRequiredService<DetailUserControlViewModel>());
        _guideAddingGreenhouses = new Lazy<GuideAddingGreenhousesAddniUserControlViewModel>(() =>
            sp.GetRequiredService<GuideAddingGreenhousesAddniUserControlViewModel>());

        LeftBoardUserControlViewModel = sp.GetRequiredService<LeftBoardUserControlViewModel>();

        CurrentPage = _guideAddingGreenhouses.Value;
        
        IsActive = true;
        
        // WeakReferenceMessenger.Default.Register(this);
        WeakReferenceMessenger.Default.Send(new PageChangedMessage(PageType.GuideAddingGreenhouses));
    }

    public void Receive(OpenGuideAddingGreenhouses message)
    {
        CurrentPage = _guideAddingGreenhouses.Value;
        WeakReferenceMessenger.Default.Send(new PageChangedMessage(PageType.GuideAddingGreenhouses));
    }

    public void Receive(OpenDetailPageMessage message)
    {
        Log.Information("Main: received OpenDetailPageMessage for device {Id}", message.DeviceId);
        CurrentPage = _detail.Value;
        WeakReferenceMessenger.Default.Send(new PageChangedMessage(PageType.Detail));
    }
    
    public void Dispose()
    {
        IsActive = false;
        
        DisposePage(_detail);

        if (LeftBoardUserControlViewModel is IDisposable rightBoardDisposable)
            rightBoardDisposable.Dispose();

        GC.SuppressFinalize(this);
    }

    private static void DisposePage<T>(Lazy<T> lazy) where T : class
    {
        if (lazy.IsValueCreated && lazy.Value is IDisposable disposable)
            disposable.Dispose();
    }
}