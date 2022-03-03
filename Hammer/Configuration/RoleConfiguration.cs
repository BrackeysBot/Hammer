using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a role configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class RoleConfiguration
{
    /// <summary>
    ///     Gets or sets the Administrator role ID.
    /// </summary>
    /// <value>The Administrator role ID.</value>
    public ulong AdministratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the role ID of the bot developer.
    /// </summary>
    /// <value>The bot developer role ID.</value>
    public ulong DeveloperRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Guru role ID.
    /// </summary>
    /// <value>The Guru role ID.</value>
    public ulong GuruRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Moderator role ID.
    /// </summary>
    /// <value>The Moderator role ID.</value>
    public ulong ModeratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Muted role ID.
    /// </summary>
    /// <value>The Muted role ID.</value>
    public ulong MutedRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Staff role ID.
    /// </summary>
    /// <value>The Staff role ID.</value>
    /// <remarks>This is the role that should be applied to both Administrator and Moderator.</remarks>
    public ulong StaffRoleId { get; set; }
}
