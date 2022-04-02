using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="Infraction" />.
/// </summary>
internal sealed class InfractionConfiguration : IEntityTypeConfiguration<Infraction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Infraction> builder)
    {
        builder.ToTable(nameof(Infraction));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.Type);
        builder.Property(e => e.IssuedAt).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.Reason);
    }
}
