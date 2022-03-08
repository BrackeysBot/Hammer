using DisCatSharp.CommandsNext;
using Hammer.Services;

namespace Hammer.CommandModules.Staff;

/// <summary>
///     Represents a module which implements staff commands.
/// </summary>
internal sealed partial class StaffModule : BaseCommandModule
{
    private readonly MessageService _messageService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffModule" /> class.
    /// </summary>
    public StaffModule(MessageService messageService)
    {
        _messageService = messageService;
    }
}
