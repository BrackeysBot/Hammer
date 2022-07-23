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

    [SlashCommand("selfhistory", "View your own infraction history.", false)]
    [SlashRequireGuild]
    public async Task SelfHistoryAsync(InteractionContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(context.User, context.Guild, false);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
