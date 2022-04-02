using DSharpPlus.CommandsNext;
using Hammer.Services;

namespace Hammer.CommandModules.User;

/// <summary>
///     Represents a module which implements user commands.
/// </summary>
internal sealed partial class UserModule : BaseCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserModule" /> class.
    /// </summary>
    public UserModule(ConfigurationService configurationService, InfractionService infractionService)
    {
        _configurationService = configurationService;
        _infractionService = infractionService;
    }
}
