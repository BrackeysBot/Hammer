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
        builder.ToTable("Infractions");
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

        builder.Property(e => e.StaffMemberId)
            .HasColumnName("staffMemberId")
            .HasColumnOrder(4);

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasColumnOrder(5);

        builder.Property(e => e.IssuedAt)
            .HasColumnName("issuedAt")
            .HasColumnOrder(6)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasColumnOrder(7);

        builder.Property(e => e.ExpirationTime)
            .HasColumnName("expirationTime")
            .HasColumnOrder(8)
            .HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
