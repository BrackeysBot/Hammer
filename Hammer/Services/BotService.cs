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
        slashCommands.RegisterCommands<AltCommand>();
        slashCommands.RegisterCommands<BanCommand>();
        slashCommands.RegisterCommands<DeleteMessageCommand>();
        slashCommands.RegisterCommands<GagCommand>();
        slashCommands.RegisterCommands<HistoryCommand>();
        slashCommands.RegisterCommands<InfoCommand>();
        slashCommands.RegisterCommands<InfractionCommand>();
        slashCommands.RegisterCommands<KickCommand>();
        slashCommands.RegisterCommands<MessageCommand>();
        slashCommands.RegisterCommands<MessageHistoryCommand>();
        slashCommands.RegisterCommands<MuteCommand>();
        slashCommands.RegisterCommands<NoteCommand>();
        slashCommands.RegisterCommands<PruneInfractionsCommand>();
        slashCommands.RegisterCommands<ReportCommands>();
        slashCommands.RegisterCommands<RuleCommand>();
        slashCommands.RegisterCommands<RulesCommand>();
        slashCommands.RegisterCommands<SelfHistoryCommand>();
        slashCommands.RegisterCommands<StaffHistoryCommand>();
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
        {
            _logger.LogError("API response: {Response}", discordException.JsonMessage);
        }

        return Task.CompletedTask;
    }

    private Task OnContextMenuErrored(SlashCommandsExtension _, ContextMenuErrorEventArgs args)
    {
        ContextMenuContext context = args.Context;
        if (args.Exception is ContextMenuExecutionChecksFailedException)
        {
            // no need to log ChecksFailedException
            return context.CreateResponseAsync("You do not have permission to use this command.", true);
        }

        string? name = context.Interaction.Data.Name;
        _logger.LogError(args.Exception, "An exception was thrown when executing context menu '{Name}'", name);
        if (args.Exception is DiscordException discordException)
        {
            _logger.LogError("API response: {Message}", discordException.JsonMessage);
        }

        return Task.CompletedTask;
    }

    private Task OnContextMenuInvoked(SlashCommandsExtension _, ContextMenuInvokedEventArgs args)
    {
        DiscordInteractionResolvedCollection? resolved = args.Context.Interaction?.Data?.Resolved;
        var properties = new List<string>();

        AddProperty("attachments", resolved?.Attachments, a => a.Url);
        AddProperty("channels", resolved?.Channels, c => c.Name);
        AddProperty("members", resolved?.Members, m => m.Id);
        AddProperty("messages", resolved?.Messages, m => m.Id);
        AddProperty("roles", resolved?.Roles, r => r.Id);
        AddProperty("users", resolved?.Users, u => u.Id);

        DiscordUser user = args.Context.User;
        string command = args.Context.CommandName;
        string propertyString = string.Join("; ", properties);
        _logger.LogInformation("{User} ran context menu '{Command}' with resolved {Properties}", user, command, propertyString);

        return Task.CompletedTask;

        void AddProperty<T, TResult>(string name, IReadOnlyDictionary<ulong, T>? dictionary, Func<T, TResult> selector)
        {
            if (dictionary is null || dictionary.Count == 0)
            {
                return;
            }

            properties.Add($"{name}: {string.Join(", ", dictionary.Select(r => selector(r.Value)))}");
        }
    }

    private Task OnSlashCommandErrored(SlashCommandsExtension _, SlashCommandErrorEventArgs args)
    {
        InteractionContext context = args.Context;
        if (args.Exception is SlashExecutionChecksFailedException)
        {
            // no need to log SlashExecutionChecksFailedException
            return context.CreateResponseAsync("You do not have permission to use this command.", true);
        }

        string? name = context.Interaction.Data.Name;
        _logger.LogError(args.Exception, "An exception was thrown when executing slash command '{Name}'", name);
        if (args.Exception is DiscordException discordException)
        {
            _logger.LogError("API response: {Message}", discordException.JsonMessage);
        }

        return Task.CompletedTask;
    }

    private Task OnSlashCommandInvoked(SlashCommandsExtension _, SlashCommandInvokedEventArgs args)
    {
        var optionsString = "";
        if (args.Context.Interaction?.Data?.Options is { } options)
        {
            optionsString = $" {string.Join(" ", options.Select(o => $"{o?.Name}: '{o?.Value}'"))}";
        }

        DiscordUser user = args.Context.User;
        string command = args.Context.CommandName;
        _logger.LogInformation("{User} ran slash command /{Command}{Options}", user, command, optionsString);
        return Task.CompletedTask;
    }
}
