using Hammer.Configuration;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="Rule" />.
/// </summary>
internal sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public RuleConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        DatabaseConfiguration configuration = _configurationService.BotConfiguration.Database;
        string tablePrefix = configuration.Provider == "sqlite" ? string.Empty : configuration.TablePrefix;
        builder.ToTable(tablePrefix + "Rule");
        builder.HasKey(e => new { e.Id, e.GuildId });

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.Brief);
        builder.Property(e => e.Description);
    }
}
