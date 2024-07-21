using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.Commands.Reports;

internal sealed partial class ReportCommands
{
    [ContextMenu(ApplicationCommandType.MessageContextMenu, "Report Message")]
    public async Task ReportMessageAsync(ContextMenuContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);

        DiscordUser user = context.User;
        DiscordMessage message = context.Interaction.Data.Resolved.Messages.First().Value;
        await _reportService.ReportMessageAsync(message, (DiscordMember)user).ConfigureAwait(false);

        var builder = new DiscordWebhookBuilder();
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle("Message Reported");
        embed.WithDescription(EmbedMessages.MessageReportFeedback.FormatSmart(new { user }));
        embed.WithFooter("Reporting this message again will have no impact.");
        builder.AddEmbed(embed);

        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
