using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="DeletedMessage" />.
/// </summary>
[SuppressMessage("ReSharper", "InheritdocConsiderUsage", Justification = "Unnecessary")]
internal sealed class DeletedMessageConfiguration : IEntityTypeConfiguration<DeletedMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeletedMessage> builder)
    {
        builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.HasMany(e => e.Attachments).WithOne(e => e.Message);
    }
}
