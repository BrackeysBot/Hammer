using Hammer.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MuteConfiguration = Hammer.Data.EntityConfigurations.MuteConfiguration;

namespace Hammer.Data;

/// <summary>
///     Represents a session with the <c>hammer.db</c> database.
/// </summary>
internal sealed class MigrationContext : DbContext
{
    private readonly ILogger<MigrationContext> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrationContext" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public MigrationContext(ILogger<MigrationContext> logger)
    {
        _logger = logger;
    }

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

        _logger.LogDebug("Using SQLite database provider");
        optionsBuilder.UseSqlite("Data Source='data/hammer.db'");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AltAccountConfiguration(false));
        modelBuilder.ApplyConfiguration(new BlockedReporterConfiguration(false));
        modelBuilder.ApplyConfiguration(new DeletedMessageConfiguration(false));
        modelBuilder.ApplyConfiguration(new InfractionConfiguration(false));
        modelBuilder.ApplyConfiguration(new MemberNoteConfiguration(false));
        modelBuilder.ApplyConfiguration(new MuteConfiguration(false));
        modelBuilder.ApplyConfiguration(new StaffMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ReportedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TemporaryBanConfiguration(false));
        modelBuilder.ApplyConfiguration(new TrackedMessageConfiguration(false));
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
    }
}
