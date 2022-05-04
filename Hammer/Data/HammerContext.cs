using System.IO;
using DSharpPlus;
using Hammer.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Hammer.Data;

/// <summary>
///     Represents a session with the <c>hammer.db</c> database.
/// </summary>
internal sealed class HammerContext : DbContext
{
    private readonly DiscordClient _discordClient;
    private readonly string _dataSource;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HammerContext" /> class.
    /// </summary>
    /// <param name="discordClient">The <see cref="DiscordClient" />.</param>
    /// <param name="plugin">The owning plugin.</param>
    public HammerContext(DiscordClient discordClient, HammerPlugin plugin)
    {
        _discordClient = discordClient;
        _dataSource = Path.Combine(plugin.DataDirectory.FullName, "hammer.db");
    }

    /// <summary>
    ///     Gets the set of users who are blocked from making reports.
    /// </summary>
    /// <value>The set of blocked reporters.</value>
    public DbSet<BlockedReporter> BlockedReporters { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of staff-deleted messages.
    /// </summary>
    /// <value>The set of staff-deleted messages.</value>
    public DbSet<DeletedMessage> DeletedMessages { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of infractions.
    /// </summary>
    /// <value>The set of infractions.</value>
    public DbSet<Infraction> Infractions { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of member notes.
    /// </summary>
    /// <value>The set of member notes.</value>
    public DbSet<MemberNote> MemberNotes { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of mutes.
    /// </summary>
    /// <value>The set of mutes.</value>
    public DbSet<Mute> Mutes { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of reported messages.
    /// </summary>
    /// <value>The set of reported messages.</value>
    public DbSet<ReportedMessage> ReportedMessages { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of rules.
    /// </summary>
    /// <value>The set of rules.</value>
    public DbSet<Rule> Rules { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of staff messages.
    /// </summary>
    /// <value>The set of staff messages.</value>
    public DbSet<StaffMessage> StaffMessages { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of temporary bans.
    /// </summary>
    /// <value>The set of temporary bans.</value>
    public DbSet<TemporaryBan> TemporaryBans { get; private set; } = null!; // assigned when context is created

    /// <summary>
    ///     Gets the set of tracked messages.
    /// </summary>
    /// <value>The set of tracked messages.</value>
    public DbSet<TrackedMessage> TrackedMessages { get; private set; } = null!; // assigned when context is created

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite($"Data Source={_dataSource}");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlockedReporterConfiguration());
        modelBuilder.ApplyConfiguration(new DeletedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InfractionConfiguration(_discordClient));
        modelBuilder.ApplyConfiguration(new MemberNoteConfiguration());
        modelBuilder.ApplyConfiguration(new MuteConfiguration());
        modelBuilder.ApplyConfiguration(new StaffMessageConfiguration(_discordClient));
        modelBuilder.ApplyConfiguration(new ReportedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new TemporaryBanConfiguration());
        modelBuilder.ApplyConfiguration(new TrackedMessageConfiguration());
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
    }
}
