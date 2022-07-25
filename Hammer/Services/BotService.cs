using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
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
        _discordClient.UseInteractivity();

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

        RegisterEvents(slashCommands);

        await _discordClient.ConnectAsync().ConfigureAwait(false);
    }

    private Task OnReady(DiscordClient sender, ReadyEventArgs e)
    {
        Logger.Info("Discord client ready");
        return Task.CompletedTask;
    }

    private static void RegisterEvents(SlashCommandsExtension slashCommands)
    {
        slashCommands.AutocompleteErrored += (_, args) =>
        {
            Logger.Error(args.Exception, $"An exception was thrown when performing autocomplete");
            if (args.Exception is DiscordException discordException)
                Logger.Error($"API response: {discordException.JsonMessage}");

            return Task.CompletedTask;
        };

        slashCommands.SlashCommandInvoked += (_, args) =>
        {
            var optionsString = "";
            if (args.Context.Interaction?.Data?.Options is { } options)
                optionsString = $" {string.Join(" ", options.Select(o => $"{o?.Name}: '{o?.Value}'"))}";

            Logger.Info($"{args.Context.User} ran slash command /{args.Context.CommandName}{optionsString}");
            return Task.CompletedTask;
        };

        slashCommands.ContextMenuInvoked += (_, args) =>
        {
            DiscordInteractionResolvedCollection? resolved = args.Context.Interaction?.Data?.Resolved;
            var properties = new List<string>();
            if (resolved?.Attachments?.Count > 0)
                properties.Add($"attachments: {string.Join(", ", resolved.Attachments.Select(a => a.Value.Url))}");
            if (resolved?.Channels?.Count > 0)
                properties.Add($"channels: {string.Join(", ", resolved.Channels.Select(c => c.Value.Name))}");
            if (resolved?.Members?.Count > 0)
                properties.Add($"members: {string.Join(", ", resolved.Members.Select(m => m.Value.Id))}");
            if (resolved?.Messages?.Count > 0)
                properties.Add($"messages: {string.Join(", ", resolved.Messages.Select(m => m.Value.Id))}");
            if (resolved?.Roles?.Count > 0)
                properties.Add($"roles: {string.Join(", ", resolved.Roles.Select(r => r.Value.Id))}");
            if (resolved?.Users?.Count > 0)
                properties.Add($"users: {string.Join(", ", resolved.Users.Select(r => r.Value.Id))}");

            Logger.Info($"{args.Context.User} invoked context menu '{args.Context.CommandName}' with resolved " +
                        string.Join("; ", properties));

            return Task.CompletedTask;
        };

        slashCommands.ContextMenuErrored += (_, args) =>
        {
            ContextMenuContext context = args.Context;
            if (args.Exception is ContextMenuExecutionChecksFailedException)
            {
                context.CreateResponseAsync("You do not have permission to use this command.", true);
                return Task.CompletedTask; // no need to log ChecksFailedException
            }

            string? name = context.Interaction.Data.Name;
            Logger.Error(args.Exception, $"An exception was thrown when executing context menu '{name}'");
            if (args.Exception is DiscordException discordException)
                Logger.Error($"API response: {discordException.JsonMessage}");

            return Task.CompletedTask;
        };

        slashCommands.SlashCommandErrored += (_, args) =>
        {
            InteractionContext context = args.Context;
            if (args.Exception is SlashExecutionChecksFailedException)
            {
                context.CreateResponseAsync("You do not have permission to use this command.", true);
                return Task.CompletedTask; // no need to log SlashExecutionChecksFailedException
            }

            string? name = context.Interaction.Data.Name;
            Logger.Error(args.Exception, $"An exception was thrown when executing slash command '{name}'");
            if (args.Exception is DiscordException discordException)
                Logger.Error($"API response: {discordException.JsonMessage}");

            return Task.CompletedTask;
        };
    }
}
