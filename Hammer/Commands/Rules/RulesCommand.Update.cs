using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace Hammer.Commands.Rules;

internal sealed partial class RulesCommand
{
    private static readonly Regex MessageLinkRegex = GetMessageLinkRegex();

    [SlashCommand("update", "Sends the rule embed", false)]
    [SlashRequireGuild]
    public async Task UpdateAsync(InteractionContext context,
        [Option("messageLink", "The link to the message to edit.")]
        string messageLink)
    {
        Match match = MessageLinkRegex.Match(messageLink);

        if (!match.Success)
        {
            await context.CreateResponseAsync("Invalid message link.", true);
            return;
        }

        ulong guildId = ulong.Parse(match.Groups[1].Value);
        if (guildId != context.Guild.Id)
        {
            await context.CreateResponseAsync("Invalid message link.", true);
            return;
        }

        DiscordChannel channel = context.Guild.GetChannel(ulong.Parse(match.Groups[2].Value));
        if (channel is null)
        {
            await context.CreateResponseAsync("Invalid message link.", true);
            return;
        }

        DiscordMessage message;

        try
        {
            message = await channel.GetMessageAsync(ulong.Parse(match.Groups[3].Value));
        }
        catch (NotFoundException)
        {
            await context.CreateResponseAsync("Invalid message link.", true);
            return;
        }

        if (message.Author != context.Client.CurrentUser)
        {
            await context.CreateResponseAsync("Invalid message link.", true);
            return;
        }

        await context.CreateResponseAsync($"Sending rules to {channel.Mention}", true);
        await _ruleService.ModifyRulesMessageAsync(message);
    }

    [GeneratedRegex(@"https://discord\.com/channels/(\d+)/(\d+)/(\d+)", RegexOptions.Compiled)]
    private static partial Regex GetMessageLinkRegex();
}
