using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GreenhouseApp.Infrastructure;
using GreenhouseApp.ViewModels;
using GreenhouseApp.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;
using Serilog.Debugging;

namespace GreenhouseApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetupSerilog();
        SetupGlobalExceptionHandlers();
        
        LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .AddDefaultMappers()
        );
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = AppServices.Get<MainViewModel>(),
            };
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () => new MainView { DataContext = new MainViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void SetupSerilog()
    {
        var now = DateTime.Now;
        var datePart = now.ToString("dd_MM_yyyy");

        var logPath = Path.Combine(
            AppPaths.UserDataDir,
            "logs",
            $"launcher-log-{datePart}.txt");
            
        SelfLog.Enable(msg => 
        {
            Debug.WriteLine(msg);
            Console.Error.WriteLine(msg);
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logPath,
                outputTemplate: "[{Timestamp:dd.MM.yyyy HH:mm:ss}][{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private void SetupGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled AppDomain exception. Terminating={IsTerminating}", args.IsTerminating);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) =>
            {
                Log.Information("Application exiting");

                if (desktop.MainWindow?.DataContext is IDisposable viewModel)
                    viewModel.Dispose();

                Log.CloseAndFlush();
            };
        }
    }
}