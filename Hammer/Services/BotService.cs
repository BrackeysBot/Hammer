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
using Microsoft.Extensions.Logging;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages the bot's Discord connection.
/// </summary>
internal sealed class BotService : BackgroundService
{
    private readonly ILogger<BotService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BotService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="discordClient">The Discord client.</param>
    public BotService(ILogger<BotService> logger, IServiceProvider serviceProvider, DiscordClient discordClient)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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
        UnregisterEvents();
        return Task.WhenAll(_discordClient.DisconnectAsync(), base.StopAsync(cancellationToken));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Hammer v{Version} is starting", Version);

        _discordClient.UseInteractivity();

        SlashCommandsExtension slashCommands = _discordClient.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = _serviceProvider
        });

        _logger.LogInformation("Registering commands");
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
        RegisterEvents();
        
        _logger.LogInformation("Connecting to Discord");
        await _discordClient.ConnectAsync().ConfigureAwait(false);
    }

    private void RegisterEvents()
    {
        SlashCommandsExtension slashCommands = _discordClient.GetSlashCommands();
        slashCommands.AutocompleteErrored += OnAutocompleteErrored;
        slashCommands.ContextMenuErrored += OnContextMenuErrored;
        slashCommands.ContextMenuInvoked += OnContextMenuInvoked;
        slashCommands.SlashCommandErrored += OnSlashCommandErrored;
        slashCommands.SlashCommandInvoked += OnSlashCommandInvoked;
    }

    private void UnregisterEvents()
    {
        SlashCommandsExtension slashCommands = _discordClient.GetSlashCommands();
        slashCommands.AutocompleteErrored -= OnAutocompleteErrored;
        slashCommands.ContextMenuErrored -= OnContextMenuErrored;
        slashCommands.ContextMenuInvoked -= OnContextMenuInvoked;
        slashCommands.SlashCommandErrored -= OnSlashCommandErrored;
        slashCommands.SlashCommandInvoked -= OnSlashCommandInvoked;
    }

    private Task OnAutocompleteErrored(SlashCommandsExtension _, AutocompleteErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "An exception was thrown when performing autocomplete");
        if (args.Exception is DiscordException discordException)
            _logger.LogError("API response: {Response}", discordException.JsonMessage);

        return Task.CompletedTask;
    }

    private Task OnContextMenuErrored(SlashCommandsExtension _, ContextMenuErrorEventArgs args)
    {
        ContextMenuContext context = args.Context;
        if (args.Exception is ContextMenuExecutionChecksFailedException)
        {
            context.CreateResponseAsync("You do not have permission to use this command.", true);
            return Task.CompletedTask; // no need to log ChecksFailedException
        }

        string? name = context.Interaction.Data.Name;
        _logger.LogError(args.Exception, "An exception was thrown when executing context menu '{Name}'", name);
        if (args.Exception is DiscordException discordException)
            _logger.LogError("API response: {Response}", discordException.JsonMessage);

        return Task.CompletedTask;
    }

    private Task OnContextMenuInvoked(SlashCommandsExtension _, ContextMenuInvokedEventArgs args)
    {
        ContextMenuContext context = args.Context;
        DiscordInteractionResolvedCollection? resolved = context.Interaction?.Data?.Resolved;
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

        _logger.LogInformation("{User} invoked context menu {Command} with resolved {Properties}", context.User,
            context.CommandName, string.Join("; ", properties));

        return Task.CompletedTask;
    }

    private Task OnSlashCommandErrored(SlashCommandsExtension _, SlashCommandErrorEventArgs args)
    {
        InteractionContext context = args.Context;
        if (args.Exception is SlashExecutionChecksFailedException)
        {
            context.CreateResponseAsync("You do not have permission to use this command.", true);
            return Task.CompletedTask; // no need to log SlashExecutionChecksFailedException
        }

        string? name = context.Interaction.Data.Name;
        _logger.LogError(args.Exception, "An exception was thrown when executing slash command '{Name}'", name);
        if (args.Exception is DiscordException discordException)
            _logger.LogError("API response: {Response}", discordException.JsonMessage);

        return Task.CompletedTask;
    }

    private Task OnSlashCommandInvoked(SlashCommandsExtension _, SlashCommandInvokedEventArgs args)
    {
        var optionsString = "";
        InteractionContext context = args.Context;
        if (context.Interaction?.Data?.Options is { } options)
            optionsString = $" {string.Join(" ", options.Select(o => $"{o?.Name}: '{o?.Value}'"))}";

        _logger.LogInformation("{User} ran slash command /{Command}{Options}", context.User, context.CommandName, optionsString);
        return Task.CompletedTask;
    }
}
