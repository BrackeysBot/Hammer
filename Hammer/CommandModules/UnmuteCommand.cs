using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using NLog;
using X10D.Text;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>unmute</c> command.
/// </summary>
internal sealed class UnmuteCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnmuteCommand" /> class.
    /// </summary>
    /// <param name="muteService">The mute service.</param>
    public UnmuteCommand(MuteService muteService)
    {
        _muteService = muteService;
    }

    [SlashCommand("unmute", "Unmutes a user.", false)]
    [SlashRequireGuild]
    public async Task UnmuteAsync(InteractionContext context,
        [Option("user", "The user to unmute.")] DiscordUser user,
        [Option("reason", "The reason for the mute revocation."), RemainingText] string? reason = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        try
        {
            await _muteService.RevokeMuteAsync(user, context.Member!, reason).ConfigureAwait(false);

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

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
