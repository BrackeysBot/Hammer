using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;
using NLog;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a class which implements the <c>warn</c> command.
/// </summary>
internal sealed class WarnCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly RuleService _ruleService;
    private readonly WarningService _warningService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarnCommand" /> class.
    /// </summary>
    /// <param name="ruleService">The rule service.</param>
    /// <param name="warningService">The warning service.</param>
    public WarnCommand(RuleService ruleService, WarningService warningService)
    {
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

        var embed = new DiscordEmbedBuilder();
        try
        {
            Rule? rule = null;
            if (ruleBroken.HasValue)
                rule = _ruleService.GetRuleById(context.Guild, (int) ruleBroken.Value);

            Infraction infraction = await _warningService.WarnAsync(user, context.Member, reason, rule).ConfigureAwait(false);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Warned user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} warned {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue warning to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing warning");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the warning.");
            embed.WithFooter("See log for further details.");
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
