using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using X10D.DSharpPlus;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("copy", "Copies all infractions from one user to another.", false)]
    [SlashRequireGuild]
    public async Task CopyAsync(InteractionContext context,
        [Option("source", "The user whose infractions to copy.")] DiscordUser source,
        [Option("destination", "The user who will acquire the copied infractions.")] DiscordUser destination)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        IEnumerable<Infraction> infractions = _infractionService.EnumerateInfractions(source, context.Guild);
        List<Infraction> copies = infractions.Select(infraction => new Infraction(infraction) {UserId = destination.Id}).ToList();

        await _infractionService.AddInfractionsAsync(copies).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(destination);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Infractions Copied");
        embed.WithDescription($"{copies.Count} infractions for {source.Mention} have been copied to {destination.Mention}.");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
