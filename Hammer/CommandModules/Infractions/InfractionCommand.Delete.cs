using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Resources;

namespace Hammer.CommandModules.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("delete", "Deletes an infraction.", false)]
    [SlashRequireGuild]
    public async Task DeleteAsync(InteractionContext context,
        [Autocomplete(typeof(InfractionAutocompleteProvider))] [Option("infraction", "The infraction to delete.")]
        long infractionId)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        var embed = new DiscordEmbedBuilder();

        Infraction? infraction = _infractionService.GetInfraction(infractionId);
        if (infraction is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("Infraction not found");
            embed.WithDescription($"The infraction with the ID `{infractionId}` was not found.");
        }
        else
        {
            embed.WithColor(0x00FF00);
            embed.WithTitle(EmbedTitles.InfractionRedacted);
            embed.WithDescription(EmbedMessages.InfractionRedacted);
            await _infractionService.RemoveInfractionAsync(infraction);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
