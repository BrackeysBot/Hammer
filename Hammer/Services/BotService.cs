using System.Reflection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Hammer.Commands;
using Hammer.Commands.Infractions;
using Hammer.Commands.Notes;
using Hammer.Commands.Reports;
using Hammer.Commands.Rules;
using Hammer.Commands.V3Migration;
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
    private readonly HttpClient _httpClient;
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BotService" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="discordClient">The Discord client.</param>
    public BotService(IServiceProvider serviceProvider, HttpClient httpClient, DiscordClient discordClient)
    {
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
        _discordClient = discordClient;

        var attribute = typeof(BotService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Version = attribute?.InformationalVersion ?? "Unknown";
    }

    /// <summary>
    ///     Gets the date and time at which the bot was started.
    /// </summary>
    /// <value>The start timestamp.</value>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>
    ///     Gets the bot version.
    /// </summary>
    /// <value>The bot version.</value>
    public string Version { get; }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        UnregisterEvents(_discordClient.GetSlashCommands());
        return Task.WhenAll(_discordClient.DisconnectAsync(), base.StopAsync(cancellationToken));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.UtcNow;
        Logger.Info($"Hammer v{Version} is starting...");

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
        slashCommands.RegisterCommands<InfoCommand>();
        slashCommands.RegisterCommands<InfractionCommand>();
        slashCommands.RegisterCommands<KickCommand>();
        slashCommands.RegisterCommands<MessageCommand>();
        slashCommands.RegisterCommands<MessageHistoryCommand>();
        slashCommands.RegisterCommands<MigrateCommand>();
        slashCommands.RegisterCommands<MuteCommand>();
        slashCommands.RegisterCommands<NoteCommand>();
        slashCommands.RegisterCommands<PruneInfractionsCommand>();
        slashCommands.RegisterCommands<ReportCommands>();
        slashCommands.RegisterCommands<RuleCommand>();
        slashCommands.RegisterCommands<RulesCommand>();
        slashCommands.RegisterCommands<SelfHistoryCommand>();
        slashCommands.RegisterCommands<UnbanCommand>();
        slashCommands.RegisterCommands<UnmuteCommand>();
        slashCommands.RegisterCommands<ViewMessageCommand>();
        slashCommands.RegisterCommands<WarnCommand>();
        RegisterEvents(slashCommands);

        Logger.Info("Connecting to Discord...");
        await _discordClient.ConnectAsync().ConfigureAwait(false);
    }

    private static void RegisterEvents(SlashCommandsExtension slashCommands)
    {
        slashCommands.AutocompleteErrored += OnAutocompleteErrored;
        slashCommands.ContextMenuErrored += OnContextMenuErrored;
        slashCommands.ContextMenuInvoked += OnContextMenuInvoked;
        slashCommands.SlashCommandErrored += OnSlashCommandErrored;
        slashCommands.SlashCommandInvoked += OnSlashCommandInvoked;
    }

    private static void UnregisterEvents(SlashCommandsExtension slashCommands)
    {
        slashCommands.AutocompleteErrored -= OnAutocompleteErrored;
        slashCommands.ContextMenuErrored -= OnContextMenuErrored;
        slashCommands.ContextMenuInvoked -= OnContextMenuInvoked;
        slashCommands.SlashCommandErrored -= OnSlashCommandErrored;
        slashCommands.SlashCommandInvoked -= OnSlashCommandInvoked;
    }

    private static Task OnAutocompleteErrored(SlashCommandsExtension _, AutocompleteErrorEventArgs args)
    {
        Logger.Error(args.Exception, "An exception was thrown when performing autocomplete");
        if (args.Exception is DiscordException discordException) Logger.Error($"API response: {discordException.JsonMessage}");

        return Task.CompletedTask;
    }

    private static Task OnContextMenuErrored(SlashCommandsExtension _, ContextMenuErrorEventArgs args)
    {
        ContextMenuContext context = args.Context;
        if (args.Exception is ContextMenuExecutionChecksFailedException)
        {
            context.CreateResponseAsync("You do not have permission to use this command.", true);
            return Task.CompletedTask; // no need to log ChecksFailedException
        }

        string? name = context.Interaction.Data.Name;
        Logger.Error(args.Exception, $"An exception was thrown when executing context menu '{name}'");
        if (args.Exception is DiscordException discordException) Logger.Error($"API response: {discordException.JsonMessage}");

        return Task.CompletedTask;
    }

    private static Task OnContextMenuInvoked(SlashCommandsExtension _, ContextMenuInvokedEventArgs args)
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
        if (resolved?.Roles?.Count > 0) properties.Add($"roles: {string.Join(", ", resolved.Roles.Select(r => r.Value.Id))}");
        if (resolved?.Users?.Count > 0) properties.Add($"users: {string.Join(", ", resolved.Users.Select(r => r.Value.Id))}");

        Logger.Info($"{args.Context.User} invoked context menu '{args.Context.CommandName}' with resolved " +
                    string.Join("; ", properties));

        return Task.CompletedTask;
    }

    private static Task OnSlashCommandErrored(SlashCommandsExtension _, SlashCommandErrorEventArgs args)
    {
        InteractionContext context = args.Context;
        if (args.Exception is SlashExecutionChecksFailedException)
        {
            context.CreateResponseAsync("You do not have permission to use this command.", true);
            return Task.CompletedTask; // no need to log SlashExecutionChecksFailedException
        }

        string? name = context.Interaction.Data.Name;
        Logger.Error(args.Exception, $"An exception was thrown when executing slash command '{name}'");
        if (args.Exception is DiscordException discordException) Logger.Error($"API response: {discordException.JsonMessage}");

        return Task.CompletedTask;
    }

    private static Task OnSlashCommandInvoked(SlashCommandsExtension _, SlashCommandInvokedEventArgs args)
    {
        var optionsString = "";
        if (args.Context.Interaction?.Data?.Options is { } options)
            optionsString = $" {string.Join(" ", options.Select(o => $"{o?.Name}: '{o?.Value}'"))}";

        Logger.Info($"{args.Context.User} ran slash command /{args.Context.CommandName}{optionsString}");
        return Task.CompletedTask;
    }
}
