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
        builder.ToTable("BlockedReporters");
        builder.HasKey(e => new {e.UserId, e.GuildId});

        builder.Property(e => e.UserId)
            .HasColumnName("userId")
            .HasColumnOrder(1);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(2);

        builder.Property(e => e.StaffMemberId)
            .HasColumnName("staffMemberId")
            .HasColumnOrder(3);

        builder.Property(e => e.BlockedAt)
            .HasColumnName("blockedAt")
            .HasColumnOrder(4)
            .HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
