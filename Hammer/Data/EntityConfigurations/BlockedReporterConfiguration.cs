using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="BlockedReporter" />.
/// </summary>
internal sealed class BlockedReporterConfiguration : IEntityTypeConfiguration<BlockedReporter>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BlockedReporterConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public BlockedReporterConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedReporter> builder)
    {
        builder.ToTable("BlockedReporter");
        builder.HasKey(e => new { e.UserId, e.GuildId });

        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.StaffMemberId);

        PropertyBuilder<DateTimeOffset> blockedAtProperty = builder.Property(e => e.BlockedAt);
        if (_isMySql)
        {
            blockedAtProperty.HasColumnType("DATETIME(6)");
        }
        else
        {
            blockedAtProperty.HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
