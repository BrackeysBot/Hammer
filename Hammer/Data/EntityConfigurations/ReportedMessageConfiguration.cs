using Hammer.Configuration;
using Hammer.Data.ValueConverters;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the database configuration for <see cref="ReportedMessage" />.
/// </summary>
internal class ReportedMessageConfiguration : IEntityTypeConfiguration<ReportedMessage>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportedMessageConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public ReportedMessageConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReportedMessage> builder)
    {
        builder.ToTable("ReportedMessage");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.MessageId);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.ReporterId);
        builder.Property(e => e.Content);
        builder.Property(e => e.Attachments).HasConversion<UriListToBytesConverter>();
    }
}
