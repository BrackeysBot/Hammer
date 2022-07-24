using DSharpPlus;
using DSharpPlus.Entities;
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

        await _infractionService.GagAsync(member, staffMember).ConfigureAwait(false);
        await context.CreateResponseAsync($"{member.Username} has been gagged").ConfigureAwait(false);
    }
}
