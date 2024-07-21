using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="Mute" />.
/// </summary>
internal sealed class MuteConfiguration : IEntityTypeConfiguration<Mute>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public MuteConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Mute> builder)
    {
        builder.ToTable("Mute");
        builder.HasKey(e => new { e.UserId, e.GuildId });

        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);

        if (_isMySql)
        {
            builder.Property(e => e.ExpiresAt).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.ExpiresAt).HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
