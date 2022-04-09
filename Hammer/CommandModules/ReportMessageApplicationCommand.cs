using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Resources;
using Hammer.Services;
using SmartFormat;

namespace Hammer.CommandModules;

internal sealed class ReportMessageApplicationCommand : ApplicationCommandModule
{
    private readonly MessageReportService _messageReportService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReportMessageApplicationCommand" /> class.
    /// </summary>
    /// <param name="messageReportService">The message report service.</param>
    public ReportMessageApplicationCommand(MessageReportService messageReportService)
    {
        _messageReportService = messageReportService;
    }

    [ContextMenu(ApplicationCommandType.MessageContextMenu, "🚩 Report Message")]
    public Task ReportMessageCommandAsync(ContextMenuContext context)
    {
        _ = context.DeferAsync(true);

        DiscordUser user = context.User;
        DiscordMessage message = context.Interaction.Data.Resolved.Messages.First().Value;
        _ = _messageReportService.ReportMessageAsync(message, (DiscordMember) user);

        var builder = new DiscordWebhookBuilder();
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedTitles.MessageReported);
        embed.WithDescription(EmbedMessages.MessageReportFeedback.FormatSmart(new {user}));
        embed.WithFooter(EmbedMessages.MessageReportNoDuplicates);
        builder.AddEmbed(embed);

        _ = context.EditResponseAsync(builder);

        return Task.CompletedTask;
    }
}
