using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>history</c> command.
/// </summary>
internal sealed class HistoryCommand : ApplicationCommandModule
{
    private readonly InfractionService _infractionService;

    private readonly Dictionary<DiscordMessage, DiscordUser> _historyUsers = new();
    private readonly Dictionary<DiscordMessage, int> _historyPage = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryCommand" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="infractionService">The infraction service.</param>
    public HistoryCommand(DiscordClient discordClient, InfractionService infractionService)
    {
        _infractionService = infractionService;
        discordClient.ComponentInteractionCreated += OnComponentInteractionCreated;
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "View Infraction History", false)]
    [SlashRequireGuild]
    public async Task HistoryAsync(ContextMenuContext context)
    {
        DiscordUser user = context.Interaction.Data.Resolved.Users.First().Value;

        await context.DeferAsync().ConfigureAwait(false);
        DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(user, context.Guild, true, 0);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);

        if (_infractionService.GetInfractionCount(user, context.Guild) > 10)
        {
            builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "next-page", "Next Page",
                emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️"))));
        }

        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);
        _historyPage[message] = 0;
        _historyUsers[message] = user;
    }

    [SlashCommand("history", "Views the infraction history for a user.", false)]
    [SlashRequireGuild]
    public async Task HistoryAsync(InteractionContext context,
        [Option("user", "The user whose history to view.")] DiscordUser user)
    {
        await context.DeferAsync().ConfigureAwait(false);
        DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(user, context.Guild, true, 0);

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        if (_infractionService.GetInfractionCount(user, context.Guild) > 10)
        {
            builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Secondary, "next-page", "Next Page",
                emoji: new DiscordComponentEmoji(DiscordEmoji.FromUnicode("➡️"))));
        }

        DiscordMessage message = await context.EditResponseAsync(builder).ConfigureAwait(false);
        _historyPage[message] = 0;
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
