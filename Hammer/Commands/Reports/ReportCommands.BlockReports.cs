using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using X10D.DSharpPlus;

namespace Hammer.Commands.Reports;

internal sealed partial class ReportCommands
{
    [SlashCommand("blockreports", "Blocks a user from reporting messages.", false)]
    [SlashRequireGuild]
    public async Task BlockReportsAsync(InteractionContext context, [Option("user", "The user to block.")] DiscordUser user)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        DiscordGuild guild = context.Guild;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (_reportService.IsUserBlocked(user, guild))
        {
            embed.WithColor(0xFF9800);
            embed.WithTitle("User Already Blocked");
            embed.WithDescription($"{user.Mention} is already blocked from reporting messages.");
        }
        else
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("User Blocked");
            embed.WithDescription($"{user.Mention} will no longer be able to make message reports.");
            await _reportService.BlockUserAsync(user, context.Member).ConfigureAwait(false);
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
