using System;
using DSharpPlus.Entities;
using Hammer.API;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
internal sealed class Infraction : IInfraction
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Infraction" /> class.
    /// </summary>
    /// <param name="type">The type of the infraction.</param>
    /// <param name="user">The user to whom the infraction is issued.</param>
    /// <param name="guild">The guild in which the infraction is issued.</param>
    /// <param name="staffMember">The staff member who issued the infraction.</param>
    /// <param name="issuedAt">The date and time at which the infraction was issued.</param>
    /// <param name="reason">The reason for the infraction.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="type" /> is not a defined <see cref="InfractionType" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public Infraction(InfractionType type, DiscordUser user, DiscordGuild guild, DiscordUser staffMember, DateTimeOffset issuedAt,
        string? reason)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        Guild = guild ?? throw new ArgumentNullException(nameof(guild));
        IssuedAt = issuedAt;
        Reason = reason;
        StaffMember = staffMember ?? throw new ArgumentNullException(nameof(staffMember));
        Type = type;
        User = user ?? throw new ArgumentNullException(nameof(user));
    }

    private Infraction()
    {
    }

    /// <inheritdoc />
    public DiscordGuild Guild { get; private set; }

    /// <inheritdoc />
    public long Id { get; private set; }

    /// <inheritdoc />
    public bool IsRedacted { get; internal set; }

    /// <inheritdoc />
    public DateTimeOffset IssuedAt { get; private set; }

    /// <inheritdoc />
    public string? Reason { get; private set; }

    /// <inheritdoc />
    public DiscordUser StaffMember { get; private set; }

    /// <inheritdoc />
    public InfractionType Type { get; private set; }

    /// <inheritdoc />
    public DiscordUser User { get; private set; }

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
