using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;

namespace GreenhouseApp.Infrastructure;

public abstract class UiTimerManager
{
    public static CancellationTokenSource UpdateUiAsync(Func<Task> asyncAction, TimeSpan interval)
    {
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(interval);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(token);

                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            await asyncAction();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Ошибка в UI-задаче");
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка цикла таймера");
                }
            }
        }, token);
        
        return cts;
    }

    public static CancellationTokenSource UpdateUi(Action syncAction, TimeSpan interval)
    {
        return UpdateUiAsync(() =>
        {
            syncAction();
            return Task.CompletedTask;
        }, interval);
    }
}
