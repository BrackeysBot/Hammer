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
    private readonly IDbContextFactory<MigrationContext> _migrationContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseService" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The <see cref="HammerContext" /> factory.</param>
    /// <param name="migrationContextFactory">The <see cref="MigrationContext" /> factory.</param>
    public DatabaseService(ILogger<DatabaseService> logger,
        IDbContextFactory<HammerContext> dbContextFactory,
        IDbContextFactory<MigrationContext> migrationContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _migrationContextFactory = migrationContextFactory;
    }

    /// <summary>
    ///     Migrates the database from one source to another.
    /// </summary>
    public async Task<int> MigrateAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();

        if (!context.IsMySql)
        {
            _logger.LogWarning("Cannot migrate from SQLite to SQLite. This operation will be skipped");
            return 0;
        }

        await using MigrationContext migration = await _migrationContextFactory.CreateDbContextAsync();

        _logger.LogInformation("Migrating database");
        context.AltAccounts.AddRange(migration.AltAccounts);
        context.BlockedReporters.AddRange(migration.BlockedReporters);
        context.DeletedMessages.AddRange(migration.DeletedMessages);
        context.Infractions.AddRange(migration.Infractions);
        context.MemberNotes.AddRange(migration.MemberNotes);
        context.Mutes.AddRange(migration.Mutes);
        context.ReportedMessages.AddRange(migration.ReportedMessages);
        context.Rules.AddRange(migration.Rules);
        context.StaffMessages.AddRange(migration.StaffMessages);
        context.TemporaryBans.AddRange(migration.TemporaryBans);
        context.TrackedMessages.AddRange(migration.TrackedMessages);

        _logger.LogDebug("Saving database");
        await context.SaveChangesAsync();

        int count = 0;
        count += context.AltAccounts.Count();
        count += context.BlockedReporters.Count();
        count += context.DeletedMessages.Count();
        count += context.Infractions.Count();
        count += context.MemberNotes.Count();
        count += context.Mutes.Count();
        count += context.ReportedMessages.Count();
        count += context.Rules.Count();
        count += context.StaffMessages.Count();
        count += context.TemporaryBans.Count();
        count += context.TrackedMessages.Count();
        return count;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CreateDatabaseAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync();

        if (Environment.GetEnvironmentVariable("USE_MYSQL") != "1")
        {
            _logger.LogInformation("Creating database");
            await context.Database.EnsureCreatedAsync();
        }

        _logger.LogInformation("Applying migrations");
        await context.Database.MigrateAsync();
    }
}
