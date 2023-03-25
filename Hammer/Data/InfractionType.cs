using Hammer.Resources;

namespace Hammer.Data;

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

public static class InfractionTypeExtensions
{
    public static string? GetEmbedMessage(this InfractionType type)
    {
        return type switch
        {
            InfractionType.Warning => EmbedMessages.WarningDescription,
            InfractionType.TemporaryMute => EmbedMessages.TemporaryMuteDescription,
            InfractionType.Mute => EmbedMessages.MuteDescription,
            InfractionType.Kick => EmbedMessages.KickDescription,
            InfractionType.Ban => EmbedMessages.BanDescription,
            InfractionType.TemporaryBan => EmbedMessages.TemporaryBanDescription,
            _ => null
        };
    }
}
