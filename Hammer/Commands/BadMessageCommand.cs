using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Interactivity;
using Hammer.Services;
using Microsoft.Extensions.Logging;
using X10D.Text;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the <c>Warn For This</c> context menu.
/// </summary>
internal sealed class BadMessageCommand : ApplicationCommandModule
{
    private readonly ILogger<BadMessageCommand> _logger;
    private readonly ConfigurationService _configurationService;
    private readonly WarningService _warningService;
    private readonly MessageDeletionService _messageDeletionService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BadMessageCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="messageDeletionService">The message deletion service.</param>
    /// <param name="ruleService">The rule service.</param>
    /// <param name="warningService">The warning service.</param>
    public BadMessageCommand(
        ILogger<BadMessageCommand> logger,
        ConfigurationService configurationService,
        MessageDeletionService messageDeletionService,
        RuleService ruleService,
        WarningService warningService
    )
    {
        _logger = logger;
        _configurationService = configurationService;
        _messageDeletionService = messageDeletionService;
        _ruleService = ruleService;
        _warningService = warningService;
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Warn For This", false)]
    [SlashRequireGuild]
    public async Task BadMessageAsync(ContextMenuContext context)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? configuration))
        {
            configuration = new GuildConfiguration();
        }

        string defaultReason = configuration.DefaultBadMessageWarning;
        DiscordMember staffMember = context.Member;
        DiscordMessage message = context.TargetMessage;
        DiscordUser user = message.Author;

        var importantNotes = new List<string>();
        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Warning Details");

        DiscordModalTextInput ruleInput = modal.AddInput("Rule ID", "The ID of the rule which was broken.",
            isRequired: false,
            maxLength: 5);
        DiscordModalTextInput reasonInput = modal.AddInput("Reason", "The reason for the warning.",
            defaultReason,
            false,
            TextInputStyle.Paragraph,
            maxLength: 250);

        DiscordModalResponse modalResponse =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

        if (modalResponse != DiscordModalResponse.Success)
            return;

        DiscordGuild guild = context.Guild;
        if (!TryGetRule(guild, ruleInput.Value, out Rule? rule))
        {
            importantNotes.Add("The specified rule does not exist - it will be omitted from the infraction.");
        }

        string reason = MentionUtility.ReplaceChannelMentions(guild, reasonInput.Value.WithWhiteSpaceAlternative(defaultReason));
        await _messageDeletionService.DeleteMessageAsync(message, staffMember).ConfigureAwait(false);

        var additionalInfo = $"Message {message.Id} in {message.Channel.Mention} (#{message.Channel.Name})";
        (Infraction infraction, bool dmSuccess) = await _warningService.WarnAsync(user, staffMember, reason, rule, additionalInfo)
            .ConfigureAwait(false);

        if (!dmSuccess)
            importantNotes.Add("The warning was successfully issued, but the user could not be DM'd.");

        var builder = new DiscordEmbedBuilder();

        if (importantNotes.Count > 0)
            builder.AddField("⚠️ Important Notes", string.Join("\n", importantNotes.Select(n => $"• {n}")));

        builder.WithAuthor(user);
        builder.WithColor(DiscordColor.Orange);
        builder.WithTitle("Warned user");
        builder.WithDescription(reason);
        builder.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

        _logger.LogInformation("{StaffMember} warned {User}. Reason: {Reason}", context.Member, user, reason);

        var response = new DiscordFollowupMessageBuilder();
        response.AsEphemeral();
        await context.FollowUpAsync(response.AddEmbed(builder)).ConfigureAwait(false);
    }

    private bool TryGetRule(DiscordGuild guild, string? query, out Rule? rule)
    {
        rule = null;
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        if (int.TryParse(query, out int ruleId))
        {
            if (_ruleService.GuildHasRule(guild, ruleId))
            {
                rule = _ruleService.GetRuleById(guild, ruleId);
                return true;
            }
        }
        else
        {
            rule = _ruleService.SearchForRule(guild, query);
            if (rule is not null)
            {
                return true;
            }
        }

        return false;
    }
}
