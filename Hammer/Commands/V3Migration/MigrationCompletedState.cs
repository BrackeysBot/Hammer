using DSharpPlus.Entities;
using Hammer.Interactivity;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationCompletedState : ConversationState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationCompletedState" /> class.
    /// </summary>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationCompletedState(Conversation conversation) : base(conversation)
    {
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0x00FF00);
        embed.WithThumbnail(context.Client.CurrentUser.AvatarUrl);
        embed.WithTitle("✅ Migration complete");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);

        await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);

        return null;
    }
}
