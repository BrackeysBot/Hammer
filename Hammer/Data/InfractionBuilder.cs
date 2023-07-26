using DSharpPlus.Entities;

namespace Hammer.Data;

/// <summary>
///     Represents a mutable <see cref="Infraction" />. This class cannot be inherited.
/// </summary>
internal sealed class InfractionBuilder
{
    private string? _reason;
    private InfractionType? _type;
    private DiscordUser? _target;
    private DiscordGuild? _guild;
    private DiscordUser? _staffMember;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfractionBuilder" /> class.
    /// </summary>
    public InfractionBuilder()
    {
    }

    /// <summary>
    ///     Gets or sets the additional information about the infraction.
    /// </summary>
    /// <value>The additional information.</value>
    public string? AdditionalInformation { get; set; }

    /// <summary>
    ///     Gets or sets the guild to which this infraction applies.
    /// </summary>
    /// <value>The guild.</value>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public DiscordGuild Guild
    {
        get => _guild!;
        set => _guild = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets or sets the date and time at which this infraction was issued.
    /// </summary>
    /// <value>The issue date and time.</value>
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     Gets or sets the reason for the infraction.
    /// </summary>
    /// <value>The reason.</value>
    public string? Reason
    {
        get => _reason;
        set => _reason = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    ///     Gets or sets the rule which was broken.
    /// </summary>
    /// <value>The rule.</value>
    public Rule? Rule { get; set; }

    /// <summary>
    ///     Gets or sets the staff member who issued this infraction.
    /// </summary>
    /// <value>The staff member.</value>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public DiscordUser StaffMember
    {
        get => _staffMember!;
        set => _staffMember = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets or sets the target user for this infraction.
    /// </summary>
    /// <value>The target user.</value>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public DiscordUser Target
    {
        get => _target!;
        set => _target = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets or sets the type of this infraction.
    /// </summary>
    /// <value>The infraction type.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="value" /> is not a defined <see cref="InfractionType" />.
    /// </exception>
    public InfractionType Type
    {
        get => _type ?? (InfractionType) (-1);
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            _type = value;
        }
    }

    /// <summary>
    ///     Builds the infraction.
    /// </summary>
    /// <returns>A new instance of <see cref="Infraction" />.</returns>
    public Infraction Build()
    {
        if (_target is null) throw new InvalidOperationException($"{nameof(Target)} is not set!");
        if (_type is null) throw new InvalidOperationException($"{nameof(Type)} is not set!");
        if (_guild is null) throw new InvalidOperationException($"{nameof(Guild)} is not set!");

        return new Infraction
        {
            AdditionalInformation = AdditionalInformation,
            Type = Type,
            UserId = Target.Id,
            GuildId = Guild.Id,
            StaffMemberId = StaffMember.Id,
            Reason = Reason,
            IssuedAt = IssuedAt,
            RuleId = Rule?.Id,
            RuleText = Rule?.Brief ?? Rule?.Description
        };
    }

    /// <summary>
    ///     Specifies the additional information about the infraction.
    /// </summary>
    /// <param name="additionalInformation">The additional information.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithAdditionalInformation(string? additionalInformation)
    {
        AdditionalInformation = additionalInformation;
        return this;
    }

    /// <summary>
    ///     Specifies the issue date and time for this infraction.
    /// </summary>
    /// <param name="issuedAt">The issue date and time.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithIssueTime(DateTimeOffset issuedAt)
    {
        IssuedAt = issuedAt;
        return this;
    }

    /// <summary>
    ///     Specifies the guild for the infraction.
    /// </summary>
    /// <param name="guild">The guild.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithGuild(DiscordGuild guild)
    {
        Guild = guild;
        return this;
    }

    /// <summary>
    ///     Specifies the reason for the infraction.
    /// </summary>
    /// <param name="reason">The reason.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithReason(string? reason)
    {
        Reason = reason;
        return this;
    }

    /// <summary>
    ///     Specifies the rule which was broken.
    /// </summary>
    /// <param name="rule">The broken rule.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithRule(Rule? rule)
    {
        Rule = rule;
        return this;
    }

    /// <summary>
    ///     Specifies the staff member who issued this infraction.
    /// </summary>
    /// <param name="staffMember">The staff member.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithStaffMember(DiscordUser staffMember)
    {
        StaffMember = staffMember;
        return this;
    }

    /// <summary>
    ///     Specifies the target member for the infraction.
    /// </summary>
    /// <param name="member">The target member.</param>
    /// <returns>This infraction builder.</returns>
    /// <remarks>This method simultaneously sets <see cref="Target" /> and <see cref="Guild" />.</remarks>
    public InfractionBuilder WithTargetMember(DiscordMember member)
    {
        return WithTargetUser(member).WithGuild(member.Guild);
    }

    /// <summary>
    ///     Specifies the target user for the infraction.
    /// </summary>
    /// <param name="target">The target user.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithTargetUser(DiscordUser target)
    {
        Target = target;
        return this;
    }

    /// <summary>
    ///     Specifies the type for the infraction.
    /// </summary>
    /// <param name="type">The infraction type.</param>
    /// <returns>This infraction builder.</returns>
    public InfractionBuilder WithType(InfractionType type)
    {
        Type = type;
        return this;
    }
}
