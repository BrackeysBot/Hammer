using System;
using System.Text.Json.Serialization;

namespace Hammer.Configuration;

/// <summary>
///     Represents a mute configuration.
/// </summary>
internal sealed class MuteConfiguration
{
    /// <summary>
    ///     Gets or sets the duration of a gag.
    /// </summary>
    /// <value>The duration of a gag, in milliseconds.</value>
    [JsonPropertyName("gagDuration")]
    public long GagDuration { get; set; } = (long) TimeSpan.FromMinutes(5).TotalMilliseconds;
}
