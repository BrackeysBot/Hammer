using System;
using DSharpPlus.Entities;
using Hammer.API;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
internal sealed class Infraction : IInfraction
{
    /// <inheritdoc />
    public ulong GuildId { get; private set; }

    /// <inheritdoc />
    public long Id { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset IssuedAt { get; private set; }

    /// <inheritdoc />
    public string? Reason { get; private set; }

    /// <inheritdoc />
    public int? RuleId { get; private set; }

    /// <inheritdoc />
    public ulong StaffMemberId { get; private set; }

    /// <inheritdoc />
    public InfractionType Type { get; private set; }

    /// <inheritdoc />
    public ulong UserId { get; private set; }

    /// <summary>
    ///     Constructs a new <see cref="Infraction" />.
    /// </summary>
    /// <param name="type">The infraction type.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="staffMemberId">The staff member ID.</param>
    /// <param name="guildId">The ID of the guild in which the infraction was issued.</param>
    /// <param name="reason">The infraction reason.</param>
    /// <param name="issuedAt">Optional. The date and time at which the infraction was issued. Defaults to the current time.</param>
    /// <param name="ruleBroken">Optional. The rule which was broken.</param>
    /// <returns></returns>
    public static Infraction Create(InfractionType type, ulong userId, ulong staffMemberId, ulong guildId, string? reason,
        DateTimeOffset? issuedAt = null, int? ruleBroken = null)
    {
        return new Infraction
        {
            GuildId = guildId,
            IssuedAt = issuedAt ?? DateTimeOffset.UtcNow,
            Reason = reason,
            RuleId = ruleBroken,
            StaffMemberId = staffMemberId,
            Type = type,
            UserId = userId
        };
    }

    /// <summary>
    ///     Constructs a new <see cref="Infraction" />.
    /// </summary>
    /// <param name="type">The infraction type.</param>
    /// <param name="user">The user.</param>
    /// <param name="staffMember">The staff member.</param>
    /// <param name="guild">The guild in which the infraction was issued.</param>
    /// <param name="reason">The infraction reason.</param>
    /// <param name="issuedAt">Optional. The date and time at which the infraction was issued. Defaults to the current time.</param>
    /// <param name="ruleBroken">Optional. The rule which was broken.</param>
    /// <returns></returns>
    public static Infraction Create(InfractionType type, DiscordUser user, DiscordUser staffMember, DiscordGuild guild,
        string? reason, DateTimeOffset? issuedAt = null, int? ruleBroken = null)
    {
        return Create(type, user.Id, staffMember.Id, guild.Id, reason, issuedAt, ruleBroken);
    }

    /// <inheritdoc />
    public int CompareTo(IInfraction? other)
    {
        if (other is null) return 1;
        return IssuedAt.CompareTo(other.IssuedAt);
    }

    /// <inheritdoc />
    public bool Equals(IInfraction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is IInfraction other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }

    public static bool operator ==(Infraction? left, IInfraction? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Infraction? left, IInfraction? right)
    {
        return !Equals(left, right);
    }

    public static bool operator ==(IInfraction? left, Infraction? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IInfraction? left, Infraction? right)
    {
        return !Equals(left, right);
    }
}
