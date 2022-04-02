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

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        builder.Property(e => e.OccuredAt).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.Type);
    }
}
