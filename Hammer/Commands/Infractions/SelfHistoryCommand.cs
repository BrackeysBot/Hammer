using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>selfhistory</c> command.
/// </summary>
internal sealed class SelfHistoryCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelfHistoryCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public SelfHistoryCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [SlashCommand("selfhistory", "View your own infraction history.")]
    [SlashRequireGuild]
    public async Task SelfHistoryAsync(InteractionContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        DiscordMessage message;
        var builder = new DiscordWebhookBuilder();

        try
        {
            message = await context.Member.SendMessageAsync("Please wait...").ConfigureAwait(false);

            builder.WithContent("Unable to send infraction history. Please make sure you have DMs enabled for this server.");
            await context.EditResponseAsync(builder).ConfigureAwait(false);
        }
        catch (UnauthorizedException)
        {
            return;
        }

        await _infractionService.DisplayInfractionHistoryAsync(message, context.User, context.User, context.Guild, false)
            .ConfigureAwait(false);

        builder.WithContent("Your infraction history has been sent to your DMs.");
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
