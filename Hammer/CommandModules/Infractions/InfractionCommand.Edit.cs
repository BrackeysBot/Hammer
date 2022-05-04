using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;

namespace Hammer.CommandModules.Infractions;

internal sealed partial class InfractionCommand
{
    [SlashCommand("edit", "Edits an infraction.", false)]
    [SlashRequireGuild]
    public async Task EditAsync(InteractionContext context,
        [Autocomplete(typeof(InfractionAutocompleteProvider))] [Option("infraction", "The infraction to modify.")]
        long infractionId,
        [Option("reason", "The new reason for the infraction. To remove the reason, enter a single hyphen ( - ).")]
        string? reason = null,
        [Autocomplete(typeof(RuleAutocompleteProvider))]
        [Option("rule", "The new rule which was broken. To remove the rule, enter 0.")]
        long? ruleId = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        var embed = new DiscordEmbedBuilder();
        var builder = new DiscordWebhookBuilder();

        Infraction? infraction = _infractionService.GetInfraction(infractionId);
        if (infraction is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("Infraction not found");
            embed.WithDescription($"The infraction with the ID `{infractionId}` was not found.");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        if ((reason is null || reason == infraction.Reason) && (ruleId is null || infraction.RuleId == ruleId))
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("No Changes");
            embed.WithDescription("No changes were made to the infraction.");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        if (ruleId == 0) ruleId = null;
        if (reason == "-") reason = null;

        Rule? rule = null;
        if (ruleId is not null)
        {
            rule = _ruleService.GetRuleById(context.Guild, (int) ruleId.Value);
            if (rule is null)
            {
                embed.WithColor(0xFF0000);
                embed.WithTitle("Rule not found");
                embed.WithDescription($"The rule with the ID `{ruleId}` was not found.");
                builder.AddEmbed(embed);
                await context.EditResponseAsync(builder).ConfigureAwait(false);
                return;
            }
        }

        // D#+ only accepts long, so we must cast because stupidity
        // yeah, I hate it too.
        var newRuleId = (int?) ruleId;

        embed.WithColor(DiscordColor.Green);
        await _infractionService.ModifyInfractionAsync(infraction, i =>
        {
            if (newRuleId is not null)
            {
                i.RuleId = newRuleId;
                embed.AddField("New Rule Broken", $"{rule!.Id} - {rule.Brief ?? rule.Content}");
            }

            if (reason is not null)
            {
                i.Reason = reason;
                embed.AddField("New Reason", reason);
            }
        }).ConfigureAwait(false);

        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
