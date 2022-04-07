using System;
using System.Diagnostics.CodeAnalysis;
using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a user which is being tracked in a guild.
/// </summary>
internal class TrackedUser : IEquatable<TrackedUser>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TrackedUser" /> class.
    /// </summary>
    /// <param name="user">The tracked user.</param>
    /// <param name="guild">The guild.</param>
    /// <param name="expirationTime">The tracking expiration time.</param>
    public TrackedUser(DiscordUser user, DiscordGuild guild, DateTimeOffset? expirationTime)
    {
        User = user;
        Guild = guild;
        ExpirationTime = expirationTime;
    }

    /// <summary>
    ///     Gets the guild in which the user is being tracked.
    /// </summary>
    /// <value>The guild.</value>
    public DiscordGuild Guild { get; private set; }

    /// <summary>
    ///     Gets the user being tracked.
    /// </summary>
    /// <value>The tracked user.</value>
    public DiscordUser User { get; private set; }

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
        return Guild == other.Guild && User == other.User;
    }

    public static bool operator ==(TrackedUser left, TrackedUser right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TrackedUser left, TrackedUser right)
    {
        return !(left == right);
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
        return HashCode.Combine(Guild, User);
    }
}
