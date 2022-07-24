using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the gag user context menu.
/// </summary>
internal sealed class GagCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GagCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public GagCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Gag", false)]
    [SlashRequireGuild]
    public async Task GagAsync(ContextMenuContext context)
    {
        DiscordMember staffMember = context.Member;
        DiscordMember? member = context.Interaction.Data.Resolved.Members.First().Value;

        if (staffMember is null)
        {
            await context.CreateResponseAsync("Cannot perform this action outside of a guild.", true).ConfigureAwait(false);
            return;
        }

        if (member is null)
        {
            await context.CreateResponseAsync("You must target a user to gag them.", true).ConfigureAwait(false);
            return;
        }

        try
        {
            await _infractionService.GagAsync(member, staffMember).ConfigureAwait(false);
            await context.CreateResponseAsync($"{member.Mention} has been gagged", true).ConfigureAwait(false);
        }
        catch (UnauthorizedException)
        {
            await context.CreateResponseAsync($"I cannot gag {member.Mention}", true).ConfigureAwait(false);
        }
    }

    [SlashCommand("gag", "Temporarily gags a user, so that a more final infraction can be issued.", false)]
    [SlashRequireGuild]
    public async Task GagAsync(
        InteractionContext context,
        [Option("user", "The user to gag.")] DiscordUser user,
        [Option("duration", "The duration of the gag. Defaults to 5 minutes")] TimeSpan? duration = null
    )
    {
        DiscordMember staffMember = context.Member;

        if (staffMember is null)
        {
            await context.CreateResponseAsync("Cannot perform this action outside of a guild.", true).ConfigureAwait(false);
            return;
        }

        try
        {
            await _infractionService.GagAsync(user, staffMember).ConfigureAwait(false);
            await context.CreateResponseAsync($"{user.Mention} has been gagged", true).ConfigureAwait(false);
        }
        catch (UnauthorizedException)
        {
            await context.CreateResponseAsync($"I cannot gag {user.Mention}", true).ConfigureAwait(false);
        }
    }
}
