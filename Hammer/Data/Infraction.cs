using System;
using System.Diagnostics.CodeAnalysis;
using Hammer.API;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
internal class Infraction : IInfraction
{
    /// <summary>
    ///     Gets or sets the time at which this infraction expires.
    /// </summary>
    /// <value>The expiration time.</value>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the infraction.
    /// </summary>
    /// <value>The ID of the infraction.</value>
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID fo the guild in which this infraction was issued.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who issued this infraction.
    /// </summary>
    /// <value>The ID of the staff member who issued this infraction.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the reason for the infraction.
    /// </summary>
    /// <value>The reason for the infraction.</value>
    public string? Reason { get; set; }

    /// <summary>
    ///     Gets or sets the time of the infraction.
    /// </summary>
    /// <value>The time of the infraction.</value>
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the type of the infraction.
    /// </summary>
    /// <value>The type of the infraction.</value>
    public InfractionType Type { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who received this infraction.
    /// </summary>
    /// <value>The ID of the user who received this infraction.</value>
    public ulong UserId { get; set; }

    /// <inheritdoc />
    public bool Equals(IInfraction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other is not Infraction infraction) return false;
        return Id == infraction.Id;
    }

    /// <inheritdoc />
    public int CompareTo(IInfraction? other)
    {
        if (other is null) return 1;
        return IssuedAt.CompareTo(other.IssuedAt);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is IInfraction other && Equals(other);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
