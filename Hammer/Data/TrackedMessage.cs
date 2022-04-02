using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message being tracked by the bot's database.
/// </summary>
internal class TrackedMessage : IEquatable<TrackedMessage>, IEquatable<DiscordMessage>
{
    /// <summary>
    ///     Gets or sets the attachments in this message.
    /// </summary>
    /// <value>The attachments.</value>
    public IReadOnlyList<Uri> Attachments { get; set; } = ArraySegment<Uri>.Empty;

    /// <summary>
    ///     Gets or sets the ID of the user which sent this message.
    /// </summary>
    /// <value>The author's user ID.</value>
    public ulong AuthorId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the channel in which this message was sent.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    public string? Content { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this message was originally sent.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTimeOffset CreationTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this message was deleted.
    /// </summary>
    /// <value>The deletion timestamp, or <see langword="null" /> if the message has not been deleted.</value>
    public DateTimeOffset? DeletionTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong Id { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this message has been deleted.
    /// </summary>
    /// <value><see langword="true" /> if this message has been deleted; otherwise, <see langword="false" />.</value>
    public bool IsDeleted { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild in which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    public static bool operator ==(TrackedMessage left, DiscordMessage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrackedMessage left, DiscordMessage right)
    {
        return !(left == right);
    }

    public static bool operator ==(DiscordMessage left, TrackedMessage right)
    {
        return right == left;
    }

    public static bool operator !=(DiscordMessage left, TrackedMessage right)
    {
        return !(left == right);
    }

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
            CreationTimestamp = message.CreationTimestamp,
            Attachments = message.Attachments.Select(a => new Uri(a.Url)).ToArray()
        };
    }

    /// <inheritdoc />
    public bool Equals(DiscordMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return Id == other.Id && ChannelId == other.Channel.Id && GuildId == other.Channel.Guild.Id;
    }

    /// <inheritdoc />
    public bool Equals(TrackedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ChannelId == other.ChannelId && Id == other.Id && GuildId == other.GuildId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return (obj is TrackedMessage tracked && Equals(tracked)) || (obj is DiscordMessage message && Equals(message));
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, ChannelId, GuildId);
    }
}
