using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        return Task.FromResult(rules.Select(rule => new DiscordAutoCompleteChoice(GetRuleDescription(rule), rule.Id)));
    }

    private static string GetRuleDescription(Rule rule)
    {
        return $"{rule.Id}: {rule.Brief ?? rule.Description}";
    }
}
