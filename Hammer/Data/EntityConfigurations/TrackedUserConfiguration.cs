using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TrackedUser" />.
/// </summary>
internal sealed class TrackedUserConfiguration : IEntityTypeConfiguration<TrackedUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrackedUser> builder)
    {
        builder.ToTable("TrackedUsers");
        builder.HasKey(e => new {e.GuildId, e.UserId});

        builder.Property(e => e.UserId)
            .HasColumnName("userId")
            .HasColumnOrder(1);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(2);

        builder.Property(e => e.ExpirationTime)
            .HasColumnName("expirationTime")
            .HasColumnOrder(3)
            .HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
