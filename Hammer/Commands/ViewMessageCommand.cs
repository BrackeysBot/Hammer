using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Configuration;
using Hammer.Resources;
using Hammer.Services;
using X10D.DSharpPlus;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>viewmessage</c> command.
/// </summary>
internal sealed class ViewMessageCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly MessageService _messageService;
    private readonly MessageDeletionService _messageDeletionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewMessageCommand" /> class.
    /// </summary>
    public ViewMessageCommand(
        ConfigurationService configurationService,
        MessageService messageService,
        MessageDeletionService messageDeletionService)
    {
        _configurationService = configurationService;
        _messageService = messageService;
        _messageDeletionService = messageDeletionService;
    }

    [SlashCommand("viewmessage", "Views a staff message, or deleted message, by its ID.", false)]
    public async Task ViewMessageAsync(
        InteractionContext context,
        [Option("id", "The ID of the message to retrieve.")] string rawId
    )
    {
        await context.DeferAsync().ConfigureAwait(false);
        var embed = new DiscordEmbedBuilder();

        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        if (long.TryParse(rawId, out long staffMessageId) &&
            await _messageService.GetStaffMessage(staffMessageId) is { } staffMessage &&
            staffMessage.GuildId == context.Guild.Id)
        {
            embed.WithColor(guildConfiguration.PrimaryColor);
            embed.WithTitle($"Message {staffMessage.Id}");
            embed.AddField("Recipient", MentionUtility.MentionUser(staffMessage.RecipientId), true);
            embed.AddField("Staff Member", MentionUtility.MentionUser(staffMessage.StaffMemberId), true);
            embed.AddField("Sent", Formatter.Timestamp(staffMessage.SentAt), true);
            embed.AddField("Content", Formatter.BlockCode(staffMessage.Content));
        }
        else if (ulong.TryParse(rawId, out ulong deletedMessageId) &&
            await _messageDeletionService.GetDeletedMessage(deletedMessageId) is { } deletedMessage &&
            deletedMessage.GuildId == context.Guild.Id)
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle($"Deleted Message {deletedMessage.MessageId}");
            embed.AddField("Author", MentionUtility.MentionUser(deletedMessage.AuthorId), true);
            embed.AddField("Channel", MentionUtility.MentionChannel(deletedMessage.ChannelId), true);
            embed.AddField("Created", Formatter.Timestamp(deletedMessage.CreationTimestamp), true);
            embed.AddField("Deleted", Formatter.Timestamp(deletedMessage.DeletionTimestamp), true);
            embed.AddField("Staff Member", MentionUtility.MentionUser(deletedMessage.StaffMemberId), true);

            bool hasContent = !string.IsNullOrWhiteSpace(deletedMessage.Content);
            bool hasAttachments = deletedMessage.Attachments.Count > 0;

            string? content = hasContent ? Formatter.Sanitize(deletedMessage.Content) : null;
            string? attachments = hasAttachments ? string.Join('\n', deletedMessage.Attachments.Select(a => a.AbsoluteUri)) : null;
            
            embed.AddFieldIf(hasContent, "Content", () => Formatter.BlockCode(content!.Length >= 1014 ? content[..1011] + "..." : content));
            embed.AddFieldIf(hasAttachments, "Attachments", attachments);
        }
        else
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("No such message");
            embed.WithDescription($"Could not find a message with the ID {rawId}");
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}
