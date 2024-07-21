using Hammer.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.EntityConfigurations;

/// <summary>
///     Represents a class which defines the configuration for the <see cref="DeletedMessage"/> entity.
/// </summary>
internal sealed class DeletedMessageConfiguration : IEntityTypeConfiguration<DeletedMessage>
{
    private readonly bool _isMySql;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeletedMessageConfiguration" /> class.
    /// </summary>
    /// <param name="isMySql">
    ///     <see langword="true" /> if this configuration should use MySQL configuration, otherwise <see langword="false" />.
    /// </param>
    public DeletedMessageConfiguration(bool isMySql)
    {
        _isMySql = isMySql;
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

        if (_isMySql)
        {
            builder.Property(e => e.CreationTimestamp).HasColumnType("DATETIME(6)");
            builder.Property(e => e.DeletionTimestamp).HasColumnType("DATETIME(6)");
        }
        else
        {
            builder.Property(e => e.CreationTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
            builder.Property(e => e.DeletionTimestamp).HasConversion<DateTimeOffsetToBytesConverter>();
        }
    }
}
