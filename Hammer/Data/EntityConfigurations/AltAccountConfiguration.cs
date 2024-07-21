using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="AltAccount" />.
/// </summary>
internal sealed class AltAccountConfiguration : IEntityTypeConfiguration<AltAccount>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AltAccountConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public AltAccountConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AltAccount> builder)
    {
        builder.ToTable("AltAccount");
        builder.HasKey(e => new { e.UserId, e.AltId });

        builder.Property(e => e.UserId);
        builder.Property(e => e.AltId);
        builder.Property(e => e.StaffMemberId);

        if (_isMySql)
        {
            builder.Property(e => e.RegisteredAt).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.RegisteredAt).HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
