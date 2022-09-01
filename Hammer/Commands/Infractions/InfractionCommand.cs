using DSharpPlus.SlashCommands;
using Hammer.Services;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a module which implements infraction commands.
/// </summary>
[SlashCommandGroup("infraction", "Manage infractions.", false)]
internal sealed partial class InfractionCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionCommand" /> class.
    /// </summary>
    public InfractionCommand(
        ConfigurationService configurationService,
        InfractionService infractionService,
        RuleService ruleService
    )
    {
        _configurationService = configurationService;
        _infractionService = infractionService;
        _ruleService = ruleService;
    }
}
