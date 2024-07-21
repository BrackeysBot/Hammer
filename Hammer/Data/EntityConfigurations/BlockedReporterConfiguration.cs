using Hammer.Configuration;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="BlockedReporter" />.
/// </summary>
internal sealed class BlockedReporterConfiguration : IEntityTypeConfiguration<BlockedReporter>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockedReporterConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public BlockedReporterConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedReporter> builder)
    {
        DatabaseConfiguration configuration = _configurationService.BotConfiguration.Database;
        string tablePrefix = configuration.Provider == "sqlite" ? string.Empty : configuration.TablePrefix;
        builder.ToTable(tablePrefix + "BlockedReporter");
        builder.HasKey(e => new { e.UserId, e.GuildId });

        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.StaffMemberId);

        PropertyBuilder<DateTimeOffset> blockedAtProperty = builder.Property(e => e.BlockedAt);
        if (_configurationService.BotConfiguration.Database.Provider == "sqlite")
        {
            blockedAtProperty.HasConversion<DateTimeOffsetToBytesConverter>();
        }
        else
        {
            blockedAtProperty.HasColumnType("DATETIME(6)");
        }
    }
}
