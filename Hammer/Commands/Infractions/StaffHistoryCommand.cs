using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Services;
using Humanizer;
using X10D.DSharpPlus;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>staffhistory</c> command.
/// </summary>
internal sealed class StaffHistoryCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffHistoryCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public StaffHistoryCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [SlashCommand("staffhistory", "Searches a staff member's history.", false)]
    [SlashRequireGuild]
    public async Task StaffHistoryAsync(InteractionContext context,
        [Option("staffMember", "The staff member whose infractions to search.")] DiscordUser user)
    {
        IEnumerable<Infraction> infractions =
            _infractionService.GetInfractions(context.Guild).Where(i => i.StaffMemberId == user.Id);
        Infraction[] staffInfractions =
            infractions.Where(i => i.StaffMemberId == user.Id).OrderByDescending(i => i.IssuedAt).ToArray();

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);
        embed.WithTitle("Staff History");
        embed.WithColor(DiscordColor.Orange);
        embed.AddField("Total Infractions", staffInfractions.Length, true);
        embed.AddField("Warnings", staffInfractions.Count(i => i.Type == InfractionType.Warning), true);
        embed.AddField("Temporary Mutes", staffInfractions.Count(i => i.Type == InfractionType.TemporaryMute), true);
        embed.AddField("Temporary Bans", staffInfractions.Count(i => i.Type == InfractionType.TemporaryBan), true);
        embed.AddField("Kicks", staffInfractions.Count(i => i.Type == InfractionType.Kick), true);
        embed.AddField("Mutes", staffInfractions.Count(i => i.Type == InfractionType.Mute), true);
        embed.AddField("Bans", staffInfractions.Count(i => i.Type == InfractionType.Ban), true);

        var builder = new StringBuilder();
        int upperBound = Math.Min(10, staffInfractions.Length);

        for (var index = 0; index < upperBound; index++)
        {
            Infraction infraction = staffInfractions[index];
            builder.Append($"**ID: {infraction.Id}** • ");
            builder.Append($"{infraction.Type.Humanize()} • ");
            if (!string.IsNullOrWhiteSpace(infraction.Reason))
            {
                builder.Append($"{infraction.Reason} • ");
            }

            builder.AppendLine(Formatter.Timestamp(infraction.IssuedAt));
        }

        embed.WithDescription($"**Last 10 Infractions**\n{builder}");
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
