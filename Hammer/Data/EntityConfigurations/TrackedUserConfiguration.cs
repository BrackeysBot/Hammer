using DSharpPlus;
using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TrackedUser" />.
/// </summary>
internal sealed class TrackedUserConfiguration : IEntityTypeConfiguration<TrackedUser>
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TrackedUserConfiguration" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public TrackedUserConfiguration(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TrackedUser> builder)
    {
        builder.ToTable("TrackedUsers");
        builder.HasKey(e => new {e.Guild, e.User});

        builder.Property(e => e.User).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.Guild).HasConversion(new DiscordGuildValueConverter(_discordClient));
        builder.Property(e => e.ExpirationTime).HasConversion<DateTimeOffsetToBytesConverter>();
    }
}
