using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the configuration for the <see cref="DeletedMessage"/> entity.
/// </summary>
internal sealed class DeletedMessageConfiguration : IEntityTypeConfiguration<DeletedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeletedMessage> builder)
    {
        builder.ToTable(nameof(DeletedMessage));
        builder.HasKey(e => e.MessageId);

        builder.Property(e => e.MessageId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.Content);
        builder.Property(e => e.Attachments).HasConversion<UriListToBytesConverter>();
    }
}
