using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a mute configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class MuteConfiguration
{
    /// <summary>
    ///     Gets the duration of a gag.
    /// </summary>
    /// <value>The duration of a gag, in milliseconds.</value>
    public long GagDuration { get; private set; } = (long) TimeSpan.FromMinutes(5).TotalMilliseconds;
}
