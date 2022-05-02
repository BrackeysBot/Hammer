using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;
using NLog;

namespace Hammer.CommandModules;

internal sealed class WarnCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly WarningService _warningService;

    public WarnCommand(WarningService warningService)
    {
        _warningService = warningService;
    }

    [SlashCommand("warn", "Issues a warning to a user.", false)]
    public async Task WarnSlashCommandAsync(InteractionContext context,
        [Option("user", "The user to warn.")] DiscordUser user,
        [Option("reason", "The reason for the warning.")] string reason)
    {
        await context.DeferAsync(true);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await _warningService.WarnAsync(user, context.Member, reason);

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

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}
