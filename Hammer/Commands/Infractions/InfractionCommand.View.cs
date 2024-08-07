using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;

namespace Hammer.Commands.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("view", "Views an infraction.", false)]
    [SlashRequireGuild]
    public async Task ViewAsync(InteractionContext context,
        [Option("infraction", "The infraction to view.")] long infractionId
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
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle($"Infraction {infraction.Id}");
            embed.AddField("User", MentionUtility.MentionUser(infraction.UserId), true);
            embed.AddField("Type", infraction.Type.Humanize(), true);
            embed.AddField("Staff Member", MentionUtility.MentionUser(infraction.StaffMemberId), true);
            embed.AddField("Issued", Formatter.Timestamp(infraction.IssuedAt), true);
            embed.AddFieldIf(infraction.RuleId.HasValue, "Rule Broken", () => $"{infraction.RuleId} - {infraction.RuleText}", true);
            embed.AddFieldIf(infraction.Reason is not null, "Reason", () => infraction.Reason);
            embed.AddFieldIf(!string.IsNullOrWhiteSpace(infraction.AdditionalInformation), "Additional Information", () => infraction.AdditionalInformation);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
