namespace Hammer.Configuration;

/// <summary>
///     Represents a reaction configuration.
/// </summary>
internal sealed class ReactionConfiguration
{
    /// <summary>
    ///     Gets or sets the delete message reaction.
    /// </summary>
    /// <value>The delete message reaction.</value>
    public string DeleteMessageReaction { get; set; } = ":wastebasket:";

    /// <summary>
    ///     Gets or sets the gag reaction.
    /// </summary>
    /// <value>The gag reaction.</value>
    public string GagReaction { get; set; } = ":mute:";

    /// <summary>
    ///     Gets or sets the history reaction.
    /// </summary>
    /// <value>The history reaction.</value>
    public string HistoryReaction { get; set; } = ":clock4:";

    /// <summary>
    ///     Gets or sets the report reaction.
    /// </summary>
    /// <value>The report reaction.</value>
    public string ReportReaction { get; set; } = ":triangular_flag_on_post:";
}
