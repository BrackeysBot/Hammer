using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Resources;
using Humanizer;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("track")]
    [Description("Starts tracking a user and their actions.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task TrackCommandAsync(CommandContext context, [Description("The ID of the user to track.")] ulong userId)
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

        await TrackCommandAsync(context, user);
    }

    [Command("track")]
    [Description("Starts tracking a user and their actions.")]
    [RequirePermissionLevel(PermissionLevel.Moderator)]
    public async Task TrackCommandAsync(CommandContext context, [Description("The user to track.")] DiscordUser user,
        [Description("The duration of the track. Defaults to indefinite tracking.")] TimeSpan? duration = null)
    {
        await context.AcknowledgeAsync();

        if (duration.HasValue)
            await _userTrackingService.TrackUserAsync(user, context.Guild, duration.Value);
        else
            await _userTrackingService.TrackUserAsync(user, context.Guild);

        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);
        embed.WithColor(0xFF9800);
        embed.WithTitle(EmbedTitles.TrackingEnabled);
        embed.WithDescription(EmbedMessages.TrackingEnabled.FormatSmart(new {user}));
        embed.AddFieldOrElse(duration.HasValue, EmbedFieldNames.Duration, () => duration!.Value.Humanize(), () => "Indefinite");

        await context.RespondAsync(embed);
    }
}
