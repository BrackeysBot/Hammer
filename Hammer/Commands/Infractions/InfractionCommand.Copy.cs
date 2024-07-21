using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("copy", "Copies all infractions from one user to another.", false)]
    [SlashRequireGuild]
    public async Task CopyAsync(InteractionContext context,
        [Option("source", "The user whose infractions to copy.")]
        DiscordUser source,
        [Option("destination", "The user who will acquire the copied infractions.")]
        DiscordUser destination)
    {
        if (source == destination)
        {
            await context.CreateResponseAsync("You can't copy infractions to the same user.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);

        IEnumerable<Infraction> infractions = _infractionService.EnumerateInfractions(source, context.Guild);
        List<Infraction> copies = infractions.Select(infraction => new Infraction(infraction) { UserId = destination.Id })
            .ToList();

        _infractionService.AddInfractions(copies);

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(destination);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Infractions Copied");
        embed.WithDescription($"{copies.Count} infractions for {source.Mention} have been copied to {destination.Mention}.");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);

        embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle("Infractions Copied");
        embed.AddField("From", source.Mention, true);
        embed.AddField("To", destination.Mention, true);
        embed.AddField("Count", copies.Count, true);
        embed.AddField("Staff Member", context.Member.Mention, true);
        await _logService.LogAsync(context.Guild, embed).ConfigureAwait(false);
    }
}
