using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a reaction configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class ReactionConfiguration
{
    /// <summary>
    ///     Gets the delete message reaction.
    /// </summary>
    /// <value>The delete message reaction.</value>
    public string DeleteMessageReaction { get; private set; } = ":wastebasket:";

    /// <summary>
    ///     Gets the gag reaction.
    /// </summary>
    /// <value>The gag reaction.</value>
    public string GagReaction { get; private set; } = ":mute:";

    /// <summary>
    ///     Gets the history reaction.
    /// </summary>
    /// <value>The history reaction.</value>
    public string HistoryReaction { get; private set; } = ":clock4:";
}
