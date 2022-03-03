using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a bot configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class BotConfiguration
{
    /// <summary>
    ///     Gets the channel configuration.
    /// </summary>
    /// <value>The channel configuration.</value>
    public ChannelConfiguration ChannelConfiguration { get; private set; } = new();

    /// <summary>
    ///     Gets the mute configuration.
    /// </summary>
    /// <value>The mute configuration.</value>
    public MuteConfiguration MuteConfiguration { get; private set; } = new();

    /// <summary>
    ///     Gets the reaction configuration.
    /// </summary>
    /// <value>The reaction configuration.</value>
    public ReactionConfiguration ReactionConfiguration { get; private set; } = new();

    /// <summary>
    ///     Gets the role configuration.
    /// </summary>
    /// <value>The role configuration.</value>
    public RoleConfiguration RoleConfiguration { get; private set; } = new();

    /// <summary>
    ///     Gets the command prefix.
    /// </summary>
    /// <value>The command prefix.</value>
    public string Prefix { get; private set; } = "h[]";
}
