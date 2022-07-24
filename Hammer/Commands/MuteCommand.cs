using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
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
///     Represents a class which implements the <c>mute</c> command.
/// </summary>
internal sealed class MuteCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly MuteService _muteService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteCommand" /> class.
    /// </summary>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="muteService">The mute service.</param>
    /// <param name="ruleService">The rule service.</param>
    public MuteCommand(
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        MuteService muteService,
        RuleService ruleService
    )
    {
        _cooldownService = cooldownService;
        _infractionService = infractionService;
        _muteService = muteService;
        _ruleService = ruleService;
    }

    [SlashCommand("mute", "Temporarily or permanently mutes a user", false)]
    [SlashRequireGuild]
    public async Task MuteAsync(InteractionContext context,
        [Option("user", "The user to mute")] DiscordUser user,
        [Option("reason", "The reason for the mute")] string? reason = null,
        [Option("duration", "The duration of the mute")] string? durationRaw = null,
        [Option("rule", "The rule which was broken.")] long? ruleBroken = null)
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

        TimeSpan? duration = durationRaw?.ToTimeSpan() ?? null;
        var message = new DiscordWebhookBuilder();

        Rule? rule = null;
        if (ruleBroken.HasValue)
        {
            var ruleId = (int) ruleBroken.Value;
            if (_ruleService.GuildHasRule(context.Guild, ruleId))
                rule = _ruleService.GetRuleById(context.Guild, ruleId);
            else
                message.WithContent("The specified rule does not exist - it will be omitted from the infraction.");
        }

        Task<Infraction> infractionTask = duration is null
            ? _muteService.MuteAsync(user, context.Member!, reason, rule)
            : _muteService.TemporaryMuteAsync(user, context.Member!, reason, duration.Value, rule);

        var builder = new DiscordEmbedBuilder();
        try
        {
            infraction = await infractionTask.ConfigureAwait(false);
            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Red);
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");
            reason = reason.WithWhiteSpaceAlternative("None");

            if (duration is null)
            {
                builder.WithTitle("Muted user");
                Logger.Info($"{context.Member} muted {user}. Reason: {reason}");
            }
            else
            {
                builder.WithTitle("Temporarily muted user");
                Logger.Info($"{context.Member} temporarily muted {user} for {duration.Value.Humanize()}. Reason: {reason}");
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue mute to {user}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing mute");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the mute.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
