using SimpleServiceManager;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSystemd()
    .ConfigureAppConfiguration(conf =>
    {
        conf.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<IClientManager, ClientProcessManager>();
        services.AddHostedService<ServiceManager>();
    })
    .ConfigureLogging((hostingContext, logging) => logging.AddLog4Net("log4net.config"))
    .Build();

await host.RunAsync();

namespace SimpleServiceManager
{
    public static class CancellationHelper
    {
        public static Func<CancellationTokenSource> FromLifetime(IHostApplicationLifetime lifetime, CancellationToken cancel)
        {   // Link to all lifetime events
            return () => CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStarted, lifetime.ApplicationStopping, lifetime.ApplicationStopped, cancel);
        }
        public static CancellationTokenSource FromLifetimeStopping(IHostApplicationLifetime lifetime, CancellationToken cancel)
        {   // Link only to the lifetime stopping, stopped events 
            return CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, lifetime.ApplicationStopped, cancel);
        }

        public static async Task<bool> TryContinueOnCancel(TimeSpan waitPeriod, Func<CancellationTokenSource> cancel)
        {
            try
            {   // A cancellation may be due to the lifetime Started, Stopping, Stopped signals, or the local cancel
                using var cts = cancel();
                await Task.Delay(waitPeriod, cts.Token);
                return false; // If a Cancellation did not occur, return false
            }
            catch (TaskCanceledException)
            {   // Cancellation is expected
                return true;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }
    }
}