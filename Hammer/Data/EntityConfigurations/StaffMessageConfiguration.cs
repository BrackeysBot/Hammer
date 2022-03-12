using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="StaffMessage" />.
/// </summary>
internal sealed class StaffMessageConfiguration : IEntityTypeConfiguration<StaffMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffMessage> builder)
    {
        builder.ToTable("StaffMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").HasColumnOrder(1);
        builder.Property(e => e.GuildId).HasColumnName("guildId").HasColumnOrder(2);
        builder.Property(e => e.StaffMemberId).HasColumnName("staffMemberId").HasColumnOrder(3);
        builder.Property(e => e.RecipientId).HasColumnName("recipientId").HasColumnOrder(4);
        builder.Property(e => e.Content).HasColumnName("content").HasColumnOrder(5);
    }
}
