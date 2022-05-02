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
using Humanizer;
using NLog;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>ban</c> command.
/// </summary>
internal sealed class BanCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanCommandModule" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    public BanCommandModule(BanService banService)
    {
        _banService = banService;
    }

    [Command("ban")]
    [Description("Temporarily bans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task TempBanCommandAsync(CommandContext context, [Description("The ID of the user to ban.")] ulong userId,
        [Description("The duration of the ban.")]
        TimeSpan duration,
        [Description("The reason for the ban."), RemainingText]
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

            Logger.Info($"{context.Member} attempted to ban non-existent user {userId}");
            return;
        }

        await TempBanCommandAsync(context, user, duration, reason);
    }

    [Command("ban")]
    [Description("Temporarily bans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task TempBanCommandAsync(CommandContext context, [Description("The user to ban.")] DiscordUser user,
        [Description("The duration of the ban.")]
        TimeSpan duration,
        [Description("The reason for the ban."), RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _banService.TemporaryBanAsync(user, context.Member!, reason, duration);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Temporarily banned user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} temporarily banned {user} for {duration.Humanize()}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue ban to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing ban");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the ban.");
            embed.WithFooter("See log for further details.");
        }

        await context.RespondAsync(embed).ConfigureAwait(false);
    }

    [Command("ban")]
    [Description("Bans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task BanCommandAsync(CommandContext context, [Description("The ID of the user to ban.")] ulong userId,
        [Description("The reason for the ban."), RemainingText]
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

            Logger.Info($"{context.Member} attempted to ban non-existent user {userId}");
            return;
        }

        await BanCommandAsync(context, user, reason);
    }

    [Command("ban")]
    [Description("Bans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task BanCommandAsync(CommandContext context, [Description("The user to ban.")] DiscordUser user,
        [Description("The reason for the ban."), RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _banService.BanAsync(user, context.Member!, reason);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Banned user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} banned {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue mute to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing ban");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the ban.");
            embed.WithFooter("See log for further details.");
        }

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
