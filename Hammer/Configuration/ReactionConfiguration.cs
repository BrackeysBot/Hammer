using System.Text.Json.Serialization;

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
    [JsonPropertyName("deleteMessageReaction")]
    public string DeleteMessageReaction { get; set; } = ":wastebasket:";

    /// <summary>
    ///     Gets or sets the gag reaction.
    /// </summary>
    /// <value>The gag reaction.</value>
    [JsonPropertyName("gagReaction")]
    public string GagReaction { get; set; } = ":mute:";

    /// <summary>
    ///     Gets or sets the history reaction.
    /// </summary>
    /// <value>The history reaction.</value>
    [JsonPropertyName("historyReaction")]
    public string HistoryReaction { get; set; } = ":clock4:";

    /// <summary>
    ///     Gets or sets the report reaction.
    /// </summary>
    /// <value>The report reaction.</value>
    [JsonPropertyName("reportReaction")]
    public string ReportReaction { get; set; } = ":triangular_flag_on_post:";

    /// <summary>
    ///     Gets or sets the track reaction.
    /// </summary>
    /// <value>The track reaction.</value>
    [JsonPropertyName("trackReaction")]
    public string TrackReaction { get; set; } = ":mag:";
}
