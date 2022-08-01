using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hammer.Services;

/// <summary>
///     Represents a service which connects to the Hammer database.
/// </summary>
internal sealed class DatabaseService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseService" /> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory.</param>
    public DatabaseService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return CreateDatabaseAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        Directory.CreateDirectory("data");

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        Logger.Info("Creating database...");
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        Logger.Info("Applying migrations...");
        await context.Database.MigrateAsync().ConfigureAwait(false);
    }
}
