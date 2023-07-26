using Humanizer;

namespace Hammer.Data;

/// <summary>
///     Specifies options to provide to <see cref="Hammer.Services.InfractionService.CreateInfractionAsync" />.
/// </summary>
internal readonly struct InfractionOptions
{
    private readonly DateTimeOffset? _expirationTime;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionOptions" /> structure.
    /// </summary>
    public InfractionOptions()
    {
        _expirationTime = null;
        NotifyUser = true;
        Reason = null;
        RuleBroken = null;
    }

    /// <summary>
    ///     Gets or initializes additional information about the infraction.
    /// </summary>
    /// <value>The additional information.</value>
    public string? AdditionalInformation { get; init; }

    /// <summary>
    ///     Gets or initializes the duration of the infraction.
    /// </summary>
    /// <value>The duration of the ban, or <see langword="null" /> to indicate the ban is permanent.</value>
    /// <remarks>It's only necessary to initialize this value, or <see cref="ExpirationTime" />, not both.</remarks>
    /// <seealso cref="ExpirationTime" />
    public TimeSpan? Duration
    {
        get => _expirationTime.HasValue ? _expirationTime - DateTimeOffset.UtcNow : null;
        init => _expirationTime = value.HasValue ? DateTimeOffset.UtcNow + value : null;
    }

    /// <summary>
    ///     Gets or initializes the duration of the infraction.
    /// </summary>
    /// <value>The duration of the ban, or <see langword="null" /> to indicate the ban is permanent.</value>
    /// <remarks>It's only necessary to initialize this value, or <see cref="Duration" />, not both.</remarks>
    /// <seealso cref="Duration" />
    public DateTimeOffset? ExpirationTime
    {
        get => _expirationTime;
        init => _expirationTime = value;
    }

    /// <summary>
    ///     Gets or initializes a value indicating whether the user should be notified of the infraction.
    /// </summary>
    /// <value><see langword="true" /> to notify the user; otherwise, <see langword="false" />.</value>
    public bool NotifyUser { get; init; }

    public string ReadableDuration => Duration.HasValue ? Duration.Value.Humanize() : "permanent";

    /// <summary>
    ///     Gets or initializes the reason for the infraction.
    /// </summary>
    /// <value>The reason.</value>
    public string? Reason { get; init; }

    /// <summary>
    ///     Gets or initializes the rule which was broken.
    /// </summary>
    /// <value>The rule broken, or <see langword="null" /> if no specific rule was broken.</value>
    public Rule? RuleBroken { get; init; }
}
