using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>history</c> command.
/// </summary>
internal sealed class HistoryCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public HistoryCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [SlashCommand("history", "Views the infraction history for a user.", false)]
    [SlashRequireGuild]
    public async Task HistoryAsync(InteractionContext context,
        [Option("user", "The user whose history to view.")] DiscordUser user)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(user, context.Guild, true);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
