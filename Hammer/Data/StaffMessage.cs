using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Data;

/// <summary>
///     Represents a message sent from a staff member to a community member.
/// </summary>
[Table("StaffMessages")]
internal sealed class StaffMessage : IEquatable<StaffMessage>
{
    /// <summary>
    ///     Gets or sets the content of the message.
    /// </summary>
    /// <value>The message content.</value>
    [Column("content", Order = 5)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    ///     Gets or sets the ID of the guild from which this message was sent.
    /// </summary>
    /// <value>The guild ID.</value>
    [Column("guildId", Order = 2)]
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the message.
    /// </summary>
    [Key, Column("id", Order = 1)]
    public long Id { get; set; }

    /// <summary>
    ///     Gets the user ID of the staff member who sent the message.
    /// </summary>
    /// <value>The staff member's user ID.</value>
    [Column("staffMemberId", Order = 3)]
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets the user ID of the user who received the message.
    /// </summary>
    /// <value>The target user's user ID.</value>
    [Column("targetUserId", Order = 4)]
    public ulong RecipientId { get; set; }

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
        return ReferenceEquals(this, obj) || obj is StaffMessage other && Equals(other);
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
