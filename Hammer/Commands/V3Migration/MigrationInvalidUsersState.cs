using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Hammer.Data.v3_compat;
using Hammer.Interactivity;
using Hammer.Services;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable CS0618

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationInvalidUsersState : ConversationState
{
    private readonly List<UserData> _userDataEntries;
    private readonly V3ToV4UpgradeService _upgradeService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationInvalidUsersState" /> class.
    /// </summary>
    /// <param name="userDataEntries">The legacy <see cref="UserData" /> entries.</param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationInvalidUsersState(List<UserData> userDataEntries, Conversation conversation)
        : base(conversation)
    {
        _userDataEntries = userDataEntries;
        _upgradeService = Conversation.Services.GetRequiredService<V3ToV4UpgradeService>();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        var builder = new DiscordWebhookBuilder();
        builder.Clear();
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Processing Users");
        embed.WithDescription("Hammer needs to validate the users in your database.\nThis process may take several minutes.\n\n" +
                              "You'll receive a notification if your intervention is required.");

        builder.AddEmbed(embed);
        await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);

        var forceInvalid = false;
        int invalidUserCount = await _upgradeService.GetInvalidUserCountAsync(_userDataEntries).ConfigureAwait(false);

        if (invalidUserCount > 0)
        {
            embed.WithTitle("Force Invalid Users");
            embed.WithDescription(
                $"The v3 database you have provided contains infractions for **{"user".ToQuantity(invalidUserCount)}** which could not be retrieved.\n" +
                "This usually means means that the user no longer exists, and has been permanently deleted - but it may also indicate " +
                "an error in the user retrieval process.\n\n" +
                "Would you like to migrate the infractions for these users?");

            string forceInvalidId = $"conv-{Conversation.Id:N}-forceinvalid";
            string noForceId = $"conv-{Conversation.Id}-noforce";
            string cancelId = $"conv-{Conversation.Id:N}-cancel";

            var buttons = new List<DiscordButtonComponent>
            {
                new(ButtonStyle.Success, forceInvalidId, "Yes", emoji: new DiscordComponentEmoji(948187925449961482)),
                new(ButtonStyle.Danger, noForceId, "No", emoji: new DiscordComponentEmoji(948187924120342538)),
                new(ButtonStyle.Danger, cancelId, "Cancel", emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🛑")))
            };

            builder.Clear();
            builder.WithContent(context.User.Mention);
            builder.AddComponents(buttons);
            builder.AddEmbed(embed);

            DiscordMessage message = await context.Interaction!.EditOriginalResponseAsync(builder).ConfigureAwait(false);

            InteractivityResult<ComponentInteractionCreateEventArgs> result =
                await message.WaitForButtonAsync(context.User).ConfigureAwait(false);

            if (result.TimedOut || result.Result.Id == cancelId)
                return new MigrationCanceledState(Conversation);

            forceInvalid = result.Result.Id == forceInvalidId;
        }

        return new MigrationProcessInfractionsState(_userDataEntries, forceInvalid, Conversation);
    }
}
