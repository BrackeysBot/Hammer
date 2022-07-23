using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a temporary ban.
/// </summary>
internal sealed class TemporaryBan : IEquatable<TemporaryBan>
{
    /// <summary>
    ///     Gets the date and time of the ban's expiration.
    /// </summary>
    /// <value>The expiration date and time.</value>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    ///     Gets the guild in which the ban is active.
    /// </summary>
    /// <value>The guild.</value>
    public ulong GuildId { get; private set; }

    /// <summary>
    ///     Gets the user who was banned.
    /// </summary>
    /// <value>The banned user.</value>
    public ulong UserId { get; private set; }

    /// <summary>
    ///     Constructs a new <see cref="TemporaryBan" />.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="guildId">The ID of the guild in which the infraction was issued.</param>
    /// <param name="expiresAt">The date and time at which this ban expires.</param>
    /// <returns>The newly-created <see cref="TemporaryBan" />.</returns>
    public static TemporaryBan Create(ulong userId, ulong guildId, DateTimeOffset expiresAt)
    {
        return new TemporaryBan
        {
            GuildId = guildId,
            UserId = userId,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    ///     Constructs a new <see cref="TemporaryBan" />.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="guild">The guild in which the infraction was issued.</param>
    /// <param name="expiresAt">The date and time at which this ban expires.</param>
    /// <returns>The newly-created <see cref="TemporaryBan" />.</returns>
    public static TemporaryBan Create(DiscordUser user, DiscordGuild guild, DateTimeOffset expiresAt)
    {
        return Create(user.Id, guild.Id, expiresAt);
    }

    /// <inheritdoc />
    public bool Equals(TemporaryBan? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserId == other.UserId && GuildId == other.GuildId;
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
        return HashCode.Combine(UserId, GuildId);
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
