using DisCatSharp.CommandsNext;
using Hammer.Services;

namespace Hammer.CommandModules.Rules;

/// <summary>
///     Represents a module which implements rule-related commands.
/// </summary>
internal sealed partial class RulesModule : BaseCommandModule
{
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RulesModule" /> class.
    /// </summary>
    public RulesModule(RuleService ruleService)
    {
        _ruleService = ruleService;
    }
}
