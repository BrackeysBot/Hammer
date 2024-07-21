using Hammer.Configuration;
using Hammer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines configuration for a <see cref="MemberNote" />.
/// </summary>
internal sealed class MemberNoteConfiguration : IEntityTypeConfiguration<MemberNote>
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberNoteConfiguration" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    public MemberNoteConfiguration(ConfigurationService configurationService)
    {
        _configurationService = configurationService;
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MemberNote> builder)
    {
        builder.ToTable("MemberNote");
        string tablePrefix = configuration.Provider == "sqlite" ? string.Empty : configuration.TablePrefix;
        builder.ToTable(tablePrefix + "MemberNote");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.Type);
        builder.Property(e => e.UserId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.AuthorId);

        if (_configurationService.BotConfiguration.Database.Provider == "sqlite")
        {
            builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        }
        else
        {
            builder.Property(e => e.CreationTimestamp).HasColumnType("DATETIME(6)");
        }

        builder.Property(e => e.Content);
    }
}
