using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using X10D.Text;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("setbrief", "Sets the new brief for a rule.", false)]
    [SlashRequireGuild]
    public async Task SetBriefAsync(InteractionContext context,
        [Autocomplete(typeof(RuleAutocompleteProvider))] [Option("rule", "The rule to modify.")] long ruleId,
        [Option("brief", "The new rule brief.")] string brief)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);
        DiscordGuild guild = context.Guild;
        var builder = new DiscordWebhookBuilder();

        if (!_ruleService.GuildHasRule(guild, (int) ruleId))
        {
            builder.AddEmbed(_ruleService.CreateRuleNotFoundEmbed(guild, (int) ruleId));
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        Rule rule = _ruleService.GetRuleById(guild, (int) ruleId)!;
        brief = brief.AsNullIfWhiteSpace();

        await _ruleService.SetRuleBriefAsync(rule, brief).ConfigureAwait(false);

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {rule.Id} updated");

        if (string.IsNullOrWhiteSpace(rule.Brief))
            embed.WithDescription(rule.Description);
        else
            embed.AddField(rule.Brief, rule.Description);

        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }

    [SlashCommand("setdescription", "Sets the new description for a rule.", false)]
    [SlashRequireGuild]
    public async Task SetDescriptionAsync(InteractionContext context,
        [Autocomplete(typeof(RuleAutocompleteProvider))] [Option("rule", "The rule to modify.")] long ruleId,
        [Option("description", "The new rule description.")] string description)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync().ConfigureAwait(false);
        DiscordGuild guild = context.Guild;
        var builder = new DiscordWebhookBuilder();
        var embed = new DiscordEmbedBuilder();

        if (!_ruleService.GuildHasRule(guild, (int) ruleId))
        {
            builder.AddEmbed(_ruleService.CreateRuleNotFoundEmbed(guild, (int) ruleId));
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        Rule rule = _ruleService.GetRuleById(guild, (int) ruleId)!;

        if (string.IsNullOrWhiteSpace(description))
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Missing Description");
            embed.WithDescription("You must provide a description for the rule.");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);
            return;
        }

        await _ruleService.SetRuleContentAsync(rule, description).ConfigureAwait(false);

        embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithColor(0x4CAF50);
        embed.WithTitle($"Rule {rule.Id} updated");

        if (string.IsNullOrWhiteSpace(rule.Brief))
            embed.WithDescription(description);
        else
            embed.AddField(rule.Brief, description);

        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
