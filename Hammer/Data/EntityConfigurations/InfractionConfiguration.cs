using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="Infraction" />.
/// </summary>
[SuppressMessage("ReSharper", "InheritdocConsiderUsage", Justification = "Unnecessary")]
internal sealed class InfractionConfiguration : IEntityTypeConfiguration<Infraction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Infraction> builder)
    {
        builder.Property(e => e.Time).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
