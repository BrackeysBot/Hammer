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

        builder.Property(e => e.Id);
        builder.Property(e => e.Type);
        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.Content);
    }
}
