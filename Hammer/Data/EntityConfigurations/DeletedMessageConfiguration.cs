using Hammer.Configuration;
using Hammer.Data.ValueConverters;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the configuration for the <see cref="DeletedMessage"/> entity.
/// </summary>
internal sealed class DeletedMessageConfiguration : IEntityTypeConfiguration<DeletedMessage>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeletedMessageConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public DeletedMessageConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeletedMessage> builder)
    {
        builder.ToTable("DeletedMessage");
        builder.HasKey(e => e.MessageId);

        builder.Property(e => e.MessageId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.AuthorId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.Content);
        builder.Property(e => e.Attachments).HasConversion<UriListToBytesConverter>();

        if (_configurationService.BotConfiguration.Database.Provider == "sqlite")
        {
            builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
            builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        }
        else
        {
            builder.Property(e => e.CreationTimestamp).HasColumnType("DATETIME(6)");
            builder.Property(e => e.DeletionTimestamp).HasColumnType("DATETIME(6)");
        }
    }
}
