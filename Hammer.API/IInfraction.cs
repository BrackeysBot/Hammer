using System;

namespace Hammer.API;

/// <summary>
///     Represents an infraction.
/// </summary>
public interface IInfraction : IEquatable<IInfraction>, IComparable<IInfraction>
{
    /// <summary>
    ///     Gets the ID of the guild in which this infraction was issued.
    /// </summary>
    /// <value>The guild ID.</value>
    ulong GuildId { get; }

    /// <summary>
    ///     Gets the ID of this infraction.
    /// </summary>
    /// <value>The infraction ID.</value>
    long Id { get; }

    /// <summary>
    ///     Gets a value indicating whether this infraction has been redacted.
    /// </summary>
    /// <value><see langword="true" /> if this infraction has been redacted; otherwise, <see langword="false" />.</value>
    /// <remarks>This applies to an infraction which has been deleted, or a permanent mute/ban which has been revoked.</remarks>
    bool IsRedacted { get; }

    /// <summary>
    ///     Gets the date and time at which this infraction was issued.
    /// </summary>
    /// <value>The issue date and time.</value>
    DateTimeOffset IssuedAt { get; }

    /// <summary>
    ///     Gets the reason for this infraction.
    /// </summary>
    /// <value>The reason, or <see langword="null" /> if no reason is specified.</value>
    string? Reason { get; }

    /// <summary>
    ///     Gets the ID of the staff member who issued this infraction.
    /// </summary>
    /// <value>The staff member ID.</value>
    ulong StaffMemberId { get; }

    /// <summary>
    ///     Gets the type of this infraction.
    /// </summary>
    /// <value>The infraction type.</value>
    InfractionType Type { get; }

    /// <summary>
    ///     Gets the ID of the user who holds this infraction.
    /// </summary>
    /// <value>The user ID.</value>
    ulong UserId { get; }
}
