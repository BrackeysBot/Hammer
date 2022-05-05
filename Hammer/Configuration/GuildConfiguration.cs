using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets or sets the mute configuration.
    /// </summary>
    /// <value>The mute configuration.</value>
    [JsonPropertyName("mute")]
    public MuteConfiguration MuteConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the reaction configuration.
    /// </summary>
    /// <value>The reaction configuration.</value>
    [JsonPropertyName("reactions")]
    public ReactionConfiguration ReactionConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the role configuration.
    /// </summary>
    /// <value>The role configuration.</value>
    [JsonPropertyName("roles")]
    public RoleConfiguration RoleConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the threshold before a message report is considered urgent.
    /// </summary>
    /// <value>The urgent report threshold.</value>
    [JsonPropertyName("urgentReportThreshold")]
    public int UrgentReportThreshold { get; set; } = 5;
}
