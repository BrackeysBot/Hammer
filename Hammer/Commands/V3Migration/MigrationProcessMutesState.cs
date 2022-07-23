#pragma warning disable CS0618
using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Data;
using Hammer.Data.v3_compat;
using Hammer.Interactivity;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;
using Infraction = Hammer.Data.v3_compat.Infraction;
using InfractionType = Hammer.Data.v3_compat.InfractionType;

namespace Hammer.Commands.V3Migration;

internal sealed class MigrationProcessMutesState : ConversationState
{
    private readonly List<UserData> _userDataEntries;
    private readonly bool _forceInvalid;
    private readonly MuteService _muteService;
    private int _completed;
    private int _total;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationProcessMutesState" /> class.
    /// </summary>
    /// <param name="userDataEntries">The legacy <see cref="UserData" /> entries.</param>
    /// <param name="forceInvalid">
    ///     <see langword="true" /> to force migration of invalid infractions; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="conversation">The owning conversation.</param>
    public MigrationProcessMutesState(List<UserData> userDataEntries, bool forceInvalid, Conversation conversation)
        : base(conversation)
    {
        _userDataEntries = userDataEntries;
        _forceInvalid = forceInvalid;

        _muteService = conversation.Services.GetRequiredService<MuteService>();
    }

    /// <inheritdoc />
    public override async Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken)
    {
        _total = _userDataEntries.Sum(u => u.TemporaryInfractions.Count(b => b.Type == TemporaryInfractionType.TempBan));
        _total += _userDataEntries.Sum(u => u.Infractions.Count(b => b.Type == InfractionType.Mute));

        var cancellationTokenSource = new CancellationTokenSource();
        _ = UpdateEmbedAsync(context.Client, context.Interaction!, cancellationTokenSource.Token).ConfigureAwait(false);

        foreach (UserData userData in _userDataEntries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                break;
            }

            if (userData.Invalid && !_forceInvalid)
                continue;

            IEnumerable<TemporaryInfraction> temporaryMutes =
                userData.TemporaryInfractions.Where(i => i.Type == TemporaryInfractionType.TempMute);

            foreach (TemporaryInfraction temporaryInfraction in temporaryMutes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }

                var mute = Mute.Create(userData.ID, context.Guild!.Id, temporaryInfraction.Expire);
                await _muteService.AddMuteAsync(mute).ConfigureAwait(false);
                _completed++;
            }

            IEnumerable<Infraction> permanentMutes = userData.Infractions.Where(i => i.Type == InfractionType.Mute);
            foreach (Infraction _ in permanentMutes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }

                var mute = Mute.Create(userData.ID, context.Guild!.Id);
                await _muteService.AddMuteAsync(mute).ConfigureAwait(false);
                _completed++;
            }
        }

        cancellationTokenSource.Cancel();
        return new MigrationCompletedState(Conversation);
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
            embed.WithDescription($"Migrated {_completed} / {_total} mutes");
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
