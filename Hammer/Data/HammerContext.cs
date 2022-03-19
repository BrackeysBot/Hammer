using Hammer.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Hammer.Data;

/// <summary>
///     Represents a session with the <c>hammer.db</c> database.
/// </summary>
internal sealed class HammerContext : DbContext
{
    private const string DataSource = "hammer.db";

    /// <summary>
    ///     Gets or sets the set of users who are blocked from making reports.
    /// </summary>
    /// <value>The set of blocked reporters.</value>
    public DbSet<BlockedReporter> BlockedReporters { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of infractions.
    /// </summary>
    /// <value>The set of infractions.</value>
    public DbSet<Infraction> Infractions { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of join/leave events for a tracked user.
    /// </summary>
    /// <value>The set of join/leave events.</value>
    public DbSet<TrackedJoinLeave> JoinLeaves { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of message edits.
    /// </summary>
    /// <value>The set of message edits.</value>
    public DbSet<MessageEdit> MessageEdits { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of reported messages.
    /// </summary>
    /// <value>The set of reported messages.</value>
    public DbSet<ReportedMessage> ReportedMessages { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of rules.
    /// </summary>
    /// <value>The set of rules.</value>
    public DbSet<Rule> Rules { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of staff messages.
    /// </summary>
    /// <value>The set of staff messages.</value>
    public DbSet<StaffMessage> StaffMessages { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of tracked messages.
    /// </summary>
    /// <value>The set of tracked messages.</value>
    public DbSet<TrackedMessage> TrackedMessages { get; set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets or sets the set of tracked users.
    /// </summary>
    /// <value>The set of tracked users.</value>
    public DbSet<TrackedUser> TrackedUsers { get; set; } = null!; // assigned when context is created

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite($"Data Source={DataSource}");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new BlockedReporterConfiguration());
        modelBuilder.ApplyConfiguration(new InfractionConfiguration());
        modelBuilder.ApplyConfiguration(new MessageEditConfiguration());
        modelBuilder.ApplyConfiguration(new StaffMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ReportedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TrackedJoinLeaveConfiguration());
        modelBuilder.ApplyConfiguration(new TrackedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TrackedUserConfiguration());
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
    }
}
