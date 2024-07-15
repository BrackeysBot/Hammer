using DSharpPlus.Entities;
using Hammer.Data;
using Hammer.Extensions;
using X10D.Text;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages member warnings.
/// </summary>
internal sealed class WarningService
{
    private readonly DiscordLogService _logService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarningService" /> class.
    /// </summary>
    public WarningService(DiscordLogService logService, InfractionService infractionService, RuleService ruleService)
    {
        _logService = logService;
        _infractionService = infractionService;
        _ruleService = ruleService;
    }

    /// <summary>
    ///     Warns a user with the specified reason.
    /// </summary>
    /// <param name="user">The user to warn.</param>
    /// <param name="issuer">The staff member who issued the warning.</param>
    /// <param name="reason">The reason for the warning.</param>
    /// <param name="ruleBroken">The rule broken, if any.</param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="reason" /> is <see langword="null" />, empty, or consists of only whitespace.
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSuccess)> WarnAsync(DiscordUser user, DiscordMember issuer, string reason, Rule? ruleBroken)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("The reason cannot be empty", nameof(reason));

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        (Infraction infraction, bool success) = await _infractionService.CreateInfractionAsync(InfractionType.Warning, user, issuer, options)
            .ConfigureAwait(false);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        Rule? rule = null;
        if (infraction.RuleId is { } ruleId && _ruleService.GuildHasRule(infraction.GuildId, ruleId))
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Orange);
        embed.WithAuthor(user);
        embed.WithTitle("User warned");
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", issuer.Mention, true);
        embed.AddFieldIf(infractionCount > 0, "Total User Infractions", infractionCount, true);
        embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");

        await _logService.LogAsync(issuer.Guild, embed).ConfigureAwait(false);
        return (infraction, success);
    }
}
