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

            var totalInfractions = infractions.Count.ToString("N0");
            var infractedUsers = infractions.DistinctBy(i => i.UserId).Count().ToString("N0");
            var warnings = infractions.Count(i => i.Type is InfractionType.Warning).ToString("N0");
            var gags = infractions.Count(i => i.Type is InfractionType.Gag).ToString("N0");
            var kicks = infractions.Count(i => i.Type is InfractionType.Kick).ToString("N0");
            var messagesDeleted = (await _messageDeletionService.CountMessageDeletionsAsync(context.Guild)).ToString("N0");
            
            int tempMuteCount = infractions.Count(i => i.Type is InfractionType.TemporaryMute);
            int muteCount = infractions.Count(i => i.Type is InfractionType.Mute);
            string mutes = $"{muteCount + tempMuteCount} ({tempMuteCount}T / {muteCount}P)";
            
            int tempBanCount = infractions.Count(i => i.Type is InfractionType.TemporaryBan);
            int banCount = infractions.Count(i => i.Type is InfractionType.Ban);
            string bans = $"{banCount + tempBanCount} ({tempBanCount}T / {banCount}P)";

            embed.WithTitle("Infraction Statistics");
            embed.AddField("Total Infractions", totalInfractions, true);
            embed.AddField("Infracted Users", infractedUsers, true);
            embed.AddField("Warnings", warnings, true);
            embed.AddField("Mutes", mutes, true);
            embed.AddField("Bans", bans, true);
            embed.AddField("Kicks", kicks, true);
            embed.AddField("Gags", gags, true);
            embed.AddField("Messages Deleted", messagesDeleted, true);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
