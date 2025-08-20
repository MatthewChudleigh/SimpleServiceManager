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
        services.AddHostedService<ServiceManager>();
    })
    .ConfigureLogging((hostingContext, logging) => logging.AddLog4Net("log4net.config"))
    .Build();

await host.RunAsync(ServiceManager.MyToken.Token);
