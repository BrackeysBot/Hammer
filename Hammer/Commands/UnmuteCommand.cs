using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using Microsoft.Extensions.Logging;
using X10D.DSharpPlus;
using X10D.Text;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements the <c>unmute</c> command.
/// </summary>
internal sealed class UnmuteCommand : ApplicationCommandModule
{
    private readonly ILogger<UnmuteCommand> _logger;
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnmuteCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="muteService">The mute service.</param>
    public UnmuteCommand(ILogger<UnmuteCommand> logger, MuteService muteService)
    {
        _logger = logger;
        _muteService = muteService;
    }

    [SlashCommand("unmute", "Unmutes a user.", false)]
    [SlashRequireGuild]
    public async Task UnmuteAsync(InteractionContext context,
        [Option("user", "The user to unmute.")] DiscordUser user,
        [Option("reason", "The reason for the mute revocation.")] string? reason = null)
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
            _logger.LogInformation("{StaffMember} unmuted {User}. Reason: {Reason}", context.Member, user, reason);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Could not revoke mute");

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
