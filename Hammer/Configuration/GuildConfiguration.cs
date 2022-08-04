namespace Hammer.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets a value indicating whether the authors of interactions from this bot can delete their interactions.
    /// </summary>
    /// <value><see langword="true" /> to allow interaction author deletion; otherwise, <see langword="false" />.</value>
    public bool AllowInteractionAuthorDeletion { get; set; } = true;

    /// <summary>
    ///     Gets or sets the ID of the log channel.
    /// </summary>
    public ulong LogChannel { get; set; }

    /// <summary>
    ///     Gets or sets the mute configuration.
    /// </summary>
    /// <value>The mute configuration.</value>
    public MuteConfiguration Mute { get; set; } = new();

    /// <summary>
    ///     Gets or sets the guild's primary color.
    /// </summary>
    /// <value>The guild's primary color, in 24-bit RGB format.</value>
    public int PrimaryColor { get; set; } = 0x7837FF;

    /// <summary>
    ///     Gets or sets the reaction configuration.
    /// </summary>
    /// <value>The reaction configuration.</value>
    public ReactionConfiguration Reactions { get; set; } = new();

    /// <summary>
    ///     Gets or sets the role configuration.
    /// </summary>
    /// <value>The role configuration.</value>
    public RoleConfiguration Roles { get; set; } = new();

    /// <summary>
    ///     Gets or sets the guild's secondary color.
    /// </summary>
    /// <value>The guild's secondary color, in 24-bit RGB format.</value>
    public int SecondaryColor { get; set; } = 0xE33C6C;

    /// <summary>
    ///     Gets or sets the guild's tertiary color.
    /// </summary>
    /// <value>The guild's tertiary color, in 24-bit RGB format.</value>
    public int TertiaryColor { get; set; } = 0xFFE056;

    /// <summary>
    ///     Gets or sets the threshold before a message report is considered urgent.
    /// </summary>
    /// <value>The urgent report threshold.</value>
    public int UrgentReportThreshold { get; set; } = 5;
}
