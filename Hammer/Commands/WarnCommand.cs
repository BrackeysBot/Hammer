using DSharpPlus.Entities;
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
///     Represents a class which implements the <c>warn</c> command.
/// </summary>
internal sealed class WarnCommand : ApplicationCommandModule
{
    private readonly ILogger<WarnCommand> _logger;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;
    private readonly WarningService _warningService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarnCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    /// <param name="warningService">The warning service.</param>
    public WarnCommand(
        ILogger<WarnCommand> logger,
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        RuleService ruleService,
        WarningService warningService
    )
    {
        _logger = logger;
        _cooldownService = cooldownService;
        _infractionService = infractionService;
        _ruleService = ruleService;
        _warningService = warningService;
    }

    [SlashCommand("warn", "Issues a warning to a user.", false)]
    [SlashRequireGuild]
    public async Task WarnAsync(InteractionContext context,
        [Option("user", "The user to warn.")] DiscordUser user,
        [Option("reason", "The reason for the warning.")] string reason,
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] string? ruleSearch = null)
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
                await _warningService.WarnAsync(user, context.Member, reason, rule).ConfigureAwait(false);

            if (!dmSuccess)
                importantNotes.Add("The warning was successfully issued, but the user could not be DM'd.");

            if (importantNotes.Count > 0)
                builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));

            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Orange);
            builder.WithTitle("Warned user");
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            _logger.LogInformation("{StaffMember} warned {User}. Reason: {Reason}", context.Member, user, reason);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not issue warning to {User}", user);

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing warning");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the warning.");
            builder.WithFooter("See log for further details.");
        }

        await context.EditResponseAsync(message.AddEmbed(builder)).ConfigureAwait(false);
    }
}
