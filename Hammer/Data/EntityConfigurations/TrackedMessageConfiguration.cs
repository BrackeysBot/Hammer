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
        builder.Property(e => e.Attachments).HasConversion<UriListToBytesConverter>();
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
