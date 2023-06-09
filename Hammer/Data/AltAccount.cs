namespace Hammer.Data;

/// <summary>
///     Represents an alt account record.
/// </summary>
internal sealed record AltAccount
{
    /// <summary>
    ///     Gets or sets the ID of the alt account.
    /// </summary>
    /// <value>The alt account ID.</value>
    public ulong AltId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which the alt account was registered.
    /// </summary>
    /// <value>The date and time at which the alt account was registered.</value>
    public DateTimeOffset RegisteredAt { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member who added the alt account.
    /// </summary>
    /// <value>The staff member ID.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the primary account.
    /// </summary>
    /// <value>The primary account ID.</value>
    public ulong UserId { get; set; }
}
