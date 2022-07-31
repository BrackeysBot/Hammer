namespace Hammer.Data;

/// <summary>
///     An enumeration of permission levels.
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    ///     The default permission level for community members.
    /// </summary>
    Default,

    /// <summary>
    ///     Guru permission level.
    /// </summary>
    Guru,

    /// <summary>
    ///     Moderator permission level, formerly known as Helper.
    /// </summary>
    Moderator,

    /// <summary>
    ///     Administrator permission level, which implicitly includes Brackeys Team.
    /// </summary>
    Administrator
}
