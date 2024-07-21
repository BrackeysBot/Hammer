using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="Infraction" />.
/// </summary>
internal sealed class InfractionConfiguration : IEntityTypeConfiguration<Infraction>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public InfractionConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Infraction> builder)
    {
        builder.ToTable("Infraction");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.Type);

        if (_isMySql)
        {
            builder.Property(e => e.IssuedAt).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.IssuedAt).HasConversion<DateTimeOffsetToBytesConverter>();
        }

        builder.Property(e => e.Reason);
        builder.Property(e => e.AdditionalInformation);
        builder.Property(e => e.RuleId);
        builder.Property(e => e.RuleText);
    }
}
