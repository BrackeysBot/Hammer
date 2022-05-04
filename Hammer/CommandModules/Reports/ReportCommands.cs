using DSharpPlus.SlashCommands;
using Hammer.Services;

namespace Hammer.CommandModules.Reports;

/// <summary>
///     Represents a class which implements application commands for message reporting.
/// </summary>
internal sealed partial class ReportCommands : ApplicationCommandModule
{
    private readonly MessageReportService _reportService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportCommands" /> class.
    /// </summary>
    /// <param name="reportService">The message report service.</param>
    public ReportCommands(MessageReportService reportService)
    {
        _reportService = reportService;
    }
}
