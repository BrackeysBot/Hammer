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

        if (int.TryParse(query, out int ruleId) && ruleService.GuildHasRule(context.Guild, ruleId))
        {
            Rule rule = ruleService.GetRuleById(context.Guild, ruleId);
            var choice = new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id);
            return Task.FromResult(new[] {choice}.AsEnumerable());
        }

        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            Rule? rule = ruleService.SearchForRule(context.Guild, query);
            if (rule is not null)
            {
                var choice = new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id);
                return Task.FromResult(new[] {choice}.AsEnumerable());
            }
        }

        IReadOnlyList<Rule> rules = ruleService.GetGuildRules(context.Guild);
        return Task.FromResult(rules.Select(rule => new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id)));
    }

    private static string GetRuleDescription(Rule rule)
    {
        return $"{rule.Id}: {rule.Brief ?? rule.Description}";
    }
}
