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
    ///     Gets or sets the rule description.
    /// </summary>
    /// <value>The description.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets the ID of the guild to which this rule belongs.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the rule ID.
    /// </summary>
    /// <value>The rule ID.</value>
    public int Id { get; internal set; }

    /// <summary>
    ///     Determines whether two <see cref="Rule" /> instances are equal.
    /// </summary>
    /// <param name="left">The first rule.</param>
    /// <param name="right">The second rule.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(Rule? left, Rule? right)
    {
        return (bool) left?.Equals(right);
    }

    /// <summary>
    ///     Determines whether two <see cref="Rule" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first rule.</param>
    /// <param name="right">The second rule.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(Rule? left, Rule? right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Returns a value indicating whether this <see cref="Rule" /> is equal to another <see cref="Rule" />.
    /// </summary>
    /// <param name="other">The other rule.</param>
    /// <returns>
    ///     <see langword="true" /> if this rule is equal to <paramref name="other" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Rule? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && GuildId == other.GuildId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is Rule other && Equals(other));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(Id, GuildId);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
