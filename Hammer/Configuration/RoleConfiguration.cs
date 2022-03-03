namespace Hammer.Configuration;

/// <summary>
///     Represents a role configuration.
/// </summary>
internal sealed class RoleConfiguration
{
    /// <summary>
    ///     Gets the Administrator role ID.
    /// </summary>
    /// <value>The Administrator role ID.</value>
    public ulong AdministratorRoleId { get; set; }

    /// <summary>
    ///     Gets the role ID of the bot developer.
    /// </summary>
    /// <value>The bot developer role ID.</value>
    public ulong DeveloperRoleId { get; set; }

    /// <summary>
    ///     Gets the Guru role ID.
    /// </summary>
    /// <value>The Guru role ID.</value>
    public ulong GuruRoleId { get; set; }

    /// <summary>
    ///     Gets the Moderator role ID.
    /// </summary>
    /// <value>The Moderator role ID.</value>
    public ulong ModeratorRoleId { get; set; }
    
    /// <summary>
    ///     Gets the Muted role ID.
    /// </summary>
    /// <value>The Muted role ID.</value>
    public ulong MutedRoleId { get; set; }
    
    /// <summary>
    ///     Gets the Staff role ID.
    /// </summary>
    /// <value>The Staff role ID.</value>
    /// <remarks>This is the role that should be applied to both Administrator and Moderator.</remarks>
    public ulong StaffRoleId { get; set; }
}
