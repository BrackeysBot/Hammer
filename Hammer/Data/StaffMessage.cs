using System;
using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a message sent from a staff member to a community member.
/// </summary>
internal sealed class StaffMessage : IEquatable<StaffMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StaffMessage" /> class.
    /// </summary>
    /// <param name="staffMember">The staff member who sent the message.</param>
    /// <param name="recipient">The community member who received the message.</param>
    /// <param name="content">The content of the message.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="recipient" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="content" /> is <see langword="null" />, empty, or consists of only whitespace.</para>
    /// </exception>
    public StaffMessage(DiscordMember staffMember, DiscordUser recipient, string content)
    {
        StaffMember = staffMember ?? throw new ArgumentNullException(nameof(staffMember));
        Recipient = recipient ?? throw new ArgumentNullException(nameof(recipient));
        Content = string.IsNullOrWhiteSpace(content) ? throw new ArgumentNullException(nameof(content)) : content;
        Guild = staffMember.Guild;
    }

    private StaffMessage()
    {
        Content = string.Empty;
        Guild = null!;
        Recipient = null!;
        StaffMember = null!;
    }

    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    public string Content { get; private set; }

    /// <summary>
    ///     Gets or sets the ID of the guild from which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    public DiscordGuild Guild { get; private set; }

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    ///     Gets the user who is in receipt of this message.
    /// </summary>
    /// <value>The message recipient.</value>
    public DiscordUser Recipient { get; private set; }

    /// <summary>
    ///     Gets the user ID of the staff member who sent the message.
    /// </summary>
    /// <value>The staff member's user ID.</value>
    public DiscordUser StaffMember { get; private set; }

    /// <inheritdoc />
    public bool Equals(StaffMessage? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is StaffMessage other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
