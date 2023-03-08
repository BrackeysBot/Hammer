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
        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(context.Guild);

        if (infractions.Count == 0)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("No infractions on record");
            embed.WithDescription("Statistics cannot be generated because there are no infractions on record.");

            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("Guild is not configured!", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);
        DiscordEmbed result = await _infractionStatisticsService.CreateStatisticsEmbedAsync(context.Guild).ConfigureAwait(false);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(result);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
