using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Defines configuration for <see cref="Rule" />.
/// </summary>
internal sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.HasKey(e => new {e.Id, e.GuildId});
    }
}
