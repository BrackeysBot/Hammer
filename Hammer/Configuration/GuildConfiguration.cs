using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets or sets the channel configuration.
    /// </summary>
    /// <value>The channel configuration.</value>
    [JsonPropertyName("channels")]
    public ChannelConfiguration ChannelConfiguration { get; set; } = new();

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
    ///     Gets or sets the guild's primary color.
    /// </summary>
    /// <value>The guild's primary color, in 24-bit RGB format.</value>
    [JsonPropertyName("primaryColor")]
    public int PrimaryColor { get; set; } = 0x7837FF;

    /// <summary>
    ///     Gets or sets the guild's secondary color.
    /// </summary>
    /// <value>The guild's secondary color, in 24-bit RGB format.</value>
    [JsonPropertyName("secondaryColor")]
    public int SecondaryColor { get; set; } = 0xE33C6C;

    /// <summary>
    ///     Gets or sets the guild's tertiary color.
    /// </summary>
    /// <value>The guild's tertiary color, in 24-bit RGB format.</value>
    [JsonPropertyName("tertiaryColor")]
    public int TertiaryColor { get; set; } = 0xFFE056;
}
