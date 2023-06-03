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

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.IsDeleted);
        builder.Property(e => e.Content);
        builder.Property(e => e.Attachments).HasConversion<UriListToBytesConverter>();

        if (Environment.GetEnvironmentVariable("USE_MYSQL") == "1")
        {
            builder.Property(e => e.CreationTimestamp).HasColumnType("DATETIME(6)");
            builder.Property(e => e.DeletionTimestamp).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
            builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
