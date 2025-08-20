using System.Diagnostics.CodeAnalysis;

namespace SimpleServiceManager;

public interface IClientManager
{
    bool TryInitialiseClient([NotNullWhen(true)] out IClient? client);
}

public interface IClient
{
    void Start();
    bool IsRunning();
    bool Stop();
}

public class ServiceManager(ILogger<ServiceManager> logger, IConfiguration config, IClientManager clientManager) : BackgroundService
{
    public static readonly CancellationTokenSource MyToken = new();    
    
    // Executes the service asynchronously, starting a process defined by the file path obtained from GetPath. 
    // The process is monitored by checking if it is running every second. 
    // If the process stops, the service is also stopped. 
    // Logs the start, running status, and stop of the service along with any errors encountered.
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        var restartAppAutomatically = bool.Parse(config.GetSection("Configs:RestartAppAutomatically").Value!);
        var restartDelay = int.Parse(config.GetSection("Configs:RestartDelay").Value!);
        
        if (!clientManager.TryInitialiseClient(out var client))
        {
            return;
        }
        var sleep = TimeSpan.FromSeconds(5);
        try
        {
            try
            {
                logger.LogInformation("Simple Service Manager started running at: {time}", DateTimeOffset.Now);
                while (!cancel.IsCancellationRequested)
                {
                    client.Start();

                    await Monitor(client, sleep, cancel);
                    
                    if (restartAppAutomatically)
                    {
                        sleep = TimeSpan.FromMilliseconds(restartDelay);
                        logger.LogInformation("Client process is not running, retrying");
                    }
                    else
                    {
                        await MyToken.CancelAsync();
                        logger.LogInformation("Client process stopped");
                        break; //"fileName" process stopped so service is also stopped
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            logger.LogInformation("Simple Service Manager killed at: {time}", DateTimeOffset.Now);
            if (client.Stop())
            {
                logger.LogInformation("Simple Service Manager killed the client process at: {time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in SimpleServiceManager");
            throw;
        }
    }

    private async Task Monitor(IClient client, TimeSpan sleep, CancellationToken cancel)
    {
        var isProcessRunning = false;
        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(sleep, cancel);
            if (!client.IsRunning()) return;
            if (isProcessRunning) continue;
            
            logger.LogInformation("Client process is running");
            isProcessRunning = true;
        }
    }
}