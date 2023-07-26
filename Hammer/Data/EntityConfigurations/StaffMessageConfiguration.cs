using Hammer.Configuration;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="StaffMessage" />.
/// </summary>
internal sealed class StaffMessageConfiguration : IEntityTypeConfiguration<StaffMessage>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffMessageConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public StaffMessageConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StaffMessage> builder)
    {
        DatabaseConfiguration configuration = _configurationService.BotConfiguration.Database;
        string tablePrefix = configuration.Provider == "sqlite" ? string.Empty : configuration.TablePrefix;
        builder.ToTable(tablePrefix + "StaffMessage");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.RecipientId);
        builder.Property(e => e.Content);
    }
}
