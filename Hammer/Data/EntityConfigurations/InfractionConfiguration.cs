using DSharpPlus;
using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="Infraction" />.
/// </summary>
internal sealed class InfractionConfiguration : IEntityTypeConfiguration<Infraction>
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionConfiguration" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public InfractionConfiguration(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Infraction> builder)
    {
        builder.ToTable(nameof(Infraction));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.Guild).HasConversion(new DiscordGuildValueConverter(_discordClient));
        builder.Property(e => e.User).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.StaffMember).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.Type);
        builder.Property(e => e.IssuedAt).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.Reason);
    }
}
