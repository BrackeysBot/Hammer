using System;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Data;

/// <summary>
///     Represents a guild rule.
/// </summary>
internal sealed class Rule : IEquatable<Rule>
{
    /// <summary>
    ///     Gets or sets the brief rule description, if any.
    /// </summary>
    /// <value>The rule brief.</value>
    public string? Brief { get; set; }

    /// <summary>
    ///     Gets or sets the rule content.
    /// </summary>
    /// <value>The content.</value>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the ID of the guild to which this rule belongs.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the rule ID.
    /// </summary>
    /// <value>The rule ID.</value>
    public int Id { get; set; }

    /// <inheritdoc />
    public bool Equals(Rule? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && GuildId == other.GuildId;
    }

    public static bool operator ==(Rule? left, Rule? right)
    {
        return (bool) left?.Equals(right);
    }

    public static bool operator !=(Rule? left, Rule? right)
    {
        return !(left == right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is Rule other && Equals(other));
    }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, GuildId);
    }
}
