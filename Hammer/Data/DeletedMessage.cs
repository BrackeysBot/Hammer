using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message which has been deleted by a staff member.
/// </summary>
internal sealed class DeletedMessage : IEquatable<DeletedMessage>
{
    /// <summary>
    ///     Gets the attachments of the deleted message.
    /// </summary>
    /// <value>The attachments.</value>
    public IReadOnlyList<Uri> Attachments { get; internal set; } = ArraySegment<Uri>.Empty;

    /// <summary>
    ///     Gets the ID of the user who sent the message.
    /// </summary>
    /// <value>The author's user ID.</value>
    public ulong AuthorId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the channel in which this message was sent.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; internal set; }

    /// <summary>
    ///     Gets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    public string? Content { get; internal set; }

    /// <summary>
    ///     Gets the date and time at which the message was created.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTimeOffset CreationTimestamp { get; internal set; }

    /// <summary>
    ///     Gets the date and time at which the message was deleted.
    /// </summary>
    /// <value>The deletion timestamp.</value>
    public DateTimeOffset DeletionTimestamp { get; internal set; }

    /// <summary>
    ///     Gets the ID of the guild in which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the deleted message.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong MessageId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the staff member who deleted the message.
    /// </summary>
    /// <value>The staff member's user ID.</value>
    public ulong StaffMemberId { get; internal set; }

    /// <summary>
    ///     Determines whether two <see cref="DeletedMessage" /> instances are equal.
    /// </summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(DeletedMessage? left, DeletedMessage? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="DeletedMessage" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first message.</param>
    /// <param name="right">The second message.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(DeletedMessage? left, DeletedMessage? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Constructs a new instance of the <see cref="DeletedMessage" /> class.
    /// </summary>
    /// <param name="message">The message which was deleted.</param>
    /// <param name="staffMember">The staff member responsible for the deletion.</param>
    public static DeletedMessage Create(DiscordMessage message, DiscordMember staffMember)
    {
        return new DeletedMessage
        {
            Attachments = message.Attachments.Select(a => new Uri(a.Url)).ToArray(),
            AuthorId = message.Author.Id,
            ChannelId = message.Channel.Id,
            Content = message.Content,
            CreationTimestamp = message.CreationTimestamp,
            DeletionTimestamp = DateTimeOffset.UtcNow,
            GuildId = message.Channel.Guild.Id,
            MessageId = message.Id,
            StaffMemberId = staffMember.Id
        };
    }

    /// <inheritdoc />
    public bool Equals(DeletedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return MessageId == other.MessageId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DeletedMessage other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return MessageId.GetHashCode();
    }
}
