using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Hammer.Exceptions;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.AutocompleteProviders;

/// <summary>
///     Provides autocomplete suggestions for rules.
/// </summary>
internal sealed class RuleAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var ruleService = context.Services.GetRequiredService<RuleService>();
        string query = context.OptionValue?.ToString() ?? string.Empty;
        IReadOnlyList<Rule> rules = ruleService.GetGuildRules(context.Guild);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(rules.Select(r => new DiscordAutoCompleteChoice(GetRuleDescription(r), r.Id)));
        }

        Rule? rule;
        DiscordAutoCompleteChoice choice;

        if (int.TryParse(query, out int ruleId) && ruleService.GuildHasRule(context.Guild, ruleId))
        {
            rule = ruleService.GetRuleById(context.Guild, ruleId);
            choice = new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id);
            return Task.FromResult(new[] {choice}.AsEnumerable());
        }

        rule = ruleService.SearchForRule(context.Guild, query);
        if (rule is null)
        {
            return Task.FromResult(rules.Select(r => new DiscordAutoCompleteChoice(GetRuleDescription(r), r.Id)));
        }

        choice = new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id);
        return Task.FromResult(new[] {choice}.AsEnumerable());
    }

    private static string GetRuleDescription(Rule rule)
    {
        return $"{rule.Id}: {rule.Brief ?? rule.Description}";
    }
}
