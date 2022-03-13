using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Attributes;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("untrack")]
    [Description("Stops tracking a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task UntrackCommandAsync(CommandContext context,
        [Description("The ID of the user to stop tracking.")] ulong userId)
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

        await UntrackCommandAsync(context, user);
    }

    [Command("untrack")]
    [Description("Stops tracking a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task UntrackCommandAsync(CommandContext context, [Description("The user to stop tracking.")] DiscordUser user)
    {
        await context.AcknowledgeAsync();
        await _userTrackingService.UntrackUserAsync(user, context.Guild);

        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);
        embed.WithColor(0xFF9800);
        embed.WithTitle(EmbedTitles.TrackingDisabled);
        embed.WithDescription(EmbedMessages.TrackingDisabled.FormatSmart(new {user}));

        await context.RespondAsync(embed);
    }
}
