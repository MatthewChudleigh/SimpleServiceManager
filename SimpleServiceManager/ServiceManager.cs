using System.Diagnostics;

namespace SimpleServiceManager;

public class ServiceManager(ILogger<ServiceManager> logger, IConfiguration config) : BackgroundService
{
    public static readonly CancellationTokenSource MyToken = new();    
    public IConfigurationRoot Configuration { get; set; } = (IConfigurationRoot)config;

    private string? GetAppParams()
    {
        var path = Configuration.GetSection("Configs:AppParams").Value;
        if (string.IsNullOrEmpty(path))
        {
            logger.LogInformation("no AppParams : {time}", DateTimeOffset.Now);
        }
        
        return path;
    }

    private string? GetPath()
    {
        var path = Configuration.GetSection("Configs:AppPath").Value;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            logger.LogInformation("Simple Service Manager Exception Wrong APP path : {time}", DateTimeOffset.Now);
            return null;
        }
        return Path.GetFullPath(path);
    }
    
    private static void StartProcess(string filePath, string? appParams)
    {
        var fileExtension = Path.GetExtension(filePath);
        if (fileExtension.Equals(".ps1", StringComparison.CurrentCultureIgnoreCase))
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-File \"{filePath}\" {appParams}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
        }
        else
        {
            if (!string.IsNullOrEmpty(appParams))
            {
                Process.Start(filePath, appParams);
            }
            else
            {
                Process.Start(filePath);
            }
        }
    }
    // Executes the service asynchronously, starting a process defined by the file path obtained from GetPath. 
    // The process is monitored by checking if it is running every second. 
    // If the process stops, the service is also stopped. 
    // Logs the start, running status, and stop of the service along with any errors encountered.
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
#if (DEBUG)
        System.Diagnostics.Debugger.Launch();
#endif
        var filePath = GetPath();
        var appParams = GetAppParams();
        //string fileName = GetFileName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var restartAppAutomatically = bool.Parse(Configuration.GetSection("Configs:RestartAppAutomatically").Value!);
        var restartDelay = int.Parse(Configuration.GetSection("Configs:RestartDelay").Value!);

        // Add null check for filePath and fileName
        if (filePath == null || fileName == null)
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
                    StartProcess(filePath, appParams);

                    await Monitor(fileName, sleep, cancel);
                    
                    if (restartAppAutomatically)
                    {
                        sleep = TimeSpan.FromMilliseconds(restartDelay);
                        logger.LogInformation("Process is not running, retrying");
                    }
                    else
                    {
                        await MyToken.CancelAsync();
                        logger.LogInformation("Process stopped");
                        break; //"fileName" process stopped so service is also stopped
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }

            logger.LogInformation("Simple Service Manager killed at: {time}", DateTimeOffset.Now);
            var service = Process.GetProcessesByName(fileName).FirstOrDefault();
            if (service != null)
            {
                service.Kill();//Kill "fileName" If service is stopping.
                logger.LogInformation("Simple Service Manager killed server at: {time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred in SimpleServiceManager");
            throw;
        }
    }

    private async Task Monitor(string fileName, TimeSpan sleep, CancellationToken cancel)
    {
        var isProcessRunning = false;
        while (!cancel.IsCancellationRequested)
        {
            await Task.Delay(sleep, cancel);
            var service = Process.GetProcessesByName(fileName).FirstOrDefault();
            if (service == null)
            {
                return;
            }

            if (isProcessRunning) continue;
            
            logger.LogInformation("Process is running");
            isProcessRunning = true;
        }
    }
}