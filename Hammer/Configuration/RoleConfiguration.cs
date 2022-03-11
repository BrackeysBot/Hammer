using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a role configuration.
/// </summary>
internal sealed class RoleConfiguration
{
    /// <summary>
    ///     Gets or sets the Administrator role ID.
    /// </summary>
    /// <value>The Administrator role ID.</value>
    [JsonPropertyName("administratorRoleId")]
    public ulong AdministratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the role ID of the bot developer.
    /// </summary>
    /// <value>The bot developer role ID.</value>
    [JsonPropertyName("developerRoleId")]
    public ulong DeveloperRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Guru role ID.
    /// </summary>
    /// <value>The Guru role ID.</value>
    [JsonPropertyName("guruRoleId")]
    public ulong GuruRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Moderator role ID.
    /// </summary>
    /// <value>The Moderator role ID.</value>
    [JsonPropertyName("moderatorRoleId")]
    public ulong ModeratorRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Muted role ID.
    /// </summary>
    /// <value>The Muted role ID.</value>
    [JsonPropertyName("mutedRoleId")]
    public ulong MutedRoleId { get; set; }

    /// <summary>
    ///     Gets or sets the Staff role ID.
    /// </summary>
    /// <value>The Staff role ID.</value>
    /// <remarks>This is the role that should be applied to both Administrator and Moderator.</remarks>
    [JsonPropertyName("staffRoleId")]
    public ulong StaffRoleId { get; set; }
}
