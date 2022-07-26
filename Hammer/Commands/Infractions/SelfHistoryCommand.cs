using DSharpPlus.Entities;
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

        var builder = new DiscordWebhookBuilder();
        builder.WithContent("Please wait...");
        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);

        await _infractionService.DisplayInfractionHistoryAsync(message, context.User, context.User, context.Guild, false)
            .ConfigureAwait(false);
    }
}
