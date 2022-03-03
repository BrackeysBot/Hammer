using System.Diagnostics.CodeAnalysis;

namespace Hammer.Configuration;

/// <summary>
///     Represents a channel configuration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Immutability. Setter accessible via DI")]
internal sealed class ChannelConfiguration
{
    /// <summary>
    ///     Gets or sets the ID of the staff log channel.
    /// </summary>
    /// <value>The ID of the staff log channel.</value>
    public ulong LogChannelId { get; set; }
}
