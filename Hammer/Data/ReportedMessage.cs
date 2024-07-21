using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message report from a community user.
/// </summary>
internal sealed class ReportedMessage : IEquatable<ReportedMessage>, IEquatable<DiscordMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportedMessage" /> class.
    /// </summary>
    /// <param name="message">The message being reported.</param>
    /// <param name="reporter">The user who reported the message.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="reporter" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="message" /> and <paramref name="reporter" /> do not belong to the same guild.
    /// </exception>
    public ReportedMessage(DiscordMessage message, DiscordMember reporter)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (reporter is null) throw new ArgumentNullException(nameof(reporter));

        if (message.Channel.Guild != reporter.Guild)
            throw new ArgumentException("Message and reporter must be in the same guild.");

        Attachments = message.Attachments.Select(a => new Uri(a.Url)).ToArray();
        AuthorId = message.Author.Id;
        ChannelId = message.Channel.Id;
        Content = message.Content;
        GuildId = message.Channel.Guild.Id;
        MessageId = message.Id;
        ReporterId = reporter.Id;
    }

    private ReportedMessage()
    {
        Attachments = ArraySegment<Uri>.Empty;
    }

    /// <summary>
    ///     Gets the attachments of the message.
    /// </summary>
    /// <value>The attachments.</value>
    public IReadOnlyList<Uri> Attachments { get; private set; }

    /// <summary>
    ///     Gets the ID of the user who sent the message.
    /// </summary>
    /// <value>The author's user ID.</value>
    public ulong AuthorId { get; private set; }

    /// <summary>
    ///     Gets the ID of the channel in which the message was sent.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; private set; }

    /// <summary>
    ///     Gets the content of the message.
    /// </summary>
    public string? Content { get; private set; }

    /// <summary>
    ///     Gets the ID of the guild in which this report was made.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; private set; }

    /// <summary>
    ///     Gets or sets the ID of the report.
    /// </summary>
    /// <value>The report ID.</value>
    public long Id { get; private set; }

    /// <summary>
    ///     Gets or sets the ID the message.
    /// </summary>
    /// <value>The message user ID.</value>
    public ulong MessageId { get; private set; }

    /// <summary>
    ///     Gets or sets the ID of the user which reported the message.
    /// </summary>
    /// <value>The reporter's user ID.</value>
    public ulong ReporterId { get; private set; }

    /// <inheritdoc />
    public bool Equals(DiscordMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return MessageId == other.Id;
    }

    /// <inheritdoc />
    public bool Equals(ReportedMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public static bool operator ==(ReportedMessage left, DiscordMessage right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReportedMessage left, DiscordMessage right)
    {
        return !(left == right);
    }

    public static bool operator ==(DiscordMessage left, ReportedMessage right)
    {
        return right == left;
    }

    public static bool operator !=(DiscordMessage left, ReportedMessage right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return (obj is ReportedMessage reported && Equals(reported)) || (obj is DiscordMessage message && Equals(message));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable twice NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Id, MessageId);
    }
}
