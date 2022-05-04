using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Reports;

internal sealed partial class ReportCommands
{
    [SlashCommand("unblockreports", "Unblocks a user, allowing them to report messages.", false)]
    [SlashRequireGuild]
    public async Task UnblockReportsAsync(InteractionContext context, [Option("user", "The user to unblock.")] DiscordUser user)
    {
        await context.DeferAsync().ConfigureAwait(false);

        DiscordGuild guild = context.Guild;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (!_reportService.IsUserBlocked(user, guild))
        {
            embed.WithColor(0x4CAF50);
            embed.WithTitle(EmbedTitles.UserNotBlocked);
            embed.WithDescription(EmbedMessages.UserNotBlocked.FormatSmart(new {user}));
        }
        else
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.UserUnblocked);
            embed.WithDescription(EmbedMessages.UserUnblocked.FormatSmart(new {user}));
            await _reportService.UnblockUserAsync(user, guild).ConfigureAwait(false);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
