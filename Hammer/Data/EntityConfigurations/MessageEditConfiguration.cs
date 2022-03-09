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
        builder.Property(e => e.AttachmentsAfter).HasConversion<UriListToBytesConverter>();
        builder.Property(e => e.AttachmentsBefore).HasConversion<UriListToBytesConverter>();
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.EditTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
