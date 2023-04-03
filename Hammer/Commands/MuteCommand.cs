using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;
using Humanizer;
using Microsoft.Extensions.Logging;
using X10D.DSharpPlus;
using X10D.Text;
using X10D.Time;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the <c>mute</c> command.
/// </summary>
internal sealed class MuteCommand : ApplicationCommandModule
{
    private readonly ILogger<MuteCommand> _logger;
    private readonly ConfigurationService _configurationService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly MuteService _muteService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="muteService">The mute service.</param>
    /// <param name="ruleService">The rule service.</param>
    public MuteCommand(
        ILogger<MuteCommand> logger,
        ConfigurationService configurationService,
        InfractionCooldownService cooldownService,
        InfractionService infractionService,
        MuteService muteService,
        RuleService ruleService
    )
    {
        _logger = logger;
        _configurationService = configurationService;
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
            _logger.LogInformation("{User} is on cooldown. Prompting for confirmation", user);
            DiscordEmbed embed = await _infractionService.CreateInfractionEmbedAsync(infraction).ConfigureAwait(false);
            bool result = await _cooldownService.ShowConfirmationAsync(context, user, infraction, embed).ConfigureAwait(false);
            if (!result) return;
        }

        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            DiscordWebhookBuilder responseBuilder = new DiscordWebhookBuilder().WithContent("This guild is not configured.");
            await context.EditResponseAsync(responseBuilder).ConfigureAwait(false);
            return;
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
                var responseBuilder = new DiscordWebhookBuilder();
                var embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Red);
                embed.WithTitle("⚠️ Error parsing duration");
                embed.WithDescription($"The duration `{durationRaw}` is not a valid duration. " +
                                      "Accepted format is `#y #mo #w #d #h #m #s #ms`");
                await context.EditResponseAsync(responseBuilder.AddEmbed(embed)).ConfigureAwait(false);
                return;
            }
        }

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

        Task<(Infraction, bool)> infractionTask;
        PermissionLevel permissionLevel = context.Member.GetPermissionLevel(guildConfiguration);
        var shouldClampDuration = false;

        if (guildConfiguration.Mute.MaxModeratorMuteDuration is { } maxModeratorMuteDuration and > 0)
            shouldClampDuration = permissionLevel == PermissionLevel.Moderator;
        else
            // pattern match does not initialize to 0 on failure. explicit = 0 is required here, else the compiler complains
            maxModeratorMuteDuration = 0;

        if (duration is null)
        {
            if (shouldClampDuration)
            {
                duration = TimeSpan.FromMilliseconds(maxModeratorMuteDuration);
                infractionTask = _muteService.TemporaryMuteAsync(user, context.Member!, reason, duration.Value, rule);
            }
            else
                infractionTask = _muteService.MuteAsync(user, context.Member!, reason, rule);
        }
        else
        {
            if (shouldClampDuration && duration.Value.TotalMilliseconds > maxModeratorMuteDuration)
                duration = TimeSpan.FromMilliseconds(maxModeratorMuteDuration);

            infractionTask = _muteService.TemporaryMuteAsync(user, context.Member!, reason, duration.Value, rule);
        }

        var builder = new DiscordEmbedBuilder();

        try
        {
            (infraction, bool dmSuccess) = await infractionTask.ConfigureAwait(false);

            if (!dmSuccess)
                importantNotes.Add("The mute was successfully issued, but the user could not be DM'd.");

            builder.WithAuthor(user);
            builder.WithColor(DiscordColor.Red);
            builder.WithDescription(reason);
            builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");
            reason = reason.WithWhiteSpaceAlternative("None");

            if (infraction.Type == InfractionType.Mute)
            {
                builder.WithTitle("Muted user");
                _logger.LogInformation("{StaffMember} muted {User}. Reason: {Reason}", context.Member, user, reason);
            }
            else if (infraction.Type == InfractionType.TemporaryMute)
            {
                builder.WithTitle("Temporarily muted user");
                builder.AddField("Duration", duration!.Value.Humanize());
                _logger.LogInformation("{StaffMember} temporarily muted {User} for {Duration}. Reason: {Reason}",
                    context.Member, user, duration.Value.Humanize(), reason);
            }

            if (importantNotes.Count > 0)
                builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not issue mute to {User}", user);

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing mute");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the mute.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
