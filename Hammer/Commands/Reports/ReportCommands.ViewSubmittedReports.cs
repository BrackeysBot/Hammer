using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Humanizer;
using X10D.DSharpPlus;

namespace Hammer.Commands.Reports;

internal sealed partial class ReportCommands
{
    [SlashCommand("viewsubmittedreports", "Views all reports submitted by a user.", false)]
    [SlashRequireGuild]
    public async Task ViewSubmittedReportsAsync(
        InteractionContext context,
        [Option("user", "The user whose submitted reports to view.")] DiscordUser user
    )
    {
        await context.DeferAsync().ConfigureAwait(false);

        var list = new List<string>();

        await foreach (ReportedMessage reportedMessage in
                       _reportService.EnumerateSubmittedReportsAsync(user, context.Guild).ConfigureAwait(false))
        {
            var id = reportedMessage.MessageId.ToString();

            try
            {
                DiscordChannel channel = await context.Client.GetChannelAsync(reportedMessage.ChannelId).ConfigureAwait(false);
                DiscordMessage message = await channel.GetMessageAsync(reportedMessage.MessageId).ConfigureAwait(false);
                id = Formatter.MaskedUrl(id, message.JumpLink);
            }
            catch (DiscordException)
            {
            }

            string channelMention = MentionUtility.MentionChannel(reportedMessage.ChannelId);
            string userMention = MentionUtility.MentionUser(reportedMessage.AuthorId);
            list.Add($"**ID {reportedMessage.Id}** \u2022 {id} in {channelMention} against {userMention}");
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(user);

        if (list.Count == 0)
        {
            embed.WithColor(DiscordColor.Green);
            embed.WithTitle("No reports");
            embed.WithDescription("No reports have been submitted by this user.");
        }
        else
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle($"{"report".ToQuantity(list.Count)}");
            embed.WithDescription(string.Join('\n', list));
        }

        await context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed)).ConfigureAwait(false);
    }
}
