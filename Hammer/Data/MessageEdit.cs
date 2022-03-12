using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DisCatSharp.EventArgs;

namespace Hammer.Data;

/// <summary>
///     Represents an instance of a message edit.
/// </summary>
internal sealed class MessageEdit : IEquatable<MessageEdit>
{
    /// <summary>
    ///     Gets or sets the ID of the user who originally sent the message.
    /// </summary>
    /// <value>The author's user ID.</value>
    public ulong AuthorId { get; set; }

    /// <summary>
    ///     Gets or sets the list of attachment URLs after the edit.
    /// </summary>
    /// <value>The post-edit attachment list.</value>
    public IReadOnlyList<Uri> AttachmentsAfter { get; set; } = ArraySegment<Uri>.Empty;

    /// <summary>
    ///     Gets or sets the list of attachment URLs prior to the edit.
    /// </summary>
    /// <value>The pre-edit attachment list.</value>
    public IReadOnlyList<Uri> AttachmentsBefore { get; set; } = ArraySegment<Uri>.Empty;

    /// <summary>
    ///     Gets or sets the ID of the channel in which the message was edited.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets the content of the message after the edit.
    /// </summary>
    /// <value>The post-edit content.</value>
    public string? ContentAfter { get; set; }

    /// <summary>
    ///     Gets or sets the content of the message before the edit.
    /// </summary>
    /// <value>The pre-edit content.</value>
    public string? ContentBefore { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this message was originally sent.
    /// </summary>
    /// <value>The creation timestamp.</value>
    public DateTimeOffset CreationTimestamp { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this message was edited.
    /// </summary>
    /// <value>The edit timestamp.</value>
    public DateTimeOffset EditTimestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the tracking ID.
    /// </summary>
    /// <value>The tracking ID</value>
    public long Id { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the guild in which the message was edited.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message which was edited.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Constructs a new <see cref="MessageEdit" /> by extracting values from an instance of
    ///     <see cref="MessageUpdateEventArgs" />.
    /// </summary>
    /// <param name="args">The arguments to extract.</param>
    /// <returns>A new instance of <see cref="MessageEdit" />.</returns>
    public static MessageEdit FromMessageUpdateEventArgs(MessageUpdateEventArgs args)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        return new MessageEdit
        {
            AttachmentsAfter = args.Message.Attachments.Select(a => new Uri(a.Url)).ToArray(),
            AttachmentsBefore = args.MessageBefore.Attachments.Select(a => new Uri(a.Url)).ToArray(),
            AuthorId = args.Author.Id,
            ChannelId = args.Channel.Id,
            ContentAfter = args.Message.Content,
            ContentBefore = args.MessageBefore.Content,
            CreationTimestamp = args.Message.CreationTimestamp,
            GuildId = args.Guild.Id,
            MessageId = args.Message.Id
        };
    }

    /// <inheritdoc />
    public bool Equals(MessageEdit? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is MessageEdit other && Equals(other);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
