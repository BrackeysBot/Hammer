using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using NLog;
using X10D.DSharpPlus;
using X10D.Text;
using ILogger = NLog.ILogger;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>kick</c> command.
/// </summary>
internal sealed class KickCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KickCommand" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    public KickCommand(
        BanService banService,
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        RuleService ruleService
    )
    {
        _banService = banService;
        _cooldownService = cooldownService;
        _infractionService = infractionService;
        _ruleService = ruleService;
    }

    [SlashCommand("kick", "Kicks a member", false)]
    [SlashRequireGuild]
    public async Task KickAsync(InteractionContext context,
        [Option("member", "The member to kick.")] DiscordUser user,
        [Option("reason", "The reason for the kick.")] string? reason = null,
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] long? ruleBroken = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        if (_cooldownService.IsCooldownActive(user, context.Member) &&
            _cooldownService.TryGetInfraction(user, out Infraction? infraction))
        {
            Logger.Info($"{user} is on cooldown. Prompting for confirmation");
            DiscordEmbed embed = await _infractionService.CreateInfractionEmbedAsync(infraction).ConfigureAwait(false);
            bool result = await _cooldownService.ShowConfirmationAsync(context, user, infraction, embed).ConfigureAwait(false);
            if (!result) return;
        }

        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();
        DiscordMember member;

        try
        {
            member = await context.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Not in guild");
            builder.WithDescription($"The user {user.Mention} is not in this guild.");
            message.AddEmbed(builder);
            await context.EditResponseAsync(message).ConfigureAwait(false);

            Logger.Info($"{context.Member} attempted to kick non-member {user}");
            return;
        }

        try
        {
            Rule? rule = null;
            if (ruleBroken.HasValue)
            {
                var ruleId = (int) ruleBroken.Value;
                if (_ruleService.GuildHasRule(context.Guild, ruleId))
                    rule = _ruleService.GetRuleById(context.Guild, ruleId);
                else
                    message.WithContent("The specified rule does not exist - it will be omitted from the infraction.");
            }

            (infraction, bool dmSuccess) =
                await _banService.KickAsync(member, context.Member!, reason, rule).ConfigureAwait(false);

            if (!dmSuccess)
                builder.AddField("⚠️ Important", "The kick was successfully issued, but the user could not be DM'd.");

            builder.WithAuthor(member);
            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("Kicked user");
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {member.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} kicked {member}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue kick to {member}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing kick");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the kick.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
