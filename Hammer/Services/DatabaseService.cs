using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hammer.Services;

/// <summary>
///     Represents a service which connects to the Hammer database.
/// </summary>
internal sealed class DatabaseService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseService" /> class.
    /// </summary>
    /// <param name="dbContextFactory">The DbContext factory.</param>
    public DatabaseService(IDbContextFactory<HammerContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CreateDatabaseAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        Directory.CreateDirectory("data");

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        Logger.Info("Creating database...");
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        Logger.Info("Applying migrations...");
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }
}
