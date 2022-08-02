using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Interactivity;
using X10D.Text;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("edit", "Edits a rule.", false)]
    [SlashRequireGuild]
    public async Task EditAsync(InteractionContext context,
        [Autocomplete(typeof(RuleAutocompleteProvider))] [Option("rule", "The rule to modify")] long ruleId)
    {
        DiscordGuild guild = context.Guild;

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        if (!_ruleService.GuildHasRule(guild, (int) ruleId))
        {
            DiscordEmbed embed = _ruleService.CreateRuleNotFoundEmbed(guild, (int) ruleId);
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        Rule rule = _ruleService.GetRuleById(guild, (int) ruleId);
        string? oldBrief = rule.Brief?.AsNullIfWhiteSpace();
        string? oldDescription = rule.Description.AsNullIfWhiteSpace();

        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Add Rule");
        DiscordModalTextInput brief = modal.AddInput("Brief Description",
            "e.g. Be respectful",
            initialValue: rule.Brief?.AsNullIfWhiteSpace(),
            isRequired: false);
        DiscordModalTextInput description = modal.AddInput("Description",
            "e.g. Please treat other members with respect. Refrain from verbal insults and attacks.",
            initialValue: rule.Description.AsNullIfWhiteSpace(),
            isRequired: true,
            inputStyle: TextInputStyle.Paragraph);

        DiscordModalResponse response =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

        if (response == DiscordModalResponse.Success)
        {
            string? newBrief = brief.Value?.AsNullIfWhiteSpace();
            string? newDescription = description.Value?.AsNullIfWhiteSpace();
            var changed = false;

            if (!string.Equals(oldBrief, newBrief) && (changed = true))
                await _ruleService.SetRuleBriefAsync(rule, newBrief).ConfigureAwait(false);

            if (!string.Equals(oldDescription, newDescription) && (changed = true))
                await _ruleService.SetRuleContentAsync(rule, newDescription!).ConfigureAwait(false);

            DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);

            if (changed)
            {
                embed.WithColor(DiscordColor.Green);
                embed.WithTitle($"Rule #{rule.Id} updated");
            }
            else
            {
                embed.WithColor(DiscordColor.Orange);
                embed.WithTitle($"Rule #{rule.Id} unchanged");
                embed.WithDescription("No changes were made to the rule.");
            }

            if (string.IsNullOrWhiteSpace(brief.Value))
                embed.WithDescription(rule.Description);
            else
                embed.AddField(rule.Brief, rule.Description);

            var webhook = new DiscordWebhookBuilder();
            webhook.AddEmbed(embed);
            await context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed)).ConfigureAwait(false);
        }
    }
}
