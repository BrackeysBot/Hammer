using System;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Data;

/// <summary>
///     Represents an event of a tracked user joining or leaving a guild.
/// </summary>
internal sealed class TrackedJoinLeave : IEquatable<TrackedJoinLeave>
{
    /// <summary>
    ///     An enumeration of possible types for a <see cref="TrackedJoinLeave" /> to represent.
    /// </summary>
    public enum JoinLeaveType
    {
        /// <summary>
        ///     The user joined.
        /// </summary>
        Join,

        /// <summary>
        ///     The user left.
        /// </summary>
        Leave
    }

    /// <summary>
    ///     Gets or sets the ID of this entry.
    /// </summary>
    /// <value>The ID of this entry.</value>
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild which the user joined or left.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this event occurred.
    /// </summary>
    /// <value>The event date and time.</value>
    public DateTimeOffset OccuredAt { get; set; }

    /// <summary>
    ///     Gets or sets the even type.
    /// </summary>
    /// <value>The event type.</value>
    public JoinLeaveType Type { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user which joined or left.
    /// </summary>
    /// <value>The user ID.</value>
    public ulong UserId { get; set; }

    /// <inheritdoc />
    public bool Equals(TrackedJoinLeave? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public static bool operator ==(TrackedJoinLeave left, TrackedJoinLeave right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrackedJoinLeave left, TrackedJoinLeave right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is TrackedJoinLeave other && Equals(other));
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
