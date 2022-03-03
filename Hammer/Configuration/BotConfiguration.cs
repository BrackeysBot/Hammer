using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a bot configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class BotConfiguration
{
    /// <summary>
    ///     Gets or sets the channel configuration.
    /// </summary>
    /// <value>The channel configuration.</value>
    public ChannelConfiguration ChannelConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the mute configuration.
    /// </summary>
    /// <value>The mute configuration.</value>
    public MuteConfiguration MuteConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the reaction configuration.
    /// </summary>
    /// <value>The reaction configuration.</value>
    public ReactionConfiguration ReactionConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the role configuration.
    /// </summary>
    /// <value>The role configuration.</value>
    public RoleConfiguration RoleConfiguration { get; set; } = new();

    /// <summary>
    ///     Gets or sets the command prefix.
    /// </summary>
    /// <value>The command prefix.</value>
    public string Prefix { get; set; } = "h[]";
}
