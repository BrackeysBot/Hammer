using System;
using System.Threading.Tasks;
using BrackeysBot.API.Configuration;
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
///     Represents a module which implements the <c>mute</c> command.
/// </summary>
internal sealed class MuteCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IConfiguration _configuration;
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteCommandModule" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="muteService">The mute service.</param>
    public MuteCommandModule(IConfiguration configuration, MuteService muteService)
    {
        _configuration = configuration;
        _muteService = muteService;
    }

    [Command("mute")]
    [Description("Temporarily mutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task TempMuteCommandAsync(CommandContext context, [Description("The ID of the user to mute.")] ulong userId,
        [Description("The duration of the mute.")]
        TimeSpan duration,
        [Description("The reason for the mute."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

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
            _ = context.RespondAsync(embed);

            Logger.Info($"{context.Member} attempted to mute non-existent user {userId}");
            return;
        }

        await TempMuteCommandAsync(context, user, duration, reason);
    }

    [Command("mute")]
    [Description("Temporarily mutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task TempMuteCommandAsync(CommandContext context, [Description("The user to mute.")] DiscordUser user,
        [Description("The duration of the mute.")]
        TimeSpan duration,
        [Description("The reason for the mute."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _muteService.TemporaryMuteAsync(user, context.Member!, reason, duration);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Temporarily muted user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} temporarily muted {user} for {duration.Humanize()}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue mute to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing mute");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the mute.");
            embed.WithFooter("See log for further details.");
        }

        _ = context.RespondAsync(embed);
    }

    [Command("mute")]
    [Description("Mutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task MuteCommandAsync(CommandContext context, [Description("The ID of the user to mute.")] ulong userId,
        [Description("The reason for the mute."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

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
            _ = context.RespondAsync(embed);

            Logger.Info($"{context.Member} attempted to mute non-existent user {userId}");
            return;
        }

        await MuteCommandAsync(context, user, reason);
    }

    [Command("mute")]
    [Description("Mutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task MuteCommandAsync(CommandContext context, [Description("The user to mute.")] DiscordUser user,
        [Description("The reason for the mute."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _muteService.MuteAsync(user, context.Member!, reason);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Muted user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            Logger.Info($"{context.Member} muted {user}. Reason: {reason.WithWhiteSpaceAlternative("None")}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue mute to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing mute");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the mute.");
            embed.WithFooter("See log for further details.");
        }

        _ = context.RespondAsync(embed);
    }
}
