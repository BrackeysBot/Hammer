using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines configuration for a <see cref="MemberNote" />.
/// </summary>
internal sealed class MemberNoteConfiguration : IEntityTypeConfiguration<MemberNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MemberNote> builder)
    {
        builder.ToTable("MemberNotes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnOrder(1);

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasColumnOrder(2);

        builder.Property(e => e.UserId)
            .HasColumnName("userId")
            .HasColumnOrder(3);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(4);

        builder.Property(e => e.AuthorId)
            .HasColumnName("authorId")
            .HasColumnOrder(5);

        builder.Property(e => e.CreationTimestamp)
            .HasColumnName("creationTimestamp")
            .HasColumnOrder(6)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.Content)
            .HasColumnName("content")
            .HasColumnOrder(7);
    }
}
