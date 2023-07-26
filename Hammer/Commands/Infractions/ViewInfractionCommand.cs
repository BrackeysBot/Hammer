using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Services;
using Humanizer;
using X10D.DSharpPlus;

namespace Hammer.Commands.Infractions;

internal sealed class ViewInfractionCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ViewInfractionCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public ViewInfractionCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [SlashCommand("viewinfraction", "Views an infraction.", false)]
    [SlashRequireGuild]
    public async Task ViewInfractionAsync(InteractionContext context,
        [Option("infraction", "The infraction to view.")]
        long infractionId
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
            embed.AddFieldIf(infraction.RuleId.HasValue, "Rule Broken", () => $"{infraction.RuleId} - {infraction.RuleText}",
                true);
            embed.AddFieldIf(infraction.Reason is not null, "Reason", () => infraction.Reason);
            embed.AddFieldIf(!string.IsNullOrWhiteSpace(infraction.AdditionalInformation), "Additional Information",
                () => infraction.AdditionalInformation);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
