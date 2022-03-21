using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Attributes;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Extensions;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("kick")]
    [Description("Kicks a member from the guild.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task KickCommandAsync(CommandContext context, [Description("The ID of the member to kick.")] ulong memberId,
        [Description("The reason for the kick")] [RemainingText]
        string? reason = null)
    {
        await KickCommandAsync(context, await context.Guild.GetMemberAsync(memberId), reason);
    }

    [Command("kick")]
    [Description("Kicks a member from the guild.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task KickCommandAsync(CommandContext context, [Description("The member to kick.")] DiscordMember member,
        [Description("The reason for the kick")] [RemainingText]
        string? reason = null)
    {
        await context.AcknowledgeAsync();
        Infraction infraction = await _infractionService.KickAsync(member, context.Member, reason);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(0xFF0000);
        embed.WithAuthor(member);
        embed.WithTitle("Kicked");
        embed.WithDescription(infraction.Reason.WithWhiteSpaceAlternative(Formatter.Italic("<no reason specified>")));
        embed.WithFooter($"Infraction #{infraction.Id} \u2022 User #{member.Id}");

        await context.RespondAsync(embed);
    }
}
