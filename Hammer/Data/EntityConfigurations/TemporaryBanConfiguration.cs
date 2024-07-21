using Hammer.Configuration;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="TemporaryBan" />.
/// </summary>
internal sealed class TemporaryBanConfiguration : IEntityTypeConfiguration<TemporaryBan>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryBanConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public TemporaryBanConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TemporaryBan> builder)
    {
        builder.ToTable("TemporaryBan");
        builder.HasKey(e => new { e.UserId, e.GuildId });

        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        if (_configurationService.BotConfiguration.Database.Provider == "sqlite")
        {
            builder.Property(e => e.ExpiresAt).HasConversion<DateTimeOffsetToBytesConverter>();
        }
        else
        {
            builder.Property(e => e.ExpiresAt).HasColumnType("DATETIME(6)");
        }
    }
}
