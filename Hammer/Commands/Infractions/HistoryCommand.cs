using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.Logging;
using X10D.Time;

namespace Hammer.Commands.Infractions;

/// <summary>
///     Represents a class which implements the <c>history</c> command.
/// </summary>
internal sealed class HistoryCommand : ApplicationCommandModule
{
    private readonly ILogger<HistoryCommand> _logger;
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HistoryCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="infractionService">The infraction service.</param>
    public HistoryCommand(ILogger<HistoryCommand> logger, InfractionService infractionService)
    {
        _logger = logger;
        _infractionService = infractionService;
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "View Infraction History", false)]
    [SlashRequireGuild]
    public async Task HistoryAsync(ContextMenuContext context)
    {
        DiscordUser user = context.Interaction.Data.Resolved.Users.First().Value;

        await context.DeferAsync(true).ConfigureAwait(false);

        var builder = new DiscordWebhookBuilder();
        var response = new InfractionHistoryResponse(_infractionService, user, context.User, context.Guild, true);

        for (var pageIndex = 0; pageIndex < response.Pages; pageIndex++)
        {
            DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(response, pageIndex);
            builder.AddEmbed(embed);
        }

        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }

    [SlashCommand("history", "Views the infraction history for a user.", false)]
    [SlashRequireGuild]
    public async Task HistoryAsync(InteractionContext context,
        [Option("user", "The user whose history to view.")] DiscordUser user,
        [Option("before", "If set, limits to infractions before the specified date.")] string? beforeRaw = null,
        [Option("after", "If set, limits to infractions after the specified date.")] string? afterRaw = null,
        [Option("type", "If set, limits to infractions of the specified type.")] InfractionType? type = null
    )
    {
        DateTimeOffset? afterDate = null;
        DateTimeOffset? beforeDate = null;
        long? afterId = null;
        long? beforeId = null;

        if (!string.IsNullOrWhiteSpace(afterRaw))
        {
            if (TimeSpanParser.TryParse(afterRaw, out TimeSpan difference))
            {
                afterDate = DateTimeOffset.UtcNow - difference;
                afterId = null;
            }
            else if (DateTimeOffset.TryParse(afterRaw, out DateTimeOffset result))
            {
                afterDate = result;
                afterId = null;
            }
            else if (long.TryParse(afterRaw, out long longValue))
            {
                afterDate = null;
                afterId = longValue;
            }
        }

        if (!string.IsNullOrWhiteSpace(beforeRaw))
        {
            if (TimeSpanParser.TryParse(beforeRaw, out TimeSpan difference))
            {
                beforeDate = DateTimeOffset.UtcNow - difference;
                beforeId = null;
            }
            else if (DateTimeOffset.TryParse(beforeRaw, out DateTimeOffset result))
            {
                beforeDate = result;
                beforeId = null;
            }
            else if (long.TryParse(beforeRaw, out long longValue))
            {
                beforeDate = null;
                beforeId = longValue;
            }
            else
            {
                beforeDate = null;
                beforeId = null;
            }
        }

        var searchOptions = new InfractionSearchOptions
        {
            IdBefore = beforeId,
            IdAfter = afterId,
            IssuedAfter = afterDate,
            IssuedBefore = beforeDate,
            Type = type
        };

        await context.DeferAsync().ConfigureAwait(false);

        var builder = new DiscordWebhookBuilder();
        var response = new InfractionHistoryResponse(_infractionService, user, context.User, context.Guild, true);

        for (var pageIndex = 0; pageIndex < response.Pages; pageIndex++)
        {
            try
            {
                DiscordEmbedBuilder embed = _infractionService.BuildInfractionHistoryEmbed(response, pageIndex, searchOptions);
                builder.AddEmbed(embed);
            }
            catch (ArgumentException exception)
            {
                _logger.LogWarning(exception, "Could not generate infraction history embed");

                var embed = new DiscordEmbedBuilder();
                embed.WithColor(DiscordColor.Red);
                embed.WithTitle("Invalid options specified");
                embed.WithDescription(exception.Message);
                embed.WithFooter("See log for further details");

                builder.AddEmbed(embed);
            }
        }

        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
