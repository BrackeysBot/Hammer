using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a role configuration.
/// </summary>
internal sealed class RoleConfiguration
{
    /// <summary>
    ///     Gets or sets the Muted role ID.
    /// </summary>
    /// <value>The Muted role ID.</value>
    [JsonPropertyName("mutedRoleId")]
    public ulong MutedRoleId { get; set; }
}
