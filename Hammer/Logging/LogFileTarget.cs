using System.Text;
using Hammer.Services;
using NLog;
using NLog.Targets;

namespace Hammer.Logging;

/// <summary>
///     Represents an NLog target which writes its output to a log file on disk.
/// </summary>
internal sealed class LogFileTarget : TargetWithLayout
{
    private readonly LoggingService _loggingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LogFileTarget" /> class.
    /// </summary>
    /// <param name="name">The name of the log target.</param>
    /// <param name="loggingService">The <see cref="LoggingService" />.</param>
    public LogFileTarget(string name, LoggingService loggingService)
    {
        _loggingService = loggingService;
        Name = name;
    }

    /// <inheritdoc />
    protected override void Write(LogEventInfo logEvent)
    {
        _loggingService.ArchiveLogFilesAsync(false).GetAwaiter().GetResult();

        using FileStream stream = _loggingService.LogFile.Open(FileMode.Append, FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(Layout.Render(logEvent));

        if (logEvent.Exception is { } exception)
            writer.Write($": {exception}");

        writer.WriteLine();
    }
}
