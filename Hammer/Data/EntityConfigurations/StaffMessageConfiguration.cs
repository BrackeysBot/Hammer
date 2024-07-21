using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="StaffMessage" />.
/// </summary>
internal sealed class StaffMessageConfiguration : IEntityTypeConfiguration<StaffMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffMessageConfiguration" /> class.
    /// </summary>
    public StaffMessageConfiguration()
    {
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffMessage> builder)
    {
        builder.ToTable("StaffMessage");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.RecipientId);
        builder.Property(e => e.Content);
    }
}
