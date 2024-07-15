using DSharpPlus;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using X10D.Hosting.DependencyInjection;

Directory.CreateDirectory("data");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/latest.log", rollingInterval: RollingInterval.Day)
#if DEBUG
    .MinimumLevel.Debug()
#endif
    .CreateLogger();

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("data/config.json", true, true))
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            LoggerFactory = new SerilogLoggerFactory(),
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents
        }));

        services.AddDbContextFactory<HammerContext>();
        services.AddHostedSingleton<DatabaseService>();

        services.AddSingleton<HttpClient>();
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<InfractionStatisticsService>();
        services.AddSingleton<MailmanService>();
        services.AddSingleton<MessageService>();
        services.AddSingleton<MessageDeletionService>();
        services.AddSingleton<WarningService>();

        services.AddHostedService<StaffReactionService>();
        services.AddHostedService<UserReactionService>();

        services.AddHostedSingleton<AltAccountService>();
        services.AddHostedSingleton<BanService>();
        services.AddHostedSingleton<DiscordLogService>();
        services.AddHostedSingleton<InfractionService>();
        services.AddHostedSingleton<InfractionCooldownService>();
        services.AddHostedSingleton<MemberNoteService>();
        services.AddHostedSingleton<MessageReportService>();
        services.AddHostedSingleton<MessageTrackingService>();
        services.AddHostedSingleton<MuteService>();
        services.AddHostedSingleton<RuleService>();


        services.AddHostedSingleton<BotService>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
