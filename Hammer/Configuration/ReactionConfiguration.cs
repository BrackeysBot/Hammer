namespace Hammer.Configuration;

/// <summary>
///     Represents a reaction configuration.
/// </summary>
internal sealed class ReactionConfiguration
{
    /// <summary>
    ///     Gets the delete message reaction.
    /// </summary>
    /// <value>The delete message reaction.</value>
    public string DeleteMessageReaction { get; set; } = ":wastebasket:";

    /// <summary>
    ///     Gets the gag reaction.
    /// </summary>
    /// <value>The gag reaction.</value>
    public string GagReaction { get; set; } = ":mute:";

    /// <summary>
    ///     Gets the history reaction.
    /// </summary>
    /// <value>The history reaction.</value>
    public string HistoryReaction { get; set; } = ":clock4:";
}
