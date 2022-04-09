using System;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
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
    private readonly UserTrackingService _userTrackingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffReactionService" /> class.
    /// </summary>
    public StaffReactionService(DiscordClient discordClient, ConfigurationService configurationService,
        InfractionService infractionService, MessageDeletionService deletionService, UserTrackingService userTrackingService)
    {
        _discordClient = discordClient;
        _configurationService = configurationService;
        _infractionService = infractionService;
        _deletionService = deletionService;
        _userTrackingService = userTrackingService;
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
            message = await message.Channel.GetMessageAsync(message.Id);
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
            _ = e.Message.DeleteReactionAsync(emoji, staffMember);
            _ = _infractionService.GagAsync(author, staffMember, message);
        }
        else if (reaction == reactionConfiguration.DeleteMessageReaction)
        {
            _ = e.Message.DeleteReactionAsync(emoji, staffMember);
            _ = _deletionService.DeleteMessageAsync(message, staffMember);
        }
        else if (reaction == reactionConfiguration.TrackReaction)
        {
            _ = e.Message.DeleteReactionAsync(emoji, staffMember);
            _ = _userTrackingService.TrackUserAsync(author, guild, TimeSpan.FromDays(7));
        }
    }
}
