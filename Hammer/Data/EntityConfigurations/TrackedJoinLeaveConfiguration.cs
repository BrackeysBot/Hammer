using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TrackedMessage" />.
/// </summary>
internal sealed class TrackedJoinLeaveConfiguration : IEntityTypeConfiguration<TrackedJoinLeave>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrackedJoinLeave> builder)
    {
        builder.ToTable("TrackedJoinLeaves");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnOrder(1);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(2);

        builder.Property(e => e.UserId)
            .HasColumnName("userId")
            .HasColumnOrder(3);

        builder.Property(e => e.OccuredAt)
            .HasColumnName("occuredAt")
            .HasColumnOrder(4)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasColumnOrder(5);
    }
}
