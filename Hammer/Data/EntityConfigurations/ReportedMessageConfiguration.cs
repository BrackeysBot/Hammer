using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="ReportedMessage" />.
/// </summary>
internal class ReportedMessageConfiguration : IEntityTypeConfiguration<ReportedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReportedMessage> builder)
    {
        builder.ToTable("ReportedMessages");
        builder.HasKey(e => e.Id);
        builder.HasOne(e => e.Message);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnOrder(1);

        builder.Property(e => e.MessageId)
            .HasColumnName("messageId")
            .HasColumnOrder(2);

        builder.Property(e => e.ReporterId)
            .HasColumnName("reporterId")
            .HasColumnOrder(3);
    }
}
