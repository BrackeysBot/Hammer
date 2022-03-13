namespace Hammer.API;

/// <summary>
///     An enumeration of infraction types.
/// </summary>
public enum InfractionType
{
    /// <summary>
    ///     Specifies that the infraction is a warning.
    /// </summary>
    Warning,

    /// <summary>
    ///     Specifies that the infraction was issued due to the deletion of a message.
    /// </summary>
    MessageDeletion,

    /// <summary>
    ///     Specifies that the infraction is a gag.
    /// </summary>
    Gag,

    /// <summary>
    ///     Specifies that the infraction is a temporary mute.
    /// </summary>
    TemporaryMute,

    /// <summary>
    ///     Specifies that the infraction is an indefinite mute.
    /// </summary>
    Mute,

    /// <summary>
    ///     Specifies that the infraction is a kick.
    /// </summary>
    Kick,

    /// <summary>
    ///     Specifies that the infraction is a temporary ban.
    /// </summary>
    TemporaryBan,

    /// <summary>
    ///     Specifies that the infraction is an indefinite ban.
    /// </summary>
    Ban
}
