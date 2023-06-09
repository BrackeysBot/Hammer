using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="AltAccount" />.
/// </summary>
internal sealed class AltAccountConfiguration : IEntityTypeConfiguration<AltAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AltAccount> builder)
    {
        builder.ToTable("AltAccount");
        builder.HasKey(e => new {e.UserId, e.AltId});

        builder.Property(e => e.UserId);
        builder.Property(e => e.AltId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.RegisteredAt).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
