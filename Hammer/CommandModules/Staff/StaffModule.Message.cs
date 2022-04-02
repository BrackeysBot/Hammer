using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("message")]
    [Description("Sends a private message to a member.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task MessageCommandAsync(CommandContext context, [Description("The ID of the member to kick.")] ulong memberId,
        [Description("The message content.")] [RemainingText]
        string message)
    {
        DiscordMember member = await context.Guild.GetMemberAsync(memberId);

        if (member is null)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(0xFF0000);

            DiscordUser user = await context.Client.GetUserAsync(memberId);
            if (user is null)
            {
                embed.WithTitle(EmbedTitles.NoSuchUser);
                embed.WithDescription(EmbedMessages.NoSuchUser.FormatSmart(new {id = memberId}));
            }
            else
            {
                embed.WithTitle(EmbedTitles.NotInGuild);
                embed.WithDescription(EmbedMessages.NotInGuild.FormatSmart(new {user}));
            }

            await context.RespondAsync(embed);
            return;
        }

        await MessageCommandAsync(context, member, message);
    }

    [Command("message")]
    [Description("Sends a private message to a member.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task MessageCommandAsync(CommandContext context, [Description("The member to message.")] DiscordMember member,
        [Description("The message content.")] [RemainingText]
        string message)
    {
        await context.AcknowledgeAsync();
        await _messageService.MessageMemberAsync(member, context.Member, message);
    }
}
