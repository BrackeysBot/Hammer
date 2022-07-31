using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using X10D.DSharpPlus;

namespace Hammer.Commands.Reports;

internal sealed partial class ReportCommands
{
    [SlashCommand("unblockreports", "Unblocks a user, allowing them to report messages.", false)]
    [SlashRequireGuild]
    public async Task UnblockReportsAsync(InteractionContext context, [Option("user", "The user to unblock.")] DiscordUser user)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        DiscordGuild guild = context.Guild;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (_reportService.IsUserBlocked(user, guild))
        {
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle("User Unblocked");
            embed.WithDescription($"{user.Mention} has been unblocked. Their message reports will now be acknowledged.");
            await _reportService.UnblockUserAsync(user, context.Member).ConfigureAwait(false);
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle("User Not Blocked");
            embed.WithDescription($"{user.Mention} was not previously blocked from reporting messages.");
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
