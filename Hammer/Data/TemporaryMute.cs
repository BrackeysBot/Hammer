using System;
using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents an instance of a temporary mute.
/// </summary>
internal record TemporaryMute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryMute" /> class.
    /// </summary>
    /// <param name="user">The muted user.</param>
    /// <param name="time">The time at which the member was muted.</param>
    /// <param name="expirationTime">The time at which the mute expires.</param>
    public TemporaryMute(DiscordUser user, DateTimeOffset time, DateTimeOffset expirationTime)
    {
        User = user;
        Time = time;
        ExpirationTime = expirationTime;
    }

    /// <summary>
    ///     Gets the time at which the mute expires.
    /// </summary>
    /// <value>The expiration time.</value>
    public DateTimeOffset ExpirationTime { get; init; }

    /// <summary>
    ///     Gets the time at which the member was muted.
    /// </summary>
    /// <value>The mute time.</value>
    public DateTimeOffset Time { get; init; }

    /// <summary>
    ///     Gets the user which was muted.
    /// </summary>
    /// <value>The muted user.</value>
    public DiscordUser User { get; init; }
}
