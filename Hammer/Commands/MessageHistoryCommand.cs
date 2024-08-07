using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>messagehistory</c> command.
/// </summary>
internal sealed class MessageHistoryCommand : ApplicationCommandModule
{
    private readonly MessageService _messageService;
    private readonly MessageDeletionService _messageDeletionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageHistoryCommand" /> class.
    /// </summary
    public MessageHistoryCommand(MessageService messageService, MessageDeletionService messageDeletionService)
    {
        _messageService = messageService;
        _messageDeletionService = messageDeletionService;
    }

    [SlashCommand("messagehistory", "Views the message history for a user.", false)]
    public async Task MessageHistoryAsync(
        InteractionContext context,
        [Option("user", "The user whose message history to view.")] DiscordUser user
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        await context.DeferAsync().ConfigureAwait(false);
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Message History");
        embed.WithAuthor(user);

        var staffMessages = new List<string>();
        await foreach (StaffMessage staffMessage in _messageService.GetStaffMessages(user, context.Guild))
        {
            staffMessages.Add($"**ID: {staffMessage.Id}** \u2022 " +
                              $"Sent by {MentionUtility.MentionUser(staffMessage.StaffMemberId)} \u2022 " +
                              Formatter.Timestamp(staffMessage.SentAt));
        }


        var deletedMessages = new List<string>();
        await foreach (DeletedMessage deletedMessage in _messageDeletionService.GetDeletedMessages(user, context.Guild))
        {
            deletedMessages.Add($"**ID: {deletedMessage.MessageId}** \u2022 " +
                              $"Sent in {MentionUtility.MentionChannel(deletedMessage.ChannelId)} \u2022 " +
                              Formatter.Timestamp(deletedMessage.CreationTimestamp));
        }

        // GetStaffMessages and GetDeletedMessages yield messages in chronological order,
        // but to be consistent with /history command, we need to reverse that order so that
        // most recent messages are displayed first.
        staffMessages.Reverse();
        deletedMessages.Reverse();

        string staffMessagesResult = staffMessages.Count > 0 ? string.Join("\n", staffMessages) : "*None*";
        string deletedMessagesResult = deletedMessages.Count > 0 ? string.Join("\n", deletedMessages) : "*None*";

        embed.WithDescription($"__**Staff Messages**__\n{staffMessagesResult}\n\n" +
                              $"__**Deleted Messages**__\n{deletedMessagesResult}");

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}
