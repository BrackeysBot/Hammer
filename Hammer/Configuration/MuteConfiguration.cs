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
    public long GagDuration { get; set; } = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

    /// <summary>
    ///     Gets or sets the maximum mute duration for moderators.
    /// </summary>
    /// <value>The maximum mute duration.</value>
    public long? MaxModeratorMuteDuration { get; set; } = (long)TimeSpan.FromDays(14).TotalMilliseconds;
}
