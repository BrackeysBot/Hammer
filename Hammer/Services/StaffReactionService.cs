using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
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
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
            return;

        var staffMember = (DiscordMember) e.User;
        if (!staffMember.IsStaffMember(configuration))
            return;

        ReactionConfiguration reactionConfiguration = configuration.Reactions;
        DiscordEmoji emoji = e.Emoji;
        string reaction = emoji.GetDiscordName();

        if (reaction == reactionConfiguration.GagReaction)
        {
            await message.DeleteReactionAsync(emoji, staffMember).ConfigureAwait(false);
            await _infractionService.GagAsync(author, staffMember, message).ConfigureAwait(false);
        }
        else if (reaction == reactionConfiguration.HistoryReaction)
        {
            await message.DeleteReactionAsync(emoji, staffMember).ConfigureAwait(false);

            var builder = new DiscordMessageBuilder();
            var response = new InfractionHistoryResponse(_infractionService, author, staffMember, guild, true);

            for (var pageIndex = 0; pageIndex < response.Pages; pageIndex++)
            {
                DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(response, pageIndex);
                builder.AddEmbed(embed);
            }

            await staffMember.SendMessageAsync(builder).ConfigureAwait(false);
        }
        else if (reaction == reactionConfiguration.DeleteMessageReaction)
        {
            await _deletionService.DeleteMessageAsync(message, staffMember).ConfigureAwait(false);
        }
    }
}
