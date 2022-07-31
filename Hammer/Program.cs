using DSharpPlus;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using X10D.Hosting.DependencyInjection;

Directory.CreateDirectory("data");

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("data/config.json", true, true))
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddNLog();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            LoggerFactory = new NLogLoggerFactory(),
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers
        }));

        services.AddSingleton<HttpClient>();

        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<MailmanService>();
        services.AddSingleton<MessageService>();
        services.AddSingleton<MessageDeletionService>();
        services.AddSingleton<WarningService>();

        services.AddHostedSingleton<BotService>();
        services.AddHostedSingleton<BanService>();
        services.AddHostedSingleton<DatabaseService>();
        services.AddHostedSingleton<DiscordLogService>();
        services.AddHostedSingleton<InfractionService>();
        services.AddHostedSingleton<InfractionCooldownService>();
        services.AddHostedSingleton<LoggingService>();
        services.AddHostedSingleton<MemberNoteService>();
        services.AddHostedSingleton<MessageReportService>();
        services.AddHostedSingleton<MessageTrackingService>();
        services.AddHostedSingleton<MuteService>();
        services.AddHostedSingleton<RuleService>();
        services.AddHostedSingleton<StaffReactionService>();
        services.AddHostedSingleton<UserReactionService>();

        services.AddDbContext<HammerContext>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
