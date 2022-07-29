using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using X10D.DSharpPlus;

namespace Hammer.Commands;

/// <summary>
///     Represents a module which implements staff commands.
/// </summary>
internal sealed class MessageCommand : ApplicationCommandModule
{
    private readonly MessageService _messageService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageCommand" /> class.
    /// </summary>
    public MessageCommand(MessageService messageService)
    {
        _messageService = messageService;
    }

    [SlashCommand("message", "Sends a private staff message to a member.", false)]
    [SlashRequireGuild]
    public async Task MessageAsync(
        InteractionContext context,
        [Option("member", "The member to message.")] DiscordUser user,
        [Option("content", "The content of the message.")] string message
    )
    {
        var embed = new DiscordEmbedBuilder();
        DiscordMember? member = await user.GetAsMemberOfAsync(context.Guild).ConfigureAwait(false);

        if (member is null)
        {
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("Not In Guild");
            embed.WithDescription($"User {user.Id} ({user.Mention}) was found, but is not in this guild.");
            await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
        }
        else
        {
            bool success = await _messageService.MessageMemberAsync(member, context.Member, message).ConfigureAwait(false);

            if (success)
            {
                embed.WithColor(DiscordColor.Green);
                embed.WithAuthor(user);
                embed.WithTitle("Message Sent");
                embed.AddField("Content", message);
                await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            }
            else
            {
                embed.WithColor(DiscordColor.Red);
                embed.WithAuthor(user);
                embed.WithTitle("Failed to send message");
                embed.WithDescription($"The message could not be sent to {user.Mention}. " +
                                      "This is likely due to DMs being disabled for this user.");
                embed.AddField("Content", message);
                await context.CreateResponseAsync(embed, ephemeral: true).ConfigureAwait(false);
            }
        }
    }
}
