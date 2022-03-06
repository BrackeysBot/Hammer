using DisCatSharp.Entities;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="DiscordMessage" />.
/// </summary>
internal static class DiscordMessageExtensions
{
    /// <summary>
    ///     Acknowledges a message by reacting to it.
    /// </summary>
    /// <param name="message">The message to acknowledge.</param>
    public static Task AcknowledgeAsync(this DiscordMessage message)
    {
        return message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
    }
}
