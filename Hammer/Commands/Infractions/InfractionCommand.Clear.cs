using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("clear", "Clears all infractions from the specified user.", false)]
    [SlashRequireGuild]
    public async Task ClearAsync(InteractionContext context,
        [Option("user", "The user whose infractions to clear")] DiscordUser user)
    {
        await context.DeferAsync().ConfigureAwait(false);

        IReadOnlyList<Infraction> infractions = _infractionService.GetInfractions(user, context.Guild);
        _infractionService.RemoveInfractions(infractions);

        int infractionCount = _infractionService.GetInfractionCount(user, context.Guild);
        int differential = infractions.Count - infractionCount;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Infractions cleared");
        embed.WithDescription($"Cleared {"infraction".ToQuantity(differential)} infractions " +
                              $"for {user.Mention}.");

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);

        if (differential > 0)
        {
            embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Infractions Cleared");
            embed.AddField("User", user.Mention, true);
            embed.AddField("Count", differential, true);
            embed.AddField("Staff Member", context.Member.Mention, true);
            await _logService.LogAsync(context.Guild, embed).ConfigureAwait(false);
        }
    }
}
