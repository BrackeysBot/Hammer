namespace Hammer.Configuration;

/// <summary>
///     Represents the database configuration.
/// </summary>
internal sealed class DatabaseConfiguration
{
    /// <summary>
    ///     Gets or sets the database provider.
    /// </summary>
    /// <value>The database provider.</value>
    public string? Provider { get; set; } = "sqlite";
}
