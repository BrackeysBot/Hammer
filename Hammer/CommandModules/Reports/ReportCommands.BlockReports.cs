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
    [SlashCommand("blockreports", "Blocks a user from reporting messages.", false)]
    [SlashRequireGuild]
    public async Task BlockReportsAsync(InteractionContext context, [Option("user", "The user to block.")] DiscordUser user)
    {
        await context.DeferAsync().ConfigureAwait(false);
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
            embed.WithDescription(EmbedMessages.UserBlocked.FormatSmart(new {user}));
            await _reportService.BlockUserAsync(user, context.Member).ConfigureAwait(false);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
