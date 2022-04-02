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

        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ExpirationTime).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
