using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using Humanizer;
using Microsoft.Extensions.Logging;
using X10D.DSharpPlus;
using X10D.Text;
using X10D.Time;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>ban</c> command.
/// </summary>
internal sealed class BanCommand : ApplicationCommandModule
{
    private readonly ILogger<BanCommand> _logger;
    private readonly BanService _banService;
    private readonly InfractionCooldownService _cooldownService;
    private readonly InfractionService _infractionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="banService">The ban service.</param>
    /// <param name="cooldownService">The cooldown service.</param>
    /// <param name="infractionService">The infraction service.</param>
    /// <param name="ruleService">The rule service.</param>
    public BanCommand(
        ILogger<BanCommand> logger,
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

    [SlashCommand("ban", "Temporarily or permanently bans a user.", false)]
    [SlashRequireGuild]
    public async Task BanAsync(InteractionContext context,
        [Option("user", "The user to ban.")] DiscordUser user,
        [Option("reason", "The reason for the ban.")] string? reason = null,
        [Option("duration", "The duration of the ban.")] string? durationRaw = null,
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

        DiscordGuild guild = context.Guild;
        if (await _banService.IsUserBannedAsync(user, guild).ConfigureAwait(false))
        {
            var responseBuilder = new DiscordWebhookBuilder();
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ User already banned");
            embed.WithDescription($"{user.Mention} ({user.Id:0}) is already banned. " +
                                  "If you are trying to replace a temporary ban with a permanent one, " +
                                  "please unban the member first before running `/ban` again.");
            await context.EditResponseAsync(responseBuilder.AddEmbed(embed)).ConfigureAwait(false);
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

        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();
        var importantNotes = new List<string>();

        Rule? rule = null;
        if (!string.IsNullOrWhiteSpace(ruleSearch))
        {
            if (int.TryParse(ruleSearch, out int ruleId))
            {
                if (_ruleService.GuildHasRule(guild, ruleId))
                {
                    rule = _ruleService.GetRuleById(guild, ruleId)!;
                }
                else
                {
                    importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
                }
            }
            else
            {
                rule = _ruleService.SearchForRule(guild, ruleSearch);
                if (rule is null)
                {
                    importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
                }
            }
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
                _logger.LogInformation("{StaffMember} canned {User}. Reason: {Reason}", context.Member, user, reason);
            }
            else
            {
                builder.WithTitle("Temporarily banned user");
                builder.AddField("Duration", duration.Value.Humanize());
                _logger.LogInformation("{StaffMember} temporarily canned {User} for {Duration}. Reason: {Reason}",
                    context.Member, user, duration.Value.Humanize(), reason);
            }

            if (importantNotes.Count > 0)
                builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not issue ban to {User}", user);

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing ban");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the ban.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
