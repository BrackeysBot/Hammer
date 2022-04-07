using System;
using DSharpPlus.Entities;

// ReSharper disable UnusedMember.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace Hammer.Data;

/// <summary>
///     Represents an instance of a mute.
/// </summary>
internal sealed class Mute : IEquatable<Mute>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Mute" /> class.
    /// </summary>
    /// <param name="user">The muted user.</param>
    /// <param name="guild">The guild.</param>
    /// <param name="expirationTime">The date and time at which the mute expires.</param>
    public Mute(DiscordUser user, DiscordGuild guild, DateTimeOffset? expirationTime)
    {
        User = user;
        Guild = guild;
        ExpiresAt = expirationTime;
    }

    private Mute()
    {
        User = null!;
        Guild = null!;
    }

    /// <summary>
    ///     Gets the date and time of the mute's expiration.
    /// </summary>
    /// <value>The expiration date and time, or <see langword="null" /> if this mute does not expire.</value>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    ///     Gets the guild in which the mute is active.
    /// </summary>
    /// <value>The guild.</value>
    public DiscordGuild Guild { get; private set; }

    /// <summary>
    ///     Gets the user who was muted.
    /// </summary>
    /// <value>The muted user.</value>
    public DiscordUser User { get; private set; }

    /// <inheritdoc />
    public bool Equals(Mute? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return User == other.User && Guild == other.Guild;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Mute other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable twice NonReadonlyMemberInGetHashCode
        return HashCode.Combine(User, Guild);
    }

    public static bool operator ==(Mute? left, Mute? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Mute? left, Mute? right)
    {
        return !Equals(left, right);
    }
}
