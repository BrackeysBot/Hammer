namespace Hammer.Data;

/// <summary>
///     Represents a message sent from a staff member to a community member.
/// </summary>
internal sealed class StaffMessage : IEquatable<StaffMessage>
{
    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    public string Content { get; internal set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the ID of the guild from which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    public long Id { get; internal set; }

    /// <summary>
    ///     Gets the user who is in receipt of this message.
    /// </summary>
    /// <value>The message recipient.</value>
    public ulong RecipientId { get; internal set; }

    /// <summary>
    ///     Gets the date and time at which this message was sent.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTimeOffset SentAt { get; internal set; }

    /// <summary>
    ///     Gets the user ID of the staff member who sent the message.
    /// </summary>
    /// <value>The staff member's user ID.</value>
    public ulong StaffMemberId { get; internal set; }

    /// <inheritdoc />
    public bool Equals(StaffMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is StaffMessage other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
