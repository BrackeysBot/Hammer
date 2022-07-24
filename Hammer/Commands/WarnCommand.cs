using DSharpPlus.Entities;
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
///     Represents a class which implements the <c>warn</c> command.
/// </summary>
internal sealed class WarnCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;
    private readonly WarningService _warningService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarnCommand" /> class.
    /// </summary>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    /// <param name="warningService">The warning service.</param>
    public WarnCommand(
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        RuleService ruleService,
        WarningService warningService
    )
    {
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
        try
        {
            Rule? rule = null;
            if (ruleBroken.HasValue)
                rule = _ruleService.GetRuleById(context.Guild, (int) ruleBroken.Value);

            infraction = await _warningService.WarnAsync(user, context.Member, reason, rule).ConfigureAwait(false);

            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Orange);
            builder.WithTitle("Warned user");
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} warned {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue warning to {user}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing warning");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the warning.");
            builder.WithFooter("See log for further details.");
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder)).ConfigureAwait(false);
    }
}
