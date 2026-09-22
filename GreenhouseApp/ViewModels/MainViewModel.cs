using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GreenhouseApp.Messages;
using GreenhouseApp.Models;
using GreenhouseApp.ViewModels.Components;
using GreenhouseApp.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GreenhouseApp.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable,
    IRecipient<OpenDetailPageMessage>,
    IRecipient<OpenGuideAddingGreenhouses>,
    IRecipient<OpenOrCloseViewImageUserControlMessage>,
    IRecipient<OpenListDevicesMessage>,
    IRecipient<OpenAddOrEditDeviceMessage>,
    IRecipient<CloseAddOrEditDeviceMessage>
{
    [ObservableProperty] public partial ViewModelBase? CurrentPage { get; set; }
    [ObservableProperty] public partial ViewModelBase? TopOverlayContent { get; set; }

    private readonly Lazy<DetailUserControlViewModel> _detail;
    private readonly Lazy<GuideAddingGreenhousesUserControlViewModel> _guideAddingGreenhouses;
    private readonly Lazy<ListDeviceUserControlViewModel>  _listDevice;
    
    public LeftBoardUserControlViewModel LeftBoardUserControlViewModel { get; }

    public MainViewModel()
    {
        var sp = AppServices.Provider;

        _detail = new Lazy<DetailUserControlViewModel>(sp.GetRequiredService<DetailUserControlViewModel>);
        _guideAddingGreenhouses = new Lazy<GuideAddingGreenhousesUserControlViewModel>(sp.GetRequiredService<GuideAddingGreenhousesUserControlViewModel>);
        _listDevice = new Lazy<ListDeviceUserControlViewModel>(sp.GetRequiredService<ListDeviceUserControlViewModel>);

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
        Log.Information("Main: received OpenDetailPageMessage for device {Id} ({Name}) [{ConnectionType}]", message.DeviceId, message.DeviceName, message.ConnectionType);
        var detailVm = _detail.Value;
        detailVm.SetDevice(message.DeviceId, message.DeviceName, message.ConnectionType);
        CurrentPage = detailVm;
        WeakReferenceMessenger.Default.Send(new PageChangedMessage(PageType.Detail));
    }

    public void Receive(OpenListDevicesMessage message)
    {
        CurrentPage = _listDevice.Value;
        WeakReferenceMessenger.Default.Send(new PageChangedMessage(PageType.ListDevice));
    }

    public void Receive(OpenOrCloseViewImageUserControlMessage message)
    {
        if (TopOverlayContent is IDisposable oldDisposable)
        {
            TopOverlayContent = null;
            oldDisposable.Dispose();
        }

        if (message.LightboxImage is not null)
        {
            TopOverlayContent = new ViewImageUserControlViewModel(
                message.LightboxImage,
                message.LightboxTitle ?? "",
                message.ZoomScale ?? 1.0);
        }
    }

    public void Receive(OpenAddOrEditDeviceMessage message)
    {
        if (TopOverlayContent is IDisposable oldDisposable)
        {
            TopOverlayContent = null;
            oldDisposable.Dispose();
        }

        TopOverlayContent = new AddOrEditDeviceUserControlViewModel(message.Device);
    }

    public void Receive(CloseAddOrEditDeviceMessage message)
    {
        if (TopOverlayContent is IDisposable oldDisposable)
        {
            TopOverlayContent = null;
            oldDisposable.Dispose();
        }
    }

    public void Dispose()
    {
        IsActive = false;
        
        DisposePage(_detail);
        DisposePage(_guideAddingGreenhouses);
        DisposePage(_listDevice);

        if (TopOverlayContent is IDisposable overlayDisposable)
            overlayDisposable.Dispose();

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