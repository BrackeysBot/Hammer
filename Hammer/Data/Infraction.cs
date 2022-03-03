using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>
[Table("Infractions")]
internal record Infraction
{
    /// <summary>
    ///     Gets or sets the ID of the infraction.
    /// </summary>
    /// <value>The ID of the infraction.</value>
    [Key, Column("id", Order = 1)]
    public int Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID fo the guild in which this infraction was issued.
    /// </summary>
    /// <value>The guild ID.</value>
    [Column("guildId", Order = 2)]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who issued this infraction.
    /// </summary>
    /// <value>The ID of the staff member who issued this infraction.</value>
    [Column("staffMemberId", Order = 4)]
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the reason for the infraction.
    /// </summary>
    /// <value>The reason for the infraction.</value>
    [Column("reason", Order = 6)]
    public string? Reason { get; set; }

    /// <summary>
    ///     Gets or sets the time of the infraction.
    /// </summary>
    /// <value>The time of the infraction.</value>
    [Column("time", Order = 7)]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the type of the infraction.
    /// </summary>
    /// <value>The type of the infraction.</value>
    [Column("type", Order = 5)]
    public InfractionType Type { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user who received this infraction.
    /// </summary>
    /// <value>The ID of the user who received this infraction.</value>
    [Column("userId", Order = 3)]
    public ulong UserId { get; set; }
}
