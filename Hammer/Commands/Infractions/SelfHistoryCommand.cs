using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>selfhistory</c> command.
/// </summary>
internal sealed class SelfHistoryCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    private readonly Dictionary<DiscordMessage, DiscordUser> _historyUsers = new();
    private readonly Dictionary<DiscordMessage, int> _historyPage = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelfHistoryCommand" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="infractionService">The infraction service.</param>
    public SelfHistoryCommand(DiscordClient discordClient, InfractionService infractionService)
    {
        _infractionService = infractionService;
        
        discordClient.ComponentInteractionCreated += OnComponentInteractionCreated;
    }

    [SlashCommand("selfhistory", "View your own infraction history.")]
    [SlashRequireGuild]
    public async Task SelfHistoryAsync(InteractionContext context)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(context.User, context.Guild, false, 0);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);

        if (_infractionService.GetInfractionCount(context.User, context.Guild) > 10)
        {
            builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "next-page", "Next Page",
                emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️"))));
        }

        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);
        _historyPage[message] = 0;
        _historyUsers[message] = context.User;
    }

    private async Task OnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (!_historyPage.TryGetValue(e.Message, out int pageIndex) ||
            !_historyUsers.TryGetValue(e.Message, out DiscordUser? user))
            return;

        int infractionCount = _infractionService.GetInfractionCount(user, e.Guild);
        var maxPages = (int) MathF.Ceiling(infractionCount / 10.0f);

        if (e.Id == "next-page")
        {
            DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(user, e.Guild, true, ++pageIndex);

            var builder = new DiscordMessageBuilder();
            builder.AddEmbed(embed);

            if (pageIndex > 0)
            {
                builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "previous-page", "Previous Page",
                    emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("⬅️"))));
            }

            if (pageIndex < maxPages)
            {
                builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "next-page", "Next Page",
                    emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️"))));
            }

            await e.Message.ModifyAsync(builder).ConfigureAwait(false);
        }
        else if (e.Id == "previous-page")
        {
            DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(user, e.Guild, true, --pageIndex);

            var builder = new DiscordMessageBuilder();
            builder.AddEmbed(embed);

            if (pageIndex > 0)
            {
                builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "previous-page", "Previous Page",
                    emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("⬅️"))));
            }

            if (pageIndex < maxPages)
            {
                builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "next-page", "Next Page",
                    emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️"))));
            }

            await e.Message.ModifyAsync(builder).ConfigureAwait(false);
        }
    }
}
