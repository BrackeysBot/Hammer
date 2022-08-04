using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Hammer.Configuration;
using Hammer.Extensions;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hammer.Services;

/// <summary>
///     Represents a service which listens for user reactions.
/// </summary>
internal sealed class UserReactionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigurationService _configurationService;
    private readonly DiscordClient _discordClient;
    private readonly MessageReportService _messageReportService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserReactionService" /> class.
    /// </summary>
    public UserReactionService(
        ConfigurationService configurationService,
        DiscordClient discordClient,
        MessageReportService messageReportService
    )
    {
        _configurationService = configurationService;
        _discordClient = discordClient;
        _messageReportService = messageReportService;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.MessageReactionAdded += DiscordClientOnMessageReactionAdded;
        return Task.CompletedTask;
    }

    private async Task DiscordClientOnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
    {
        if (e.Guild is not { } guild || e.User.IsBot)
            return;

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            return;

        ReactionConfiguration reactionConfiguration = guildConfiguration.Reactions;
        string reaction = e.Emoji.GetDiscordName();
        if (reaction == reactionConfiguration.ReportReaction)
        {
            await e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
            await _messageReportService.ReportMessageAsync(e.Message, (DiscordMember) e.User).ConfigureAwait(false);
        }
        else if (reaction == reactionConfiguration.DeleteMessageReaction &&
                 guildConfiguration.AllowInteractionAuthorDeletion &&
                 !((DiscordMember) e.User).IsStaffMember(guildConfiguration))
        {
            if (e.Message.Interaction is { } interaction && interaction.User == e.User && e.Message.Author == sender.CurrentUser)
            {
                Logger.Info($"{e.User} requested their interaction to be deleted");
                await e.Message.DeleteReactionAsync(e.Emoji, e.User).ConfigureAwait(false);
                await e.Message.DeleteAsync("Interaction author requested deletion").ConfigureAwait(false);
            }
        }
    }
}
