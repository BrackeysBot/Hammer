using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

internal sealed class TrackedMessageConfiguration : IEntityTypeConfiguration<TrackedMessage>
{
    public void Configure(EntityTypeBuilder<TrackedMessage> builder)
    {
        builder.Property(e => e.Attachments)
            .HasConversion(a => string.Join("\n", a),
                s => s.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(u => new Uri(u)).ToList());
    }
}
