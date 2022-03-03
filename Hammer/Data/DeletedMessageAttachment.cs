using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DisCatSharp.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents an attachment to a <see cref="DeletedMessage" />. This record essentially serves as a mapping from
///     <see cref="DiscordAttachment" />.
/// </summary>
[Table("DeletedMessageAttachments")]
internal record DeletedMessageAttachment
{
    /// <summary>
    ///     Gets or sets the ID of the attachment.
    /// </summary>
    /// <value>The ID of the attachment.</value>
    [Key, Column("id", Order = 1)]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message to which this attachment belonged.
    /// </summary>
    /// <value>The message ID.</value>
    [Column("messageId", Order = 2)]
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the URL of the attachment.
    /// </summary>
    /// <value>The URL.</value>
    [Column("url", Order = 3)]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the deleted message to which this attachment belongs.
    /// </summary>
    /// <value>The deleted message.</value>
    public DeletedMessage Message { get; set; } = null!;
}
