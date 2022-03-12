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

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnOrder(1);

        builder.Property(e => e.AuthorId)
            .HasColumnName("authorId")
            .HasColumnOrder(2);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(3);

        builder.Property(e => e.ChannelId)
            .HasColumnName("channelId")
            .HasColumnOrder(4);

        builder.Property(e => e.MessageId)
            .HasColumnName("messageId")
            .HasColumnOrder(5);

        builder.Property(e => e.CreationTimestamp)
            .HasColumnName("creationTimestamp")
            .HasColumnOrder(6)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.EditTimestamp)
            .HasColumnName("editTimestamp")
            .HasColumnOrder(7)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.ContentBefore)
            .HasColumnName("contentBefore")
            .HasColumnOrder(8);

        builder.Property(e => e.ContentAfter)
            .HasColumnName("contentAfter")
            .HasColumnOrder(9);

        builder.Property(e => e.AttachmentsBefore)
            .HasColumnName("attachmentsBefore")
            .HasColumnOrder(10)
            .HasConversion<UriListToBytesConverter>();

        builder.Property(e => e.AttachmentsAfter)
            .HasColumnName("attachmentsAfter")
            .HasColumnOrder(11)
            .HasConversion<UriListToBytesConverter>();
    }
}
