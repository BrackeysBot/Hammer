using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("stats", "View infraction stats.", false)]
    [SlashRequireGuild]
    public async Task StatsAsync(InteractionContext context)
    {
        await context.DeferAsync().ConfigureAwait(false);
        var embed = new DiscordEmbedBuilder();

        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(context.Guild);

        if (infractions.Count == 0)
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("No infractions on record");
            embed.WithDescription("Statistics cannot be generated because there are no infractions on record.");
        }
        else
        {
            if (_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
                embed.WithColor(guildConfiguration.PrimaryColor);
            else
                embed.WithColor(DiscordColor.Purple);

            embed.WithTitle("Infraction Statistics");
            embed.AddField("Total Infractions", infractions.Count.ToString("N0"), true);
            embed.AddField("Infracted Users", infractions.DistinctBy(i => i.UserId).Count().ToString("N0"), true);
            embed.AddField("Warnings", infractions.Count(i => i.Type is InfractionType.Warning).ToString("N0"), true);
            embed.AddField("Mutes", infractions.Count(i => i.Type is InfractionType.TemporaryMute or InfractionType.Mute).ToString("N0"), true);
            embed.AddField("Bans", infractions.Count(i => i.Type is InfractionType.TemporaryBan or InfractionType.Ban).ToString("N0"), true);
            embed.AddField("Kicks", infractions.Count(i => i.Type is InfractionType.Kick).ToString("N0"), true);
            embed.AddField("Gags", infractions.Count(i => i.Type is InfractionType.Gag).ToString("N0"), true);
            embed.AddField("Messages Deleted", infractions.Count(i => i.Type is InfractionType.MessageDeletion).ToString("N0"), true);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
