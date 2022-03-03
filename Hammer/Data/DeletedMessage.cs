using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DisCatSharp.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a deleted Discord message. This record essentially serves as a mapping from <see cref="DiscordMessage" />.
/// </summary>
[Table("DeletedMessages")]
internal record DeletedMessage
{
    /// <summary>
    ///     Gets or sets the ID of the author who sent this message.
    /// </summary>
    /// <value>The ID of the author.</value>
    [Column("authorId", Order = 4)]
    public ulong AuthorId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the channel in which this message was sent.
    /// </summary>
    /// <value>The ID of the channel.</value>
    [Column("channelId", Order = 3)]
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The content.</value>
    [Column("content", Order = 8)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the creation timestamp of the message.
    /// </summary>
    /// <value>The creation timestamp.</value>
    [Column("creationTime", Order = 6)]
    public DateTimeOffset CreationTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the deletion timestamp of the message.
    /// </summary>
    /// <value>The deletion timestamp.</value>
    [Column("deletionTime", Order = 7)]
    public DateTimeOffset DeletionTimestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    /// <value>The ID of the message.</value>
    [Key]
    [Column("id", Order = 1)]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild in which this message was sent.
    /// </summary>
    /// <value>The ID of the guild.</value>
    [Column("guildId", Order = 2)]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who deleted this message.
    /// </summary>
    /// <value>The ID of the staff member.</value>
    [Column("staffMemberId", Order = 5)]
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the list of attachments in this message.
    /// </summary>
    /// <value>The list of attachments.</value>
    public List<DeletedMessageAttachment> Attachments { get; set; } = new();

    /// <summary>
    ///     Constructs a <see cref="DeletedMessage" /> by extracting the values from a <see cref="DiscordMessage" />, and the
    ///     details of the staff member which deleted the message.
    /// </summary>
    /// <param name="message">The message which was deleted.</param>
    /// <param name="staffMember">The staff member who deleted the message.</param>
    /// <returns>A new instance of <see cref="DeletedMessage" />.</returns>
    public static DeletedMessage FromDiscordMessage(DiscordMessage message, DiscordMember staffMember)
    {
        return new DeletedMessage
        {
            Id = message.Id,
            AuthorId = message.Author.Id,
            StaffMemberId = staffMember.Id,
            CreationTimestamp = message.CreationTimestamp,
            DeletionTimestamp = DateTimeOffset.UtcNow,
            GuildId = message.Channel.Guild.Id,
            ChannelId = message.Channel.Id,
            Content = message.Content,
            Attachments = message.Attachments.Select(attachment => new DeletedMessageAttachment
            {
                Id = attachment.Id,
                MessageId = message.Id,
                Url = attachment.Url
            }).ToList()
        };
    }
}
