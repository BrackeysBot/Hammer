using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;
using NLog;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>kick</c> command.
/// </summary>
internal sealed class KickCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KickCommandModule" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    public KickCommandModule(BanService banService)
    {
        _banService = banService;
    }

    [Command("kick")]
    [Description("Kicks a member.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task KickCommandAsync(CommandContext context, [Description("The ID of the user to kick.")] ulong userId,
        [Description("The reason for the kick."), RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        DiscordUser user;
        try
        {
            user = await context.Client.GetUserAsync(userId);
        }
        catch (NotFoundException)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ No such user");
            embed.WithDescription($"No user with the ID {userId} could be found.");
            await context.RespondAsync(embed).ConfigureAwait(false);

            Logger.Info($"{context.Member} attempted to kick non-existent user {userId}");
            return;
        }

        await KickCommandAsync(context, user, reason);
    }

    [Command("kick")]
    [Description("Kicks a member.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task KickCommandAsync(CommandContext context, [Description("The member to kick.")] DiscordMember member,
        [Description("The reason for the kick."), RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _banService.KickAsync(member, context.Member!, reason);

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

        await context.RespondAsync(embed).ConfigureAwait(false);
    }

    [Command("kick")]
    [Description("Kicks a member.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task KickCommandAsync(CommandContext context, [Description("The user to kick.")] DiscordUser user,
        [Description("The reason for the kick."), RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        DiscordMember member;
        try
        {
            member = await context.Guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Not in guild");
            embed.WithDescription($"The user {user.Mention} is not in this guild.");
            await context.RespondAsync(embed).ConfigureAwait(false);

            Logger.Info($"{context.Member} attempted to kick non-member {user}");
            return;
        }

        await KickCommandAsync(context, member, reason);
    }
}
