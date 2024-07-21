namespace Hammer.Configuration;

/// <summary>
///     Represents the database configuration.
/// </summary>
internal sealed class DatabaseConfiguration
{
    /// <summary>
    ///     Gets or sets the database name.
    /// </summary>
    /// <value>The database name.</value>
    public string? Database { get; set; }

    /// <summary>
    ///     Gets or sets the remote host.
    /// </summary>
    /// <value>The remote host.</value>
    public string? Host { get; set; }

    /// <summary>
    ///     Gets or sets the login password.
    /// </summary>
    /// <value>The login password.</value>
    public string? Password { get; set; }

    /// <summary>
    ///     Gets or sets the remote port.
    /// </summary>
    /// <value>The remote port.</value>
    public int? Port { get; set; }

    /// <summary>
    ///     Gets or sets the database provider.
    /// </summary>
    /// <value>The database provider.</value>
    public string? Provider { get; set; } = "sqlite";

    /// <summary>
    ///     Gets or sets the table prefix.
    /// </summary>
    /// <value>The table prefix.</value>
    public string TablePrefix { get; set; } = "Hammer_";

    /// <summary>
    ///     Gets or sets the login username.
    /// </summary>
    /// <value>The login username.</value>
    public string? Username { get; set; }
}
