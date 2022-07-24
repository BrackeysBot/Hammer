using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Hammer.Commands;
using Hammer.Commands.Infractions;
using Hammer.Commands.Notes;
using Hammer.Commands.Reports;
using Hammer.Commands.Rules;
using Microsoft.Extensions.Hosting;
using NLog;
using ILogger = NLog.ILogger;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages the bot's Discord connection.
/// </summary>
internal sealed class BotService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BotService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="discordClient">The Discord client.</param>
    public BotService(IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        _serviceProvider = serviceProvider;
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.WhenAll(_discordClient.DisconnectAsync(), base.StopAsync(cancellationToken));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // _discordClient.UseInteractivity();
        
        SlashCommandsExtension slashCommands = _discordClient.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = _serviceProvider
        });

        Logger.Info("Registering commands...");
        slashCommands.RegisterCommands<BanCommand>();
        slashCommands.RegisterCommands<DeleteMessageCommand>();
        slashCommands.RegisterCommands<GagCommand>();
        slashCommands.RegisterCommands<HistoryCommand>();
        slashCommands.RegisterCommands<InfractionCommand>();
        slashCommands.RegisterCommands<KickCommand>();
        slashCommands.RegisterCommands<MessageCommand>();
        slashCommands.RegisterCommands<MuteCommand>();
        slashCommands.RegisterCommands<NoteCommand>();
        slashCommands.RegisterCommands<ReportCommands>();
        slashCommands.RegisterCommands<RuleCommand>();
        slashCommands.RegisterCommands<RulesCommand>();
        slashCommands.RegisterCommands<SelfHistoryCommand>();
        slashCommands.RegisterCommands<UnbanCommand>();
        slashCommands.RegisterCommands<UnmuteCommand>();
        slashCommands.RegisterCommands<WarnCommand>();

        Logger.Info("Connecting to Discord...");
        _discordClient.Ready += OnReady;

        slashCommands.SlashCommandErrored += (_, args) =>
        {
            Console.WriteLine($"The exception is here! {args.Exception}");
            return Task.CompletedTask;
        };

        await _discordClient.ConnectAsync().ConfigureAwait(false);
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        Logger.Info("Discord client ready");
        return Task.CompletedTask;
    }
}
