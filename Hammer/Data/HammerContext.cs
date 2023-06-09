using Hammer.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Hammer.Data;

/// <summary>
///     Represents a session with the <c>hammer.db</c> database.
/// </summary>
internal sealed class HammerContext : DbContext
{
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

        if (Environment.GetEnvironmentVariable("USE_MYSQL") == "1")
        {
            string host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
            string port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
            string user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? string.Empty;
            string password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? string.Empty;
            string database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "hammer";

            var connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};";
            ServerVersion version = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, version, options => options.EnableRetryOnFailure());
        }
        else
        {
            optionsBuilder.UseSqlite("Data Source='data/hammer.db'");
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AltAccountConfiguration());
        modelBuilder.ApplyConfiguration(new BlockedReporterConfiguration());
        modelBuilder.ApplyConfiguration(new DeletedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InfractionConfiguration());
        modelBuilder.ApplyConfiguration(new MemberNoteConfiguration());
        modelBuilder.ApplyConfiguration(new MuteConfiguration());
        modelBuilder.ApplyConfiguration(new StaffMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ReportedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TemporaryBanConfiguration());
        modelBuilder.ApplyConfiguration(new TrackedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
    }
}
