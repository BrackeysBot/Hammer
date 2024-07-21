using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;

namespace Hammer.Commands.Reports;

internal sealed partial class ReportCommands
{
    [SlashCommand("viewreports", "Views all reports made against this user.", false)]
    [SlashRequireGuild]
    public async Task ViewReportsAsync(
        InteractionContext context,
        [Option("user", "The user whose reported messages to view.")]
        DiscordUser user
    )
    {
        await context.DeferAsync();

        var list = new List<string>();

        foreach (ReportedMessage reportedMessage in _reportService.EnumerateReports(user, context.Guild))
        {
            var id = reportedMessage.MessageId.ToString();

            try
            {
                DiscordChannel channel = await context.Client.GetChannelAsync(reportedMessage.ChannelId);
                DiscordMessage message = await channel.GetMessageAsync(reportedMessage.MessageId);
                id = Formatter.MaskedUrl(id, message.JumpLink);
            }
            catch (DiscordException)
            {
            }

            string channelMention = MentionUtility.MentionChannel(reportedMessage.ChannelId);
            string userMention = MentionUtility.MentionUser(reportedMessage.ReporterId);
            list.Add($"**ID {reportedMessage.Id}** \u2022 {id} in {channelMention} by {userMention}");
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (list.Count == 0)
        {
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle("No reports");
            embed.WithDescription("No reports have been made against this user.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle($"{"report".ToQuantity(list.Count)}");
            embed.WithDescription(string.Join('\n', list));
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }
}
