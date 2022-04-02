using System;
using DSharpPlus.Entities;

namespace Hammer.EventData;

/// <summary>
///     Provides event data pertaining to a <see cref="DiscordUser" /> being muted or unmuted.
/// </summary>
internal sealed class MutedEventArgs : EventArgs
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MutedEventArgs" /> class.
    /// </summary>
    /// <param name="user">The <see cref="DiscordUser" />.</param>
    /// <param name="guild">The <see cref="DiscordGuild" />.</param>
    internal MutedEventArgs(DiscordUser user, DiscordGuild guild)
    {
        User = user;
        Guild = guild;
    }

    /// <summary>
    ///     Gets the guild.
    /// </summary>
    /// <value>The guild.</value>
    public DiscordGuild Guild { get; }

    /// <summary>
    ///     Gets the user.
    /// </summary>
    /// <value>The user.</value>
    public DiscordUser User { get; }
}
