using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Data.v3_compat;
using Hammer.Interactivity;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using Infraction = Hammer.Data.Infraction;
using InfractionType = Hammer.Data.InfractionType;
#pragma warning disable CS0618
using LegacyInfraction = Hammer.Data.v3_compat.Infraction;
using LegacyInfractionType = Hammer.Data.v3_compat.InfractionType;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationProcessInfractionsState : ConversationState
{
    private readonly List<UserData> _userDataEntries;
    private readonly InfractionService _infractionService;
    private int _completed;
    private int _total;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationProcessInfractionsState" /> class.
    /// </summary>
    /// <param name="userDataEntries">The legacy <see cref="UserData" /> entries.</param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationProcessInfractionsState(List<UserData> userDataEntries, Conversation conversation)
        : base(conversation)
    {
        _userDataEntries = userDataEntries;

        _infractionService = conversation.Services.GetRequiredService<InfractionService>();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        _total = _userDataEntries.Sum(u => u.Infractions.Count);

        var cancellationTokenSource = new CancellationTokenSource();
        _ = UpdateEmbedAsync(context.Client, context.Interaction!, cancellationTokenSource.Token).ConfigureAwait(false);

        foreach (UserData userData in _userDataEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                break;
            }

            foreach (LegacyInfraction legacyInfraction in userData.Infractions)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }

                InfractionType type = legacyInfraction.Type switch
                {
                    LegacyInfractionType.Warning => InfractionType.Warning,
                    LegacyInfractionType.TemporaryMute => InfractionType.TemporaryMute,
                    LegacyInfractionType.Mute => InfractionType.Mute,
                    LegacyInfractionType.Kick => InfractionType.Kick,
                    LegacyInfractionType.TemporaryBan => InfractionType.TemporaryBan,
                    LegacyInfractionType.Ban => InfractionType.Ban,
                    _ => throw new ArgumentOutOfRangeException(nameof(legacyInfraction),
                        $@"Unexpected infraction type {legacyInfraction.Type}")
                };

                var infraction = new Infraction
                {
                    Id = legacyInfraction.ID,
                    Type = type,
                    GuildId = context.Guild!.Id,
                    IssuedAt = legacyInfraction.Time,
                    StaffMemberId = legacyInfraction.Moderator,
                    Reason = legacyInfraction.Description,
                    UserId = userData.ID
                };
                await _infractionService.AddInfractionAsync(infraction).ConfigureAwait(false);
                _completed++;
            }
        }

        cancellationTokenSource.Cancel();
        return new MigrationProcessTemporaryBansState(_userDataEntries, Conversation);
    }

    private async Task UpdateEmbedAsync(BaseDiscordClient client, DiscordInteraction interaction,
        CancellationToken cancellationToken)
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0x007EC6);
        embed.WithThumbnail(client.CurrentUser.AvatarUrl);
        embed.WithTitle("💾 Migration in progress");
        embed.WithFooter("Please wait. This process may take several minutes.");

        var builder = new DiscordWebhookBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            float progress = (float) _completed / _total;
            embed.WithDescription($"Migrated {_completed} / {_total} infractions");
            embed.ClearFields();
            embed.AddField(progress.ToString("P"), Formatter.InlineCode(GetProgressBar(progress)));

            builder.Clear();
            builder.AddEmbed(embed);
            await interaction.EditOriginalResponseAsync(builder).ConfigureAwait(false);
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetProgressBar(float a, float b = 1)
    {
        var builder = new StringBuilder();

        if (b == 0)
        {
            a = 0;
            b = 1;
        }

        float percentage = Math.Clamp(a / b, 0, 1);
        (int quotient, int remainder) = Math.DivRem((int) (percentage * 100), 10);

        builder.Append('[');
        builder.Append(new string('█', quotient));

        if (remainder > 0)
        {
            if (remainder < 5) builder.Append('░');
            else if (remainder < 7) builder.Append('▒');
            else builder.Append('▓');
            builder.Append(new string(' ', 10 - quotient - 1));
        }
        else
        {
            builder.Append(new string(' ', 10 - quotient));
        }

        builder.Append(']');
        return builder.ToString();
    }
}
