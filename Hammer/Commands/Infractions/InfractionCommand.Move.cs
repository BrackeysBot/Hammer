using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using X10D.DSharpPlus;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("move", "Moves all infractions from one user to another.", false)]
    [SlashRequireGuild]
    public async Task MoveAsync(InteractionContext context,
        [Option("source", "The user whose infractions to move.")] DiscordUser source,
        [Option("destination", "The user who will acquire the moved infractions.")] DiscordUser destination)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        IEnumerable<Infraction> infractions = _infractionService.EnumerateInfractions(source, context.Guild);
        var count = 0;
        foreach (Infraction infraction in infractions)
        {
            await _infractionService.ModifyInfractionAsync(infraction, i => i.UserId = destination.Id);
            count++;
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(destination);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Infractions Moved");
        embed.WithDescription($"{count} infractions for {source.Mention} have been moved to {destination.Mention}.");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
