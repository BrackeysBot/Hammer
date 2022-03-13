using System;
using System.Diagnostics.CodeAnalysis;
using DisCatSharp.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message report from a community user.
/// </summary>
internal class ReportedMessage : IEquatable<ReportedMessage>, IEquatable<DiscordMessage>
{
    /// <summary>
    ///     Gets or sets the ID of the report.
    /// </summary>
    /// <value>The report ID.</value>
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID the message.
    /// </summary>
    /// <value>The message user ID.</value>
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the user which reported the message.
    /// </summary>
    /// <value>The reporter's user ID.</value>
    public ulong ReporterId { get; set; }

    /// <summary>
    ///     Gets the logged message.
    /// </summary>
    public TrackedMessage Message { get; set; } = null!;

    public static bool operator ==(ReportedMessage left, DiscordMessage right) => left.Equals(right);
    public static bool operator !=(ReportedMessage left, DiscordMessage right) => !(left == right);
    public static bool operator ==(DiscordMessage left, ReportedMessage right) => right == left;
    public static bool operator !=(DiscordMessage left, ReportedMessage right) => !(left == right);

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return (obj is ReportedMessage reported && Equals(reported)) || (obj is DiscordMessage message && Equals(message));
    }

    /// <inheritdoc />
    public bool Equals(ReportedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public bool Equals(DiscordMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return MessageId == other.Id && Message.ChannelId == other.Channel.Id && Message.GuildId == other.Channel.Guild.Id;
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, MessageId);
    }
}
