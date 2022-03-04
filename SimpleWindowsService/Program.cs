using SimpleWindowsService;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<FileWatcherWorkerService>();
    }).UseWindowsService()
    .Build();
await host.RunAsync();

