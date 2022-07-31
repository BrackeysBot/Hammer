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
    ///     Gets the date and time of the mute's expiration.
    /// </summary>
    /// <value>The expiration date and time, or <see langword="null" /> if this mute does not expire.</value>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    ///     Gets the guild in which the mute is active.
    /// </summary>
    /// <value>The guild.</value>
    public ulong GuildId { get; private set; }

    /// <summary>
    ///     Gets the user who was muted.
    /// </summary>
    /// <value>The muted user.</value>
    public ulong UserId { get; private set; }

    /// <summary>
    ///     Constructs a new <see cref="Mute" />.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="guildId">The ID of the guild in which the infraction was issued.</param>
    /// <param name="expiresAt">Optional. The date and time at which this mute expires, if any.</param>
    /// <returns>The newly-created <see cref="Mute" />.</returns>
    public static Mute Create(ulong userId, ulong guildId, DateTimeOffset? expiresAt = null)
    {
        return new Mute
        {
            GuildId = guildId,
            UserId = userId,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    ///     Constructs a new <see cref="Mute" />.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="guild">The guild in which the infraction was issued.</param>
    /// <param name="expiresAt">Optional. The date and time at which this mute expires, if any.</param>
    /// <returns>The newly-created <see cref="Mute" />.</returns>
    public static Mute Create(DiscordUser user, DiscordGuild guild, DateTimeOffset? expiresAt = null)
    {
        return Create(user.Id, guild.Id, expiresAt);
    }

    /// <inheritdoc />
    public bool Equals(Mute? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserId == other.UserId && GuildId == other.GuildId;
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
        return HashCode.Combine(UserId, GuildId);
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
