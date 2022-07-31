using DSharpPlus.SlashCommands;
using Hammer.Services;

namespace Hammer.Commands.Rules;

/// <summary>
///     Represents a class which implements the <c>rules</c> command.
/// </summary>
[SlashCommandGroup("rules", "Manage rules.", false)]
internal sealed partial class RulesCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RulesCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="ruleService">The rule service.</param>
    public RulesCommand(ConfigurationService configurationService, RuleService ruleService)
    {
        _configurationService = configurationService;
        _ruleService = ruleService;
    }
}
