using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GlobalConfiguration
{
    /// <summary>
    ///     Gets or sets the command prefix.
    /// </summary>
    /// <value>The command prefix.</value>
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "h[]";
}
