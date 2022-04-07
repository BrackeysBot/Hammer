using DSharpPlus;
using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="Mute" />.
/// </summary>
internal sealed class MuteConfiguration : IEntityTypeConfiguration<Mute>
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteConfiguration" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public MuteConfiguration(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Mute> builder)
    {
        builder.ToTable(nameof(Mute));
        builder.HasKey(e => new {e.User, e.Guild});

        builder.Property(e => e.Guild).HasConversion(new DiscordGuildValueConverter(_discordClient));
        builder.Property(e => e.User).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.ExpiresAt).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
