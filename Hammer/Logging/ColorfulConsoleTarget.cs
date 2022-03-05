using System.Text;
using NLog;
using NLog.Targets;

namespace Hammer.Logging;

/// <summary>
///     Represents an NLog target which supports colorful output to stdout.
/// </summary>
internal sealed class ColorfulConsoleTarget : TargetWithLayout
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ColorfulConsoleTarget" /> class.
    /// </summary>
    /// <param name="name">The name of the log target.</param>
    public ColorfulConsoleTarget(string name)
    {
        Name = name;
    }

    /// <inheritdoc />
    protected override void Write(LogEventInfo logEvent)
    {
        var message = new StringBuilder();

        message.Append(Layout.Render(logEvent));

        if (logEvent.Level == LogLevel.Warn)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
        }
        else if (logEvent.Level == LogLevel.Error || logEvent.Level == LogLevel.Fatal)
        {
            Console.ForegroundColor = ConsoleColor.Red;
        }

        if (logEvent.Exception is { } exception)
        {
            message.Append($": {exception}");
        }

        Console.WriteLine(message);
        Console.ResetColor();
    }
}
