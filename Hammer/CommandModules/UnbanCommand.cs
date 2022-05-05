using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using NLog;
using X10D.Text;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a module which implements the <c>unban</c> command.
/// </summary>
internal sealed class UnbanCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnbanCommand" /> class.
    /// </summary>
    /// <param name="banService">The ban service.</param>
    public UnbanCommand(BanService banService)
    {
        _banService = banService;
    }

    [SlashCommand("unban", "Unbans a user.", false)]
    [SlashRequireGuild]
    public async Task UnbanAsync(InteractionContext context,
        [Option("user", "The user to unban.")] DiscordUser user,
        [Option("reason", "The reason for the ban revocation.")] string? reason = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

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

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
