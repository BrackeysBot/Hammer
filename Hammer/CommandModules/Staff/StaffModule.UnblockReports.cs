using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Attributes;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("unblockreports")]
    [Description("Unblocks a user, allowing them to report messages.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task UnblockReportsCommandAsync(CommandContext context,
        [Description("The ID of the user to unblock.")]
        ulong userId)
    {
        DiscordUser user = await context.Client.GetUserAsync(userId);

        if (user is null)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchUser);
            embed.WithDescription(EmbedMessages.NoSuchUser.FormatSmart(new {id = userId}));
            await context.RespondAsync(embed);
            return;
        }

        await UnblockReportsCommandAsync(context, user);
    }

    [Command("unblockreports")]
    [Description("Unblocks a user, allowing them to report messages.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task UnblockReportsCommandAsync(CommandContext context, [Description("The user to unblock.")] DiscordUser user)
    {
        await context.AcknowledgeAsync();

        DiscordGuild guild = context.Guild;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (!_reportService.IsUserBlocked(user, guild))
        {
            embed.WithColor(0xFF9800);
            embed.WithTitle(EmbedTitles.UserNotBlocked);
            embed.WithDescription(EmbedMessages.UserNotBlocked.FormatSmart(new {user}));
        }
        else
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.UserUnblocked);
            embed.WithTitle(EmbedMessages.UserUnblocked.FormatSmart(new {user}));
            await _reportService.UnblockUserAsync(user, guild);
        }

        await context.RespondAsync(embed);
    }
}