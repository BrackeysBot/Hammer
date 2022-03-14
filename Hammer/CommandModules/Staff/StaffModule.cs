using DisCatSharp.CommandsNext;
using Hammer.Services;

namespace Hammer.CommandModules.Staff;

/// <summary>
///     Represents a module which implements staff commands.
/// </summary>
internal sealed partial class StaffModule : BaseCommandModule
{
    private readonly InfractionService _infractionService;
    private readonly MessageService _messageService;
    private readonly MessageReportService _reportService;
    private readonly UserTrackingService _userTrackingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffModule" /> class.
    /// </summary>
    public StaffModule(InfractionService infractionService, MessageService messageService, MessageReportService reportService,
        UserTrackingService userTrackingService)
    {
        _infractionService = infractionService;
        _messageService = messageService;
        _reportService = reportService;
        _userTrackingService = userTrackingService;
    }
}
