using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Hammer.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which listens for staff reactions.
/// </summary>
internal sealed class StaffReactionService : BackgroundService
{
    private readonly ConfigurationService _configurationService;
    private readonly MessageDeletionService _deletionService;
    private readonly DiscordClient _discordClient;
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffReactionService" /> class.
    /// </summary>
    public StaffReactionService(DiscordClient discordClient, ConfigurationService configurationService,
        InfractionService infractionService, MessageDeletionService deletionService)
    {
        _discordClient = discordClient;
        _configurationService = configurationService;
        _infractionService = infractionService;
        _deletionService = deletionService;
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

        DiscordMessage message = e.Message;

        if (message.Author is null)
        {
            // not cached! fetch new message
            message = await message.Channel.GetMessageAsync(message.Id).ConfigureAwait(false);
        }

        DiscordUser author = message.Author;
        GuildConfiguration configuration = _configurationService.GetGuildConfiguration(guild);

        var staffMember = (DiscordMember) e.User;
        if (!staffMember.IsStaffMember(guild))
            return;

        ReactionConfiguration reactionConfiguration = configuration.ReactionConfiguration;
        DiscordEmoji emoji = e.Emoji;
        string reaction = emoji.GetDiscordName();

        if (reaction == reactionConfiguration.GagReaction)
        {
            await e.Message.DeleteReactionAsync(emoji, staffMember).ConfigureAwait(false);
            await _infractionService.GagAsync(author, staffMember, message).ConfigureAwait(false);
        }
        else if (reaction == reactionConfiguration.DeleteMessageReaction)
        {
            await _deletionService.DeleteMessageAsync(message, staffMember).ConfigureAwait(false);
        }
    }
}
