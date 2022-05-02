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
///     Represents a module which implements the <c>warn</c> command.
/// </summary>
internal sealed class WarnCommandModule : BaseCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly WarningService _warningService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WarnCommandModule" /> class.
    /// </summary>
    /// <param name="warningService">The warning service.</param>
    public WarnCommandModule(WarningService warningService)
    {
        _warningService = warningService;
    }

    [Command("warn")]
    [Description("Issues a warning to a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task WarnCommandAsync(CommandContext context, [Description("The ID of the user to warn.")] ulong userId,
        [Description("The reason for the warning."), RemainingText]
        string reason)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        DiscordUser user;
        try
        {
            user = await context.Client.GetUserAsync(userId).ConfigureAwait(false);
        }
        catch (NotFoundException)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ No such user");
            embed.WithDescription($"No user with the ID {userId} could be found.");
            await context.RespondAsync(embed).ConfigureAwait(false);

            Logger.Info($"{context.Member} attempted to warn non-existent user {userId}");
            return;
        }

        await WarnCommandAsync(context, user, reason).ConfigureAwait(false);
    }

    [Command("warn")]
    [Description("Issues a warning to a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task WarnCommandAsync(CommandContext context, [Description("The user to warn.")] DiscordUser user,
        [Description("The reason for the warning."), RemainingText]
        string reason)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _warningService.WarnAsync(user, context.Member!, reason).ConfigureAwait(false);

            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("Warned user");
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            reason = reason.WithWhiteSpaceAlternative("None");
            Logger.Info($"{context.Member} warned {user}. Reason: {reason}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue warning to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing warning");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the warning.");
            embed.WithFooter("See log for further details.");
        }

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
