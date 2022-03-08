using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Hammer.Logging;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Layouts;

namespace Hammer.Services;

/// <summary>
///     Represents a class which implements a logging service that supports multiple log targets.
/// </summary>
/// <remarks>
///     This class implements a logging structure similar to that of Minecraft, where historic logs are compressed to a .gz and
///     the latest log is found in <c>logs/latest.log</c>.
/// </remarks>
internal sealed class LoggingService : BackgroundService
{
    private const string LogFileName = "logs/latest.log";

    /// <summary>
    ///     Initializes a new instance of the <see cref="LoggingService" /> class.
    /// </summary>
    public LoggingService()
    {
        LogFile = new FileInfo(LogFileName);
    }

    /// <summary>
    ///     Gets or sets the log file.
    /// </summary>
    /// <value>The log file.</value>
    public FileInfo LogFile { get; set; }

    /// <summary>
    ///     Archives any existing log files.
    /// </summary>
    public async Task ArchiveLogFilesAsync(bool archiveToday = true)
    {
        var latestFile = new FileInfo(LogFile.FullName);
        if (!latestFile.Exists)
        {
            return;
        }

        DateTime lastWrite = latestFile.LastWriteTime;
        string lastWriteDate = $"{lastWrite:yyyy-MM-dd}";
        var version = 0;
        string name;

        if (!archiveToday && lastWrite.Date == DateTime.Today)
        {
            return;
        }

        while (File.Exists(name = Path.Combine(LogFile.Directory!.FullName, $"{lastWriteDate}-{++version}.log.gz")))
        {
            // body ignored
        }

        await using (FileStream source = latestFile.OpenRead())
        {
            await using FileStream output = File.Create(name);
            await using var gzip = new GZipStream(output, CompressionMode.Compress);
            await source.CopyToAsync(gzip);
        }

        latestFile.Delete();
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogFile.Directory?.Create();

        LayoutRenderer.Register("TheTime", info => info.TimeStamp.ToString("HH:mm:ss"));

        Layout? layout = Layout.FromString("[${TheTime} ${level:uppercase=true}] ${message}");
        var config = new LoggingConfiguration();
        var fileLogger = new LogFileTarget("FileLogger", this) {Layout = layout};
        var consoleLogger = new ColorfulConsoleTarget("ConsoleLogger") {Layout = layout};

#if DEBUG
        LogLevel minLevel = LogLevel.Debug;
#else
        LogLevel minLevel = LogLevel.Info;
#endif
        config.AddRule(minLevel, LogLevel.Fatal, consoleLogger);
        config.AddRule(minLevel, LogLevel.Fatal, fileLogger);

        LogManager.Configuration = config;

        return ArchiveLogFilesAsync();
    }
}
