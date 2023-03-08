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
    private readonly MessageDeletionService _messageDeletionService;
    private readonly DiscordLogService _logService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionCommand" /> class.
    /// </summary>
    public InfractionCommand(
        ConfigurationService configurationService,
        DiscordLogService logService,
        InfractionService infractionService,
        MessageDeletionService messageDeletionService,
        RuleService ruleService
    )
    {
        _configurationService = configurationService;
        _infractionService = infractionService;
        _messageDeletionService = messageDeletionService;
        _logService = logService;
        _ruleService = ruleService;
    }
}
