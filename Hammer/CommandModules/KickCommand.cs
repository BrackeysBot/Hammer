using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Services;
using NLog;
using X10D.Text;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>kick</c> command.
/// </summary>
internal sealed class KickCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;
    private readonly RuleService _ruleService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KickCommand" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    /// <param name="ruleService">The rule service.</param>
    public KickCommand(BanService banService, RuleService ruleService)
    {
        _banService = banService;
        _ruleService = ruleService;
    }

    [SlashCommand("kick", "Kicks a member", false)]
    [SlashRequireGuild]
    public async Task KickCommandAsync(InteractionContext context,
        [Option("member", "The member to kick.")] DiscordUser user,
        [Option("reason", "The reason for the kick."), RemainingText] string? reason = null,
        [Option("rule", "The rule which was broken."), Autocomplete(typeof(RuleAutocompleteProvider))] long? ruleBroken = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        var builder = new DiscordWebhookBuilder();
        DiscordMember member;

        try
        {
            member = await context.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Not in guild");
            embed.WithDescription($"The user {user.Mention} is not in this guild.");
            builder.AddEmbed(embed);
            await context.EditResponseAsync(builder).ConfigureAwait(false);

            Logger.Info($"{context.Member} attempted to kick non-member {user}");
            return;
        }

        try
        {
            Rule? rule = null;
            if (ruleBroken.HasValue)
                rule = _ruleService.GetRuleById(context.Guild, (int) ruleBroken.Value);

            Infraction infraction = await _banService.KickAsync(member, context.Member!, reason, rule).ConfigureAwait(false);

            embed.WithAuthor(member);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Kicked user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {member.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} kicked {member}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue kick to {member}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing kick");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the kick.");
            embed.WithFooter("See log for further details.");
        }

        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
