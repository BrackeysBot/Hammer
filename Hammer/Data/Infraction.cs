namespace Hammer.Data;

/// <summary>
///     Represents an infraction.
/// </summary>It'
internal sealed class Infraction : IEquatable<Infraction>, IComparable<Infraction>, IComparable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Infraction" /> class.
    /// </summary>
    public Infraction()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Infraction" /> class by copying the values from another instance.
    /// </summary>
    /// <param name="other">The infraction to copy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="other" /> is <see langword="null" />.</exception>
    public Infraction(Infraction other)
    {
        if (other is null) throw new ArgumentNullException(nameof(other));
        
        GuildId = other.GuildId;
        Id = other.Id;
        IssuedAt = other.IssuedAt;
        Reason = other.Reason;
        RuleId = other.RuleId;
        StaffMemberId = other.StaffMemberId;
        Type = other.Type;
        UserId = other.UserId;
    }

    /// <summary>
    ///     Gets the ID of the guild in which this infraction was issued.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the infraction.
    /// </summary>
    /// <value>The infraction ID.</value>
    public long Id { get; internal set; }

    /// <summary>
    ///     Gets the date and time at which the infraction was issued.
    /// </summary>
    /// <value>The infraction timestamp.</value>
    public DateTimeOffset IssuedAt { get; internal set; }

    /// <summary>
    ///     Gets the reason for the infraction.
    /// </summary>
    /// <value>The reason for the infraction.</value>
    public string? Reason { get; internal set; }

    /// <summary>
    ///     Gets the ID of the rule which was broken.
    /// </summary>
    /// <value>The broken rule ID.</value>
    public int? RuleId { get; internal set; }

    /// <summary>
    ///     Gets the ID of the staff member who issued the infraction.
    /// </summary>
    /// <value>The ID of the staff member.</value>
    public ulong StaffMemberId { get; internal set; }

    /// <summary>
    ///     Gets the type of the infraction.
    /// </summary>
    /// <value>The type of the infraction.</value>
    public InfractionType Type { get; internal set; }

    /// <summary>
    ///     Gets the ID of the user to whom this infraction was issued.
    /// </summary>
    /// <value>The ID of the user who received the infraction.</value>
    public ulong UserId { get; internal set; }

    /// <summary>
    ///     Determines whether two <see cref="Infraction" /> instances are equal.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(Infraction? left, Infraction? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two <see cref="Infraction" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(Infraction? left, Infraction? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Determines whether one <see cref="Infraction" /> was created before another <see cref="Infraction" />.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is was created before <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator <(Infraction? left, Infraction? right)
    {
        return Comparer<Infraction>.Default.Compare(left, right) < 0;
    }

    /// <summary>
    ///     Determines whether one <see cref="Infraction" /> was created after another <see cref="Infraction" />.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is was created after <paramref name="right" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator >(Infraction? left, Infraction? right)
    {
        return Comparer<Infraction>.Default.Compare(left, right) > 0;
    }

    /// <summary>
    ///     Determines whether one <see cref="Infraction" /> was created at the same time as - or before - another
    ///     <see cref="Infraction" />.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is was created at the same time as - or before -
    ///     <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(Infraction? left, Infraction? right)
    {
        return Comparer<Infraction>.Default.Compare(left, right) <= 0;
    }

    /// <summary>
    ///     Determines whether one <see cref="Infraction" /> was created at the same time as - or after - another
    ///     <see cref="Infraction" />.
    /// </summary>
    /// <param name="left">The first infraction.</param>
    /// <param name="right">The second infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> is was created at the same time as - or after -
    ///     <paramref name="right" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(Infraction? left, Infraction? right)
    {
        return Comparer<Infraction>.Default.Compare(left, right) >= 0;
    }

    /// <summary>
    ///     Compares the current infraction to another infraction.
    /// </summary>
    /// <param name="other">The infraction against which to compare.</param>
    /// <returns>
    ///     A 32-bit signed integer that indicates whether this instance precedes, follows, or appears in the same position in the
    ///     sort order as the <paramref name="other"/> parameter.
    ///
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Value</term>
    ///             <description>Condition</description>
    ///         </listheader>
    ///         <item>
    ///             <term>Less than zero</term>
    ///             <description>This instance precedes <paramref name="other" />.</description>
    ///         </item>
    ///         <item>
    ///             <term>Zero</term>
    ///             <description>This instance has the same position in the sort order as <paramref name="other" />.</description>
    ///         </item>
    ///         <item>
    ///             <term>Greater than zero</term>
    ///             <description>
    ///                 <para>This instance follows <paramref name="other" />.</para>
    ///                 -or-
    ///                 <para><paramref name="other" /> is <see langword="null" />.</para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    public int CompareTo(Infraction? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return IssuedAt.CompareTo(other.IssuedAt);
    }

    /// <summary>
    ///     Compares the current infraction to another object.
    /// </summary>
    /// <param name="obj">The object against which to compare.</param>
    /// <returns>
    ///     A 32-bit signed integer that indicates whether this instance precedes, follows, or appears in the same position in the
    ///     sort order as the <paramref name="obj"/> parameter.
    ///
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Value</term>
    ///             <description>Condition</description>
    ///         </listheader>
    ///         <item>
    ///             <term>Less than zero</term>
    ///             <description>This instance precedes <paramref name="obj" />.</description>
    ///         </item>
    ///         <item>
    ///             <term>Zero</term>
    ///             <description>This instance has the same position in the sort order as <paramref name="obj" />.</description>
    ///         </item>
    ///         <item>
    ///             <term>Greater than zero</term>
    ///             <description>
    ///                 <para>This instance follows <paramref name="obj" />.</para>
    ///                 -or-
    ///                 <para><paramref name="obj" /> is <see langword="null" />.</para>
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="obj" /> is not of type <see cref="Infraction" />.</exception>
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        if (obj is not Infraction other)
            throw new ArgumentException($"Object must be of type {nameof(Infraction)}");

        return CompareTo(other);
    }

    /// <summary>
    ///     Returns a value indicating whether this <see cref="Infraction" /> is equal to another <see cref="Infraction" />.
    /// </summary>
    /// <param name="other">The other infraction.</param>
    /// <returns>
    ///     <see langword="true" /> if this infraction is equal to <paramref name="other" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Infraction? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Infraction other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
