namespace Hammer.Configuration;

/// <summary>
///     Represents the bot configuration.
/// </summary>
internal sealed class BotConfiguration
{
    /// <summary>
    ///     Gets or sets the database configuration.
    /// </summary>
    /// <value>The database configuration.</value>
    public DatabaseConfiguration Database { get; set; } = new();
}
