using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TemporaryBan" />.
/// </summary>
internal sealed class TemporaryBanConfiguration : IEntityTypeConfiguration<TemporaryBan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TemporaryBan> builder)
    {
        builder.ToTable(nameof(TemporaryBan));
        builder.HasKey(e => new {e.UserId, e.GuildId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        if (Environment.GetEnvironmentVariable("USE_MYSQL") == "1")
        {
            builder.Property(e => e.ExpiresAt).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.ExpiresAt).HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
