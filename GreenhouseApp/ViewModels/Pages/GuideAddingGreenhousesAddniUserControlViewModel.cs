using System;
using CommunityToolkit.Mvvm.Messaging;
using Serilog;

namespace GreenhouseApp.ViewModels.Pages;

public class GuideAddingGreenhousesAddniUserControlViewModel : ViewModelBase
{
    public GuideAddingGreenhousesAddniUserControlViewModel()
    {
        Log.Information("Starting guide adding greenhouses initialization.");

        IsActive = true;
    }
    
    public void Dispose()
    {
        IsActive = false;
        
        WeakReferenceMessenger.Default.UnregisterAll(this);
        
        GC.SuppressFinalize(this);
    }
}