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
///     Represents a module which implements the <c>unban</c> command.
/// </summary>
internal sealed class UnbanCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnbanCommandModule" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    public UnbanCommandModule(BanService banService)
    {
        _banService = banService;
    }

    [Command("unban")]
    [Description("Unbans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task UnbanCommandAsync(CommandContext context, [Description("The ID of the user to unban.")] ulong userId,
        [Description("The reason for the ban revocation."), RemainingText]
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

            Logger.Info($"{context.Member} attempted to revoke ban on non-existent user {userId}");
            return;
        }

        await UnbanCommandAsync(context, user, reason);
    }

    [Command("unban")]
    [Description("Unbans a user.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task UnbanCommandAsync(CommandContext context, [Description("The user to unban.")] DiscordUser user,
        [Description("The reason for the ban revocation."), RemainingText]
        string? reason = null)
    {
        _ = context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();
        try
        {
            await _banService.RevokeBanAsync(user, context.Member!, reason);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.SpringGreen);
            embed.WithTitle("Unbanned user");
            embed.WithDescription(reason);

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} unbanned {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Could not revoke ban");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error revoking ban");
            embed.WithDescription($"{exception.GetType().Name} was thrown while revoking the ban.");
            embed.WithFooter("See log for further details.");
        }

        _ = context.RespondAsync(embed);
    }
}
