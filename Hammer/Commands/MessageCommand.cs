using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Interactivity;
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
        [Option("member", "The member to message.")] DiscordUser user
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
            var modal = new DiscordModalBuilder(context.Client);
            modal.WithTitle("Send Message");
            DiscordModalTextInput message = modal.AddInput("Message", isRequired: true, inputStyle: TextInputStyle.Paragraph);

            DiscordModalResponse response =
                await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(5)).ConfigureAwait(false);

            if (response != DiscordModalResponse.Success)
                return;

            string? content = message.Value?.Trim();
            var builder = new DiscordFollowupMessageBuilder();
            builder.AsEphemeral();
            
            if (string.IsNullOrWhiteSpace(content))
            {
                embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Red);
                embed.WithAuthor(user);
                embed.WithTitle("Message not sent");
                embed.WithDescription($"An empty message cannot be sent to {user.Mention}");
                await context.FollowUpAsync(builder.AddEmbed(embed)).ConfigureAwait(false);
                return;
            }

            bool success = await _messageService.MessageMemberAsync(member, context.Member, content).ConfigureAwait(false);

            if (success)
            {
                embed.WithColor(DiscordColor.Green);
                embed.WithAuthor(user);
                embed.WithTitle("Message Sent");
                embed.AddField("Content", content);
                await context.FollowUpAsync(builder.AddEmbed(embed)).ConfigureAwait(false);
            }
            else
            {
                embed.WithColor(DiscordColor.Red);
                embed.WithAuthor(user);
                embed.WithTitle("Failed to send message");
                embed.WithDescription($"The message could not be sent to {user.Mention}. " +
                                      "This is likely due to DMs being disabled for this user.");
                embed.AddField("Content", content);
                await context.FollowUpAsync(builder.AddEmbed(embed)).ConfigureAwait(false);
            }
        }
    }
}
