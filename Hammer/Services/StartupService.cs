using System;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.CommandsNext;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;
using Hammer.CommandModules;
using Hammer.CommandModules.Rules;
using Hammer.CommandModules.Staff;
using Hammer.CommandModules.User;
using Hammer.Configuration;
using Hammer.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hammer.Services;

/// <summary>
///     Represents a class which performs startup initialization, including creating the database and connecting the Discord
///     client.
/// </summary>
internal sealed class StartupService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StartupService" /> class.
    /// </summary>
    public StartupService(
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient
    )
    {
        _serviceProvider = serviceProvider;
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken);

        Logger.Info($"Registering ApplicationCommandsExtension");
        ApplicationCommandsExtension applicationCommands = _discordClient.UseApplicationCommands(
            new ApplicationCommandsConfiguration
            {
                ServiceProvider = _serviceProvider,
            });

        Logger.Info("Registering command converters");
        commandsNext.UnregisterConverter<TimeSpanConverter>(); // default converter does not support weeks/months/years...
        commandsNext.RegisterConverter(new TimeSpanArgumentConverter()); // but my one does!

        Logger.Info("Registering command modules");
        commandsNext.RegisterCommands<RulesModule>();
        commandsNext.RegisterCommands<StaffModule>();

        Logger.Info("Registering InteractivityExtension");
        _discordClient.UseInteractivity(new InteractivityConfiguration());

        Logger.Info("Connecting to Discord");
        await _discordClient.ConnectAsync();

        await applicationCommands.CleanGlobalCommandsAsync();
        await applicationCommands.CleanGuildCommandsAsync();
    }
}
