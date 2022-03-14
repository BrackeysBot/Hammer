using System;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Data;

/// <summary>
///     Represents a user who has been blocked from making reports.
/// </summary>
internal sealed class BlockedReporter : IEquatable<BlockedReporter>
{
    /// <summary>
    ///     Gets or sets the date and time at which this user was blocked.
    /// </summary>
    /// <value>The block date and time.</value>
    public DateTimeOffset BlockedAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild in which this user is blocked.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who blocked the user.
    /// </summary>
    /// <value>The staff member's user ID.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets the ID of the user who has been blocked from creating reports.
    /// </summary>
    /// <value>The user ID.</value>
    public ulong UserId { get; set; }

    /// <inheritdoc />
    public bool Equals(BlockedReporter? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is BlockedReporter other && Equals(other));
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(UserId, GuildId);
    }
}
