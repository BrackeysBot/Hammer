using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.Data.v3_compat;
using Hammer.Interactivity;

#pragma warning disable CS0618

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationConfirmState : ConversationState
{
    private readonly List<UserData> _userDataEntries;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationConfirmState" /> class.
    /// </summary>
    /// <param name="userDataEntries">The legacy <see cref="UserData" /> entries.</param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationConfirmState(IEnumerable<UserData> userDataEntries, Conversation conversation)
        : base(conversation)
    {
        _userDataEntries = userDataEntries.ToList();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        int userCount = _userDataEntries.Count;
        int infractionCount = _userDataEntries.Sum(u => u.Infractions.Count);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0x007EC6);
        embed.WithThumbnail(context.Client.CurrentUser.GetAvatarUrl(ImageFormat.Png));
        embed.WithTitle("Infractions Received");
        embed.WithDescription(
            $"This infraction database contains **{userCount:N0} users** totalling **{infractionCount:N0} infractions**.\n\n" +
            "To confirm migration of this data, please hit Confirm.");

        string confirmId = $"conv-{Conversation.Id:N}-retry";
        string cancelId = $"conv-{Conversation.Id:N}-cancel";

        var buttons = new List<DiscordButtonComponent>
        {
            new(ButtonStyle.Success, confirmId, "Confirm", emoji: new DiscordComponentEmoji(948187925449961482)),
            new(ButtonStyle.Danger, cancelId, "Cancel", emoji: new DiscordComponentEmoji(948187924120342538))
        };

        var builder = new DiscordWebhookBuilder();
        builder.Clear();
        builder.AddEmbed(embed);
        builder.AddComponents(buttons);

        DiscordMessage message = await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);
        InteractivityResult<ComponentInteractionCreateEventArgs> result =
            await message.WaitForButtonAsync(context.User).ConfigureAwait(false);

        if (result.TimedOut || result.Result.Id == cancelId)
            return new MigrationCanceledState(Conversation);

        return new MigrationInvalidUsersState(_userDataEntries, Conversation);
    }
}
