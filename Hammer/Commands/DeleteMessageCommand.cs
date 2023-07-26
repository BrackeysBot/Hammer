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
        var embed = new DiscordEmbedBuilder();

        DiscordMessage? message = context.TargetMessage;
        if (message is null)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Deletion failed");
            embed.WithDescription("The specified message could not be retrieved.");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        try
        {
            await _deletionService.DeleteMessageAsync(message, context.Member).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithAuthor(exception.GetType().ToString());
            embed.WithTitle("Deletion failed");
            embed.WithDescription(exception.Message);
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Message deleted");
        embed.WithDescription($"Message {message.Id} by {message.Author.Mention} deleted.");
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
