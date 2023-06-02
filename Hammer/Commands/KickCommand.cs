using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.Logging;
using X10D.DSharpPlus;
using X10D.Text;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>kick</c> command.
/// </summary>
internal sealed class KickCommand : ApplicationCommandModule
{
    private readonly ILogger<KickCommand> _logger;
    private readonly BanService _banService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KickCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="banService">The ban service.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    public KickCommand(
        ILogger<KickCommand> logger,
        BanService banService,
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        RuleService ruleService
    )
    {
        _logger = logger;
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
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] string? ruleSearch = null,
        [Option("clearMessageHistory", "Clear the user's recent messages in text channels.")] bool clearMessageHistory = false)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        if (_cooldownService.IsCooldownActive(user, context.Member) &&
            _cooldownService.TryGetInfraction(user, out Infraction? infraction))
        {
            _logger.LogInformation("{User} is on cooldown. Prompting for confirmation", user);
            DiscordEmbed embed = await _infractionService.CreateInfractionEmbedAsync(infraction).ConfigureAwait(false);
            bool result = await _cooldownService.ShowConfirmationAsync(context, user, infraction, embed).ConfigureAwait(false);
            if (!result) return;
        }

        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();
        var importantNotes = new List<string>();
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

            _logger.LogInformation("{StaffMember} attempted to kick non-member {User}", context.Member, user);
            return;
        }

        try
        {
            Rule? rule = null;
            if (!string.IsNullOrWhiteSpace(ruleSearch))
            {
                if (int.TryParse(ruleSearch, out int ruleId))
                {
                    if (_ruleService.GuildHasRule(context.Guild, ruleId))
                    {
                        rule = _ruleService.GetRuleById(context.Guild, ruleId)!;
                    }
                    else
                    {
                        importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
                    }
                }
                else
                {
                    rule = _ruleService.SearchForRule(context.Guild, ruleSearch);
                    if (rule is null)
                    {
                        importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
                    }
                }
            }

            (infraction, bool dmSuccess) =
                await _banService.KickAsync(member, context.Member!, reason, rule, clearMessageHistory).ConfigureAwait(false);

            if (!dmSuccess)
                importantNotes.Add("The kick was successfully issued, but the user could not be DM'd.");

            if (importantNotes.Count > 0)
                builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));

            builder.WithAuthor(member);
            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("Kicked user");
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {member.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            _logger.LogInformation("{StaffMember} kicked {User}. Reason: {Reason}", context.Member, member, reason);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not issue kick to {Member}", member);

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing kick");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the kick.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
