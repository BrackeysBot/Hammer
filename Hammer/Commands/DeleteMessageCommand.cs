using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the <c>Delete Message</c> context menu.
/// </summary>
internal sealed class DeleteMessageCommand : ApplicationCommandModule
{
    private readonly MessageDeletionService _deletionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeleteMessageCommand" /> class.
    /// </summary>
    /// <param name="deletionService">The message deletion service.</param>
    public DeleteMessageCommand(MessageDeletionService deletionService)
    {
        _deletionService = deletionService;
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Delete Message", false)]
    [SlashRequireGuild]
    public async Task DeleteMessageAsync(ContextMenuContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        var builder = new DiscordWebhookBuilder();

        DiscordMessage? message = context.Interaction.Data.Resolved.Messages.FirstOrDefault().Value;
        if (message is null)
        {
            builder.WithContent("The specified message could not be retrieved.");
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        await _deletionService.DeleteMessageAsync(message, context.Member).ConfigureAwait(false);

        builder.WithContent($"Message {message.Id} by {message.Author.Mention} deleted.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
