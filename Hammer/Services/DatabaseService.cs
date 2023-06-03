using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hammer.Services;

/// <summary>
///     Represents a service which connects to the Hammer database.
/// </summary>
internal sealed class DatabaseService : BackgroundService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The DbContext factory.</param>
    public DatabaseService(ILogger<DatabaseService> logger, IDbContextFactory<HammerContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CreateDatabaseAsync().ConfigureAwait(false);
    }

    private async Task CreateDatabaseAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (Environment.GetEnvironmentVariable("USE_MYSQL") != "1")
        {
            _logger.LogInformation("Creating database");
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }

        _logger.LogInformation("Applying migrations");
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }
}
