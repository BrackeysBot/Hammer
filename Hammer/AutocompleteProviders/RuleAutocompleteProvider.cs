using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
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
        IReadOnlyList<Rule> rules = ruleService.GetGuildRules(context.Guild);

        var result = new List<DiscordAutoCompleteChoice>();
        string optionValue = context.OptionValue?.ToString() ?? string.Empty;
        bool hasOptionValue = !string.IsNullOrWhiteSpace(optionValue);

        foreach (Rule rule in rules)
        {
            string brief = rule.Brief ?? string.Empty;
            string description = rule.Description;
            if (!hasOptionValue ||
                (int.TryParse(optionValue, out int ruleId) && rule.Id == ruleId) ||
                brief.Equals(optionValue, StringComparison.OrdinalIgnoreCase) ||
                description.Equals(optionValue, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id.ToString()));
            }

            if (result.Count >= 25)
            {
                // Discord only allows 25 choices per autocomplete response
                break;
            }
        }

        return Task.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(result);
    }

    private static string GetRuleDescription(Rule rule)
    {
        string? summary = rule.Brief;
        if (string.IsNullOrWhiteSpace(summary))
        {
            summary = rule.Description;
            if (summary.Length > 50)
            {
                summary = summary[..50] + "...";
            }
        }

        return $"{rule.Id}: {summary}";
    }
}
