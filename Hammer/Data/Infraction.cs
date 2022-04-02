using System;
using System.ComponentModel.DataAnnotations.Schema;
using DSharpPlus.Entities;
using Hammer.API;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
internal sealed class Infraction : IInfraction
{
    /// <inheritdoc />
    [NotMapped]
    public DiscordGuild Guild { get; internal set; } = null!;

    /// <inheritdoc />
    public ulong GuildId { get; internal set; }

    /// <inheritdoc />
    public long Id { get; internal set; }

    /// <inheritdoc />
    public bool IsRedacted { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset IssuedAt { get; internal set; }

    /// <inheritdoc />
    public string? Reason { get; internal set; }

    /// <inheritdoc />
    [NotMapped]
    public DiscordUser StaffMember { get; internal set; } = null!;

    /// <inheritdoc />
    public ulong StaffMemberId { get; internal set; }

    /// <inheritdoc />
    public InfractionType Type { get; internal set; }

    /// <inheritdoc />
    [NotMapped]
    public DiscordUser User { get; internal set; } = null!;

    /// <inheritdoc />
    public ulong UserId { get; internal set; }

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
