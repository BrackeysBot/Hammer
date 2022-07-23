namespace Hammer.Configuration;

/// <summary>
///     Represents a role configuration.
/// </summary>
internal sealed class RoleConfiguration
{
    /// <summary>
    ///     Gets or sets the ID of the Administrator role.
    /// </summary>
    /// <value>The Administrator role ID.</value>
    public ulong AdministratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Guru role.
    /// </summary>
    /// <value>The Guru role ID.</value>
    public ulong GuruRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the Moderator role.
    /// </summary>
    /// <value>The Moderator role ID.</value>
    public ulong ModeratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Muted role ID.
    /// </summary>
    /// <value>The Muted role ID.</value>
    public ulong MutedRoleId { get; set; }
}
