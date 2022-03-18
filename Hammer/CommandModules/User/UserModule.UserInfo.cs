using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.User;

internal sealed partial class UserModule
{
    [Command("userinfo")]
    [Description("Displays information about a specified user.")]
    [RequireGuild]
    public async Task UserInfoAsync(CommandContext context, ulong userId)
    {
        await UserInfoAsync(context, await context.Client.GetUserAsync(userId));
    }

    [Command("userinfo")]
    [Description("Displays information about a specified user.")]
    [RequireGuild]
    public async Task UserInfoAsync(CommandContext context, DiscordUser? user = null)
    {
        await context.AcknowledgeAsync();

        if (user is null || context.Member.GetPermissionLevel(context.Guild) < PermissionLevel.Guru)
            // community members under Guru cannot view info about other members, only themselves
            user = context.Member;

        bool isMember = context.Guild.Members.TryGetValue(user.Id, out DiscordMember? member);
        int infractionCount = _infractionService.GetInfractionCount(user, context.Guild);

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);
        embed.WithColor(isMember ? member!.Color : 0x2196F3);
        embed.WithThumbnail(user.GetAvatarUrl(ImageFormat.Png));
        embed.WithTitle($"Information about {user.UsernameWithDiscriminator}");
        embed.AddField("Username", user.UsernameWithDiscriminator, true);
        embed.AddField("ID", user.Id, true);
        embed.AddField("User Created", Formatter.Timestamp(user.CreationTimestamp, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(isMember, "Join Date", () => Formatter.Timestamp(member!.JoinedAt, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(infractionCount > 0, "Infractions", infractionCount, true);
        embed.AddFieldIf(isMember, "Permission Level", () => member!.GetPermissionLevel(context.Guild).ToString("G"), true);

        if (!isMember) embed.WithFooter("⚠️ This user is not currently in this server!");

        await context.RespondAsync(embed);
    }
}
