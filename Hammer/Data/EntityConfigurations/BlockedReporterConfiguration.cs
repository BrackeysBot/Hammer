using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="BlockedReporter" />.
/// </summary>
internal sealed class BlockedReporterConfiguration : IEntityTypeConfiguration<BlockedReporter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BlockedReporter> builder)
    {
        builder.ToTable(nameof(BlockedReporter));
        builder.HasKey(e => new {e.UserId, e.GuildId});

        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.StaffMemberId);

        PropertyBuilder<DateTimeOffset> blockedAtProperty = builder.Property(e => e.BlockedAt);
        if (Environment.GetEnvironmentVariable("USE_MYSQL") == "1")
        {
            blockedAtProperty.HasColumnType("DATETIME(6)");
        }
        else
        {
            blockedAtProperty.HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
