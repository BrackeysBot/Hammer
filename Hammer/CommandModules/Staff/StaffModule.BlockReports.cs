using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("blockreports")]
    [Description("Blocks a user from reporting messages.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task BlockReportsCommandAsync(CommandContext context, [Description("The ID of the user to block.")] ulong userId)
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

        await BlockReportsCommandAsync(context, user);
    }

    [Command("blockreports")]
    [Description("Blocks a user from reporting messages.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task BlockReportsCommandAsync(CommandContext context, [Description("The user to block.")] DiscordUser user)
    {
        await context.AcknowledgeAsync();

        DiscordGuild guild = context.Guild;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (_reportService.IsUserBlocked(user, guild))
        {
            embed.WithColor(0xFF9800);
            embed.WithTitle(EmbedTitles.UserAlreadyBlocked);
            embed.WithDescription(EmbedMessages.UserAlreadyBlocked.FormatSmart(new {user}));
        }
        else
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.UserBlocked);
            embed.WithTitle(EmbedMessages.UserBlocked.FormatSmart(new {user}));
            await _reportService.BlockUserAsync(user, context.Member);
        }

        await context.RespondAsync(embed);
    }
}
