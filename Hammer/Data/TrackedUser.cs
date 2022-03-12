using System;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Data;

/// <summary>
///     Represents a user which is being tracked in a guild.
/// </summary>
internal class TrackedUser : IEquatable<TrackedUser>
{
    /// <summary>
    ///     Gets or sets the ID of the guild in which the user is being tracked.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user being tracked.
    /// </summary>
    /// <value>The user ID.</value>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Gets or sets the time at which this user should no longer be tracked.
    /// </summary>
    /// <value>The expiration time, or <see langword="null" /> if the user is tracked indefinitely.</value>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <inheritdoc />
    public bool Equals(TrackedUser? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is TrackedUser other && Equals(other);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(GuildId, UserId);
    }
}
