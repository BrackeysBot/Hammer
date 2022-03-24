using System;
using DisCatSharp.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a staff or guru created note for a member.
/// </summary>
public sealed class MemberNote : IEquatable<MemberNote>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberNote" /> class.
    /// </summary>
    /// <param name="type">The note type.</param>
    /// <param name="targetUser">The user to which this note applies.</param>
    /// <param name="author">The author of the note.</param>
    /// <param name="guild">The guild in which this note was created.</param>
    /// <param name="content">The content of the note.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="type" /> is not a value defined in <see cref="MemberNoteType" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="targetUser" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="author" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="content" /> is <see langword="null" />, empty, or consists of only whitespace..</para>
    /// </exception>
    public MemberNote(MemberNoteType type, DiscordUser targetUser, DiscordUser author, DiscordGuild guild, string content)
    {
        string? trimmedContent = content?.Trim();

        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (targetUser == null) throw new ArgumentNullException(nameof(targetUser));
        if (author == null) throw new ArgumentNullException(nameof(author));
        if (guild == null) throw new ArgumentNullException(nameof(guild));
        if (string.IsNullOrWhiteSpace(trimmedContent)) throw new ArgumentNullException(nameof(content));

        Type = type;
        UserId = targetUser.Id;
        AuthorId = author.Id;
        GuildId = guild.Id;
        Content = trimmedContent;
        CreationTimestamp = DateTimeOffset.UtcNow;
    }

    private MemberNote()
    {
    }

    /// <summary>
    ///     Gets the ID of the author of this note.
    /// </summary>
    /// <value>The author's user ID.</value>
    public ulong AuthorId { get; private set; }

    /// <summary>
    ///     Gets the ID of the guild in which this note was created.
    /// </summary>
    /// <value>The guild ID.</value>
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    ///     Gets the date and time at which this note was created.
    /// </summary>
    /// <value>The date and time at which this note was created.</value>
    public DateTimeOffset CreationTimestamp { get; private set; }

    /// <summary>
    ///     Gets the ID of the guild in which this note was created.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; private set; }

    /// <summary>
    ///     Gets the ID of this note.
    /// </summary>
    /// <value>The note ID.</value>
    public long Id { get; private set; }

    /// <summary>
    ///     Gets the type of this note.
    /// </summary>
    /// <value>The type of this note.</value>
    public MemberNoteType Type { get; private set; }

    /// <summary>
    ///     Gets the ID of the user to which this note applies.
    /// </summary>
    /// <value>The target user ID.</value>
    public ulong UserId { get; private set; }

    /// <inheritdoc />
    public bool Equals(MemberNote? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is MemberNote other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
