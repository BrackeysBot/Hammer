using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Attributes;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using SmartFormat;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("warn")]
    [Description("Issues a warning to a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task WarnCommandAsync(CommandContext context, [Description("The ID of the user to warn.")] ulong userId,
        [Description("The reason for the warning")] [RemainingText]
        string? reason = null)
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

        await WarnCommandAsync(context, user, reason);
    }

    [Command("warn")]
    [Description("Issues a warning to a user.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task WarnCommandAsync(CommandContext context, [Description("The user to warn.")] DiscordUser user,
        [Description("The reason for the warning")] [RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync();

        Infraction infraction = await _infractionService.WarnAsync(user, context.Member, reason);
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0xFF0000);
        embed.WithTitle(infraction.Type.Humanize());
        embed.WithDescription(infraction.Reason.WithWhiteSpaceAlternative(Formatter.Italic("<no reason specified>")));
        embed.WithAuthor(user);
        embed.WithFooter($"Infraction #{infraction.Id} \u2022 User #{user.Id}");

        await context.RespondAsync(embed);
    }
}
