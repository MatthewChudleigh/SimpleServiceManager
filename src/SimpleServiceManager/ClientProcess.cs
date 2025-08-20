using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SimpleServiceManager;

public class ClientProcessManager(ILogger<ClientProcessManager> logger, IConfiguration config) : IClientManager
{
    private string? GetAppParams()
    {
        var path = config.GetSection("Configs:AppParams").Value;
        if (string.IsNullOrEmpty(path))
        {
            logger.LogInformation("Configs:AppParams is empty");
        }
        
        return path;
    }

    private string? GetPath()
    {
        var path = config.GetSection("Configs:AppPath").Value;
        if (string.IsNullOrEmpty(path))
        {
            logger.LogError("Configs:AppPath is empty");
            return null;
        }
        return Path.GetFullPath(path);
    }

    public bool TryInitialiseClient([NotNullWhen(true)] out IClient? client)
    {
        client = null;
        var filePath = GetPath();
        var appParams = GetAppParams();
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // Add null check for filePath and fileName
        if (filePath == null || fileName == null) return false;

        client = new ClientProcess(filePath, fileName, appParams);
        return true;
    }
}

public class ClientProcess(string filePath, string fileName, string? appParams) : IClient
{
    public void Start()
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

    public bool IsRunning()
    {
        var service = Process.GetProcessesByName(fileName).FirstOrDefault();
        return service != null;
    }

    public bool Stop()
    {
        var service = Process.GetProcessesByName(fileName).FirstOrDefault();
        if (service == null) return false;
        service.Kill();
        return true;
    }
}