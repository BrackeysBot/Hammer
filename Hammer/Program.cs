using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("config.json", false, true))
    .ConfigureServices(services =>
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddNLog();
        });

        services.AddSingleton(provider =>
        {
            var configuration = new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                Intents = DiscordIntents.All,
                LoggerFactory = new NLogLoggerFactory(),
                ServiceProvider = provider
            };

            return new DiscordClient(configuration);
        });

        services.AddSingleton<ConfigurationService>();

        services.AddHostedService<StartupService>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
