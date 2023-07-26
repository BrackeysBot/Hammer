using DSharpPlus.Entities;

namespace Hammer;

internal sealed class MentionUtility
{
    /// <summary>
    ///     Replaces raw channel mentions with Discord channel mentions.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <param name="input">The input to sanitize.</param>
    /// <returns>The sanitized input.</returns>
    public static string ReplaceChannelMentions(DiscordGuild guild, string input)
    {
        foreach (DiscordChannel channel in guild.Channels.Values)
        {
            input = input.Replace($"#{channel.Name}", channel.Mention);
        }

        return input;
    }
}
