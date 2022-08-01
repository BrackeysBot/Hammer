using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.Interactivity;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationWelcomeState : ConversationState
{
    private readonly bool _fullMigration;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationWelcomeState" /> class.
    /// </summary>
    /// <param name="fullMigration">Whether or not to perform a full migration.</param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationWelcomeState(bool fullMigration, Conversation conversation) : base(conversation)
    {
        _fullMigration = fullMigration;
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        string startId = $"conv-{Conversation.Id:N}-start";
        string cancelId = $"conv-{Conversation.Id:N}-cancel";

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0x007EC6);
        embed.WithThumbnail(context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png));
        embed.WithTitle("v3 -> v4 Infraction Migration");
        embed.WithDescription("Welcome to the Hammer migration process.\n\n" +
                              "This process will migrate all v3 infractions to the Hammer database, and may take several minutes.\n\n" +
                              "When you are ready, click the button below to begin the migration.\n\n" +
                              "If at any point you would like to cancel the migration, hit Cancel.");

        var buttons = new List<DiscordButtonComponent>
        {
            new(ButtonStyle.Success, startId, "Proceed", emoji: new DiscordComponentEmoji(948187925449961482)),
            new(ButtonStyle.Danger, cancelId, "Cancel", emoji: new DiscordComponentEmoji(948187924120342538))
        };

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        builder.AddComponents(buttons);

        DiscordMessage response = await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);
        InteractivityResult<ComponentInteractionCreateEventArgs> result =
            await response.WaitForButtonAsync(context.User).ConfigureAwait(false);

        if (result.TimedOut || result.Result.Id == cancelId)
            return new MigrationCanceledState(Conversation);

        if (result.Result.Id == startId)
            return new MigrationUploadState(_fullMigration, Conversation);

        return null;
    }
}
