using DSharpPlus;
using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TemporaryBan" />.
/// </summary>
internal sealed class TemporaryBanConfiguration : IEntityTypeConfiguration<TemporaryBan>
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryBanConfiguration" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public TemporaryBanConfiguration(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TemporaryBan> builder)
    {
        builder.ToTable(nameof(TemporaryBan));
        builder.HasKey(e => new {e.User, e.Guild});

        builder.Property(e => e.Guild).HasConversion(new DiscordGuildValueConverter(_discordClient));
        builder.Property(e => e.User).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.ExpiresAt).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
