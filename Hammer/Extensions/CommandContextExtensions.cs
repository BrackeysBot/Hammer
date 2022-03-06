using DisCatSharp.CommandsNext;

namespace Hammer.Extensions;

/// <summary>
///     Extension methods for <see cref="CommandContext" />.
/// </summary>
internal static class CommandContextExtensions
{
    /// <summary>
    ///     Acknowledges the message provided by a <see cref="CommandContext" /> by reacting to it.
    /// </summary>
    /// <param name="context">The command context.</param>
    public static Task AcknowledgeAsync(this CommandContext context)
    {
        return context.Message.AcknowledgeAsync();
    }
}
