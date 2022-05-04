using System.Threading.Tasks;
using BrackeysBot.API;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;

namespace Hammer.CommandModules.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("view", "Views an infraction.", false)]
    [SlashRequireGuild]
    public async Task ViewAsync(InteractionContext context,
        [Autocomplete(typeof(InfractionAutocompleteProvider))] [Option("infraction", "The infraction to view.")]
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
            Rule? rule = null;
            if (infraction.RuleId is { } ruleId)
                rule = _ruleService.GetRuleById(context.Guild, ruleId);

            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle($"Infraction {infraction.Id}");
            embed.AddField("User", MentionUtility.MentionUser(infraction.UserId), true);
            embed.AddField("Type", infraction.Type.ToString("G"), true);
            embed.AddField("Staff Member", MentionUtility.MentionUser(infraction.StaffMemberId), true);
            embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
            embed.AddFieldIf(infraction.Reason is not null, "Reason", () => infraction.Reason);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
