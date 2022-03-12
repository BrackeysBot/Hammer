using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TrackedMessage" />.
/// </summary>
internal sealed class TrackedMessageConfiguration : IEntityTypeConfiguration<TrackedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrackedMessage> builder)
    {
        builder.ToTable("TrackedMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnOrder(1);

        builder.Property(e => e.GuildId)
            .HasColumnName("guildId")
            .HasColumnOrder(2);

        builder.Property(e => e.ChannelId)
            .HasColumnName("channelId")
            .HasColumnOrder(3);

        builder.Property(e => e.AuthorId)
            .HasColumnName("authorId")
            .HasColumnOrder(4);

        builder.Property(e => e.IsDeleted)
            .HasColumnName("isDeleted")
            .HasColumnOrder(5);

        builder.Property(e => e.CreationTimestamp)
            .HasColumnName("creationTimestamp")
            .HasColumnOrder(6)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.DeletionTimestamp)
            .HasColumnName("deletionTimestamp")
            .HasColumnOrder(7)
            .HasConversion<DateTimeOffsetToBytesConverter>();

        builder.Property(e => e.Content)
            .HasColumnName("content")
            .HasColumnOrder(8);

        builder.Property(e => e.Attachments)
            .HasColumnName("attachments")
            .HasColumnOrder(9)
            .HasConversion<UriListToBytesConverter>();
    }
}
