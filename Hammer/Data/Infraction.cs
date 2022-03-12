using System;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
public record Infraction
{
    /// <summary>
    ///     Gets or sets the ID of the infraction.
    /// </summary>
    /// <value>The ID of the infraction.</value>
    public int Id { get; set; }

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
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

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
}
