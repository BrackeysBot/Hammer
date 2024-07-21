using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines configuration for a <see cref="MemberNote" />.
/// </summary>
internal sealed class MemberNoteConfiguration : IEntityTypeConfiguration<MemberNote>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberNoteConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public MemberNoteConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MemberNote> builder)
    {
        builder.ToTable("MemberNote");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.Type);
        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.AuthorId);

        if (_isMySql)
        {
            builder.Property(e => e.CreationTimestamp).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        }

        builder.Property(e => e.Content);
    }
}
