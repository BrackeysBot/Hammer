using System;
using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a temporary ban.
/// </summary>
internal sealed class TemporaryBan : IEquatable<TemporaryBan>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryBan" /> class.
    /// </summary>
    /// <param name="user">The banned user.</param>
    /// <param name="guild">The guild.</param>
    /// <param name="expiresAt">The date and time at which the ban expires.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public TemporaryBan(DiscordUser user, DiscordGuild guild, DateTimeOffset expiresAt)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        Guild = guild ?? throw new ArgumentNullException(nameof(guild));
        ExpiresAt = expiresAt;
    }

    private TemporaryBan()
    {
        User = null!;
        Guild = null!;
    }

    /// <summary>
    ///     Gets the date and time of the ban's expiration.
    /// </summary>
    /// <value>The expiration date and time.</value>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    ///     Gets the guild in which the ban is active.
    /// </summary>
    /// <value>The guild.</value>
    public DiscordGuild Guild { get; private set; }

    /// <summary>
    ///     Gets the user who was banned.
    /// </summary>
    /// <value>The banned user.</value>
    public DiscordUser User { get; private set; }

    /// <inheritdoc />
    public bool Equals(TemporaryBan? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return User == other.User && Guild == other.Guild;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TemporaryBan other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable twice NonReadonlyMemberInGetHashCode
        return HashCode.Combine(User, Guild);
    }

    public static bool operator ==(TemporaryBan? left, TemporaryBan? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TemporaryBan? left, TemporaryBan? right)
    {
        return !Equals(left, right);
    }
}
