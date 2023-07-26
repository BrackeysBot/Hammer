using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    [SlashCommand("display", "Displays 1 or more embeds with the guild rules.", false)]
    [SlashRequireGuild]
    public async Task DisplayAsync(InteractionContext context,
        [Option("channel", "The channel in which to display the rules. Defaults to the current channel.")]
        DiscordChannel? channel = null)
    {
        channel ??= context.Channel;
        await context.CreateResponseAsync($"Sending rules to {channel.Mention}", true).ConfigureAwait(false);
        await _ruleService.SendRulesMessageAsync(channel);
    }
}
