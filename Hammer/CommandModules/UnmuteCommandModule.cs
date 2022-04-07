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
///     Represents a module which implements the <c>unmute</c> command.
/// </summary>
internal sealed class UnmuteCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnmuteCommandModule" /> class.
    /// </summary>
    /// <param name="muteService">The mute service.</param>
    public UnmuteCommandModule(MuteService muteService)
    {
        _muteService = muteService;
    }

    [Command("unmute")]
    [Description("Unmutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task UnmuteCommandAsync(CommandContext context, [Description("The ID of the user to unmute.")] ulong userId,
        [Description("The reason for the mute revocation."), RemainingText]
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

            Logger.Info($"{context.Member} attempted to revoke mute on non-existent user {userId}");
            return;
        }

        await UnmuteCommandAsync(context, user, reason);
    }

    [Command("unmute")]
    [Description("Unmutes a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task UnmuteCommandAsync(CommandContext context, [Description("The user to unmute.")] DiscordUser user,
        [Description("The reason for the mute revocation."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();
        try
        {
            await _muteService.RevokeMuteAsync(user, context.Member!, reason);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.SpringGreen);
            embed.WithTitle("Unmuted user");
            embed.WithDescription(reason);

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} unmuted {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Could not revoke mute");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error revoking mute");
            embed.WithDescription($"{exception.GetType().Name} was thrown while revoking the mute.");
            embed.WithFooter("See log for further details.");
        }

        _ = context.RespondAsync(embed);
    }
}
