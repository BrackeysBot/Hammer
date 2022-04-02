using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="MessageEdit" />.
/// </summary>
internal sealed class MessageEditConfiguration : IEntityTypeConfiguration<MessageEdit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MessageEdit> builder)
    {
        builder.ToTable("MessageEdits");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.MessageId);
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.EditTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.ContentBefore);
        builder.Property(e => e.ContentAfter);
        builder.Property(e => e.AttachmentsBefore).HasConversion<UriListToBytesConverter>();
        builder.Property(e => e.AttachmentsAfter).HasConversion<UriListToBytesConverter>();
    }
}
