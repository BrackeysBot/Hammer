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

            int totalInfractions = infractions.Count;
            Infraction[] distinctUserInfractions = infractions.DistinctBy(i => i.UserId).ToArray();

            int infractedUsers = distinctUserInfractions.Length;
            int warnedUsers = distinctUserInfractions.Count(i => i.Type == InfractionType.Warning);
            int mutedUsers = distinctUserInfractions.Count(i => i.Type is InfractionType.Mute or InfractionType.TemporaryMute);
            int bannedUsers = distinctUserInfractions.Count(i => i.Type is InfractionType.Ban or InfractionType.TemporaryBan);
            int kickedUsers = distinctUserInfractions.Count(i => i.Type is InfractionType.Kick);
            int gaggedUsers = distinctUserInfractions.Count(i => i.Type is InfractionType.Gag);

            int warnings = infractions.Count(i => i.Type is InfractionType.Warning);
            int gags = infractions.Count(i => i.Type is InfractionType.Gag);
            int kicks = infractions.Count(i => i.Type is InfractionType.Kick);
            int messagesDeleted = await _messageDeletionService.CountMessageDeletionsAsync(context.Guild);

            int tempMuteCount = infractions.Count(i => i.Type is InfractionType.TemporaryMute);
            int muteCount = infractions.Count(i => i.Type is InfractionType.Mute);
            var mutes = $"{muteCount + tempMuteCount} ({tempMuteCount}T / {muteCount}P)";

            int tempBanCount = infractions.Count(i => i.Type is InfractionType.TemporaryBan);
            int banCount = infractions.Count(i => i.Type is InfractionType.Ban);
            var bans = $"{banCount + tempBanCount} ({tempBanCount}T / {banCount}P)";

            embed.WithTitle("Infraction Statistics");
            embed.AddField("Total Infractions", totalInfractions.ToString("N0"));
            
            embed.AddField("Total Infracted Users", infractedUsers.ToString("N0"), true);
            embed.AddField("Total Warned Users", warnedUsers.ToString("N0"), true);
            embed.AddField("Total Muted Users", mutedUsers.ToString("N0"), true);
            embed.AddField("Total Banned Users", bannedUsers.ToString("N0"), true);
            embed.AddField("Total Kicked Users", kickedUsers.ToString("N0"), true);
            embed.AddField("Total Gagged Users", gaggedUsers.ToString("N0"), true);
            
            embed.AddField("Warnings", warnings.ToString("N0"), true);
            embed.AddField("Mutes", mutes, true);
            embed.AddField("Bans", bans, true);
            embed.AddField("Kicks", kicks.ToString("N0"), true);
            embed.AddField("Gags", gags.ToString("N0"), true);
            embed.AddField("Messages Deleted", messagesDeleted.ToString("N0"), true);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
