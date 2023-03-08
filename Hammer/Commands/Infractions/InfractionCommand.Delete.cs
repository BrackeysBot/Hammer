using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using X10D.DSharpPlus;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("delete", "Deletes an infraction.", false)]
    [SlashRequireGuild]
    public async Task DeleteAsync(InteractionContext context,
        [Option("infraction", "The infraction to delete.")] long infractionId
    )
    {
        await context.DeferAsync().ConfigureAwait(false);
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
            embed.WithTitle("Infraction Redacted");
            embed.WithDescription($"{infraction.Type} #{infraction.Id} for {MentionUtility.MentionUser(infraction.UserId)} " +
                                  "has been redacted.");
            await _infractionService.RemoveInfractionAsync(infraction);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);

        if (infraction is not null)
        {
            embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Infraction Deleted");
            embed.AddField("ID", infraction.Id, true);
            embed.AddField("User", MentionUtility.MentionUser(infraction.UserId), true);
            embed.AddField("Type", infraction.Type, true);
            embed.AddField("Staff Member", context.Member.Mention, true);
            await _logService.LogAsync(context.Guild, embed).ConfigureAwait(false);
        }
    }
}
