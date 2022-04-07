using DSharpPlus;
using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="StaffMessage" />.
/// </summary>
internal sealed class StaffMessageConfiguration : IEntityTypeConfiguration<StaffMessage>
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffMessageConfiguration" /> class.
    /// </summary>
    public StaffMessageConfiguration(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffMessage> builder)
    {
        builder.ToTable(nameof(StaffMessage));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.Guild).HasConversion(new DiscordGuildValueConverter(_discordClient));
        builder.Property(e => e.StaffMember).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.Recipient).HasConversion(new DiscordUserValueConverter(_discordClient));
        builder.Property(e => e.Content);
    }
}
