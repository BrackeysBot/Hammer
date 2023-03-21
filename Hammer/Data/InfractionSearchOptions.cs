namespace Hammer.Data;

/// <summary>
///     Defines search options for displaying infraction history.
/// </summary>
public readonly struct InfractionSearchOptions
{
    /// <summary>
    ///     Gets or initializes the ID after which all infractions are ignored.
    /// </summary>
    /// <value>The after ID, or <see langword="null" /> to not filter by "before" date.</value>
    public long? IdAfter { get; init; }

    /// <summary>
    ///     Gets or initializes the ID before which all infractions are ignored.
    /// </summary>
    /// <value>The before ID, or <see langword="null" /> to not filter by "after" date.</value>
    public long? IdBefore { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the current search options are empty.
    /// </summary>
    /// <returns><see langword="true" /> if the options are empty; otherwise, <see langword="false" />.</returns>
    public bool IsEmpty => !IdAfter.HasValue && !IdBefore.HasValue &&
                           !IssuedAfter.HasValue && !IssuedBefore.HasValue &&
                           !Type.HasValue;

    /// <summary>
    ///     Gets or initializes a timestamp before which all infractions are ignored.
    /// </summary>
    /// <value>The before timestamp, or <see langword="null" /> to not filter by "after" date.</value>
    public DateTimeOffset? IssuedAfter { get; init; }

    /// <summary>
    ///     Gets or initializes a timestamp after which all infractions are ignored.
    /// </summary>
    /// <value>The before timestamp, or <see langword="null" /> to not filter by "before" date.</value>
    public DateTimeOffset? IssuedBefore { get; init; }

    /// <summary>
    ///     Gets or initializes the infraction type filter.
    /// </summary>
    /// <value>The type by which to filter.</value>
    public InfractionType? Type { get; init; }
}
