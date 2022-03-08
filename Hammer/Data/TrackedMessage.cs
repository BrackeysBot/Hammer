using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DisCatSharp.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message being tracked by the bot's database.
/// </summary>
[Table("TrackedMessages")]
internal class TrackedMessage : IEquatable<TrackedMessage>, IEquatable<DiscordMessage>
{
    /// <summary>
    ///     Gets or sets the attachments in this message.
    /// </summary>
    /// <value>The attachments.</value>
    [Column("attachments", Order = 7)]
    public List<Uri> Attachments { get; set; } = new();

    /// <summary>
    ///     Gets or sets the ID of the user which sent this message.
    /// </summary>
    /// <value>The author's user ID.</value>
    [Column("authorId", Order = 4)]
    public ulong AuthorId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the channel in which this message was sent.
    /// </summary>
    /// <value>The channel ID.</value>
    [Column("channelId", Order = 3)]
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    [Column("content", Order = 5)]
    public string? Content { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    /// <value>The message ID.</value>
    [Key, Column("id", Order = 1)]
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this message has been deleted.
    /// </summary>
    /// <value><see langword="true" /> if this message has been deleted; otherwise, <see langword="false" />.</value>
    [Column("isDeleted", Order = 6)]
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild in which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    [Column("guildId", Order = 2)]
    public ulong GuildId { get; set; }

    public static bool operator ==(TrackedMessage left, DiscordMessage right) => left.Equals(right);
    public static bool operator !=(TrackedMessage left, DiscordMessage right) => !(left == right);
    public static bool operator ==(DiscordMessage left, TrackedMessage right) => right == left;
    public static bool operator !=(DiscordMessage left, TrackedMessage right) => !(left == right);

    /// <summary>
    ///     Constructs a <see cref="TrackedMessage" /> by extracting data from a <see cref="DiscordMessage" />.
    /// </summary>
    /// <param name="message">The message whose details to extract.</param>
    /// <returns>A new instance of <see cref="TrackedMessage" />.</returns>
    public static TrackedMessage FromDiscordMessage(DiscordMessage message)
    {
        return new TrackedMessage
        {
            Id = message.Id,
            AuthorId = message.Author.Id,
            ChannelId = message.Channel.Id,
            GuildId = message.Channel.Guild.Id,
            Content = message.Content,
            Attachments = message.Attachments.Select(a => new Uri(a.Url)).ToList()
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return (obj is TrackedMessage tracked && Equals(tracked)) || (obj is DiscordMessage message && Equals(message));
    }

    /// <inheritdoc />
    public bool Equals(TrackedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ChannelId == other.ChannelId && Id == other.Id && GuildId == other.GuildId;
    }

    /// <inheritdoc />
    public bool Equals(DiscordMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return Id == other.Id && ChannelId == other.Channel.Id && GuildId == other.Channel.Guild.Id;
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, ChannelId, GuildId);
    }
}
