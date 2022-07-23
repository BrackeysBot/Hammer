namespace Hammer.Data;

/// <summary>
///     An enumeration of possible states for a <see cref="TrackedMessage" /> to be in.
/// </summary>
[Flags]
internal enum MessageTrackState
{
    /// <summary>
    ///     Specifies that the message is not being tracked.
    /// </summary>
    NotTracked,

    /// <summary>
    ///     Specifies that the message is tracked.
    /// </summary>
    Tracked,

    /// <summary>
    ///     Specifies that the message no longer exists.
    /// </summary>
    Deleted
}
