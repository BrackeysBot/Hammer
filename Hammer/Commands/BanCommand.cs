using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using Humanizer;
using NLog;
using X10D.DSharpPlus;
using X10D.Text;
using X10D.Time;
using ILogger = NLog.ILogger;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>ban</c> command.
/// </summary>
internal sealed class BanCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanCommand" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    public BanCommand(
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

    [SlashCommand("ban", "Temporarily or permanently bans a user.", false)]
    [SlashRequireGuild]
    public async Task BanAsync(InteractionContext context,
        [Option("user", "The user to ban.")] DiscordUser user,
        [Option("reason", "The reason for the ban.")] string? reason = null,
        [Option("duration", "The duration of the ban.")] string? durationRaw = null,
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] long? ruleBroken = null,
        [Option("clearMessageHistory", "Clear the user's recent messages in text channels.")] bool clearMessageHistory = false)
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

        TimeSpan? duration = null;
        if (!string.IsNullOrWhiteSpace(durationRaw))
        {
            if (TimeSpanParser.TryParse(durationRaw, out TimeSpan timeSpan))
            {
                duration = timeSpan;
            }
            else
            {
                DiscordWebhookBuilder responseBuilder = new DiscordWebhookBuilder().WithContent("This guild is not configured.");
                var embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Red);
                embed.WithTitle("⚠️ Error parsing duration");
                embed.WithDescription($"The duration `{durationRaw}` is not a valid duration. " +
                                      "Accepted format is `#y #mo #w #d #h #m #s #ms`");
                await context.EditResponseAsync(responseBuilder).ConfigureAwait(false);
                return;
            }
        }

        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();
        var importantNotes = new List<string>();

        Rule? rule = null;
        if (ruleBroken.HasValue)
        {
            var ruleId = (int) ruleBroken.Value;
            if (_ruleService.GuildHasRule(context.Guild, ruleId))
                rule = _ruleService.GetRuleById(context.Guild, ruleId);
            else
                importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
        }

        Task<(Infraction, bool)> infractionTask = duration is null
            ? _banService.BanAsync(user, context.Member!, reason, rule, clearMessageHistory)
            : _banService.TemporaryBanAsync(user, context.Member!, reason, duration.Value, rule, clearMessageHistory);
        try
        {
            (infraction, bool dmSuccess) = await infractionTask.ConfigureAwait(false);

            if (!dmSuccess)
                importantNotes.Add("The ban was successfully issued, but the user could not be DM'd.");

            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("Banned user");
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");
            reason = reason.WithWhiteSpaceAlternative("None");

            if (duration is null)
            {
                builder.WithTitle("Banned user");
                Logger.Info($"{context.Member} banned {user}. Reason: {reason}");
            }
            else
            {
                builder.WithTitle("Temporarily banned user");
                builder.AddField("Duration", duration.Value.Humanize());
                Logger.Info($"{context.Member} temporarily banned {user} for {duration.Value.Humanize()}. Reason: {reason}");
            }

            if (importantNotes.Count > 0)
                builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue ban to {user}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing ban");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the ban.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
