using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.Interactivity;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationInvalidJsonState : ConversationState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationInvalidJsonState" /> class.
    /// </summary>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationInvalidJsonState(Conversation conversation)
        : base(conversation)
    {
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        var builder = new DiscordWebhookBuilder();
        builder.Clear();

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Invalid Database");
        embed.WithDescription(
            "The v3 database you have provided contains contains an invalid JSON structure.\n" +
            "The migration cannot continue with this database.\n\n" +
            "You may attempt again using a different database, or cancel the process.");

        string retryId = $"conv-{Conversation.Id:N}-retry";
        string cancelId = $"conv-{Conversation.Id:N}-cancel";

        var buttons = new List<DiscordButtonComponent>
        {
            new(ButtonStyle.Primary, retryId, "Retry", emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🔁"))),
            new(ButtonStyle.Danger, cancelId, "Cancel", emoji: new DiscordComponentEmoji(948187924120342538))
        };

        builder.AddComponents(buttons);
        builder.AddEmbed(embed);

        DiscordMessage message = await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);

        InteractivityResult<ComponentInteractionCreateEventArgs> result =
            await message.WaitForButtonAsync(context.User).ConfigureAwait(false);
        if (result.TimedOut || result.Result.Id == cancelId)
        {
            return new MigrationCanceledState(Conversation);
        }

        return new MigrationUploadState(Conversation);
    }
}
