using Hammer.Configuration;
using Hammer.Data.EntityConfigurations;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using MuteConfiguration = Hammer.Data.EntityConfigurations.MuteConfiguration;

namespace Hammer.Data;

/// <summary>
///     Represents a session with the <c>hammer.db</c> database.
/// </summary>
internal sealed class HammerContext : DbContext
{
    private readonly ILogger<HammerContext> _logger;
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HammerContext" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The configuration service.</param>
    public HammerContext(ILogger<HammerContext> logger, ConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    /// <summary>
    ///     Gets a value indicating whether this instance is using MySQL as its database provider.
    /// </summary>
    /// <value><see langword="true" /> if MySQL is being used; otherwise, <see langword="false" />.</value>
    public bool IsMySql { get; private set; }

    /// <summary>
    ///     Gets the set of alt accounts.
    /// </summary>
    /// <value>The set of alt accounts.</value>
    public DbSet<AltAccount> AltAccounts { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of users who are blocked from making reports.
    /// </summary>
    /// <value>The set of blocked reporters.</value>
    public DbSet<BlockedReporter> BlockedReporters { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of staff-deleted messages.
    /// </summary>
    /// <value>The set of staff-deleted messages.</value>
    public DbSet<DeletedMessage> DeletedMessages { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of infractions.
    /// </summary>
    /// <value>The set of infractions.</value>
    public DbSet<Infraction> Infractions { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of member notes.
    /// </summary>
    /// <value>The set of member notes.</value>
    public DbSet<MemberNote> MemberNotes { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of mutes.
    /// </summary>
    /// <value>The set of mutes.</value>
    public DbSet<Mute> Mutes { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of reported messages.
    /// </summary>
    /// <value>The set of reported messages.</value>
    public DbSet<ReportedMessage> ReportedMessages { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of rules.
    /// </summary>
    /// <value>The set of rules.</value>
    public DbSet<Rule> Rules { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of staff messages.
    /// </summary>
    /// <value>The set of staff messages.</value>
    public DbSet<StaffMessage> StaffMessages { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of temporary bans.
    /// </summary>
    /// <value>The set of temporary bans.</value>
    public DbSet<TemporaryBan> TemporaryBans { get; private set; } = null!;

    /// <summary>
    ///     Gets the set of tracked messages.
    /// </summary>
    /// <value>The set of tracked messages.</value>
    public DbSet<TrackedMessage> TrackedMessages { get; private set; } = null!;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        DatabaseConfiguration databaseConfiguration = _configurationService.BotConfiguration.Database;
        switch (databaseConfiguration.Provider)
        {
            case "mysql":
                IsMySql = true;
                _logger.LogTrace("Using MySQL/MariaDB database provider");
                var connectionStringBuilder = new MySqlConnectionStringBuilder
                {
                    Server = databaseConfiguration.Host,
                    Port = (uint)(databaseConfiguration.Port ?? 3306),
                    Database = databaseConfiguration.Database,
                    UserID = databaseConfiguration.Username,
                    Password = databaseConfiguration.Password
                };

                var connectionString = connectionStringBuilder.ToString();
                ServerVersion version = ServerVersion.AutoDetect(connectionString);

                _logger.LogTrace("Server version is {Version}", version);
                optionsBuilder.UseMySql(connectionString, version);
                break;

            default:
                _logger.LogTrace("Using SQLite database provider");
                optionsBuilder.UseSqlite("Data Source='data/hammer.db'");
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AltAccountConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new BlockedReporterConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new DeletedMessageConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new InfractionConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new MemberNoteConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new MuteConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new StaffMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ReportedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TemporaryBanConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new TrackedMessageConfiguration(IsMySql));
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
    }
}
