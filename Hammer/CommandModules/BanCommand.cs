using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using Humanizer;
using NLog;
using X10D.Text;
using X10D.Time;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>ban</c> command.
/// </summary>
internal sealed class BanCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanCommand" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    /// <param name="ruleService">The rule service.</param>
    public BanCommand(BanService banService, RuleService ruleService)
    {
        _banService = banService;
        _ruleService = ruleService;
    }

    [SlashCommand("ban", "Temporarily or permanently bans a user.", false)]
    [SlashRequireGuild]
    public async Task BanAsync(InteractionContext context,
        [Option("user", "The user to ban.")] DiscordUser user,
        [Option("reason", "The reason for the ban.")] string? reason = null,
        [Option("duration", "The duration of the ban.")] string? durationRaw = null,
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] long? ruleBroken = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        TimeSpan? duration = durationRaw?.ToTimeSpan() ?? null;
        var embed = new DiscordEmbedBuilder();

        Rule? rule = null;
        if (ruleBroken.HasValue)
            rule = _ruleService.GetRuleById(context.Guild, (int) ruleBroken.Value);

        Task<Infraction> infractionTask = duration is null
            ? _banService.BanAsync(user, context.Member!, reason, rule)
            : _banService.TemporaryBanAsync(user, context.Member!, reason, duration.Value, rule);
        try
        {
            Infraction infraction = await infractionTask.ConfigureAwait(false);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Banned user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");
            reason = reason.WithWhiteSpaceAlternative("None");

            if (duration is null)
            {
                embed.WithTitle("Banned user");
                Logger.Info($"{context.Member} banned {user}. Reason: {reason}");
            }
            else
            {
                embed.WithTitle("Temporarily banned user");
                Logger.Info($"{context.Member} temporarily banned {user} for {duration.Value.Humanize()}. Reason: {reason}");
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue ban to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing ban");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the ban.");
            embed.WithFooter("See log for further details.");
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
