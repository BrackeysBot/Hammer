using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Services;
using NLog;
using X10D.DSharpPlus;

namespace Hammer.Commands;

/// <summary>
///     Represents a class which implements the gag user context menu.
/// </summary>
internal sealed class GagCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly InfractionService _infractionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GagCommand" /> class.
    /// </summary>
    /// <param name="infractionService">The infraction service.</param>
    public GagCommand(InfractionService infractionService)
    {
        _infractionService = infractionService;
    }

    [ContextMenu(ApplicationCommandType.UserContextMenu, "Gag", false)]
    [SlashRequireGuild]
    public async Task GagAsync(ContextMenuContext context)
    {
        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();

        DiscordMember staffMember = context.Member;
        DiscordUser user = context.Interaction.Data.Resolved.Users.First().Value;

        if (staffMember is null)
        {
            await context.CreateResponseAsync("Cannot perform this action outside of a guild.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync(true).ConfigureAwait(false);

        try
        {
            await _infractionService.GagAsync(user, staffMember).ConfigureAwait(false);

            builder.WithColor(DiscordColor.Orange);
            builder.WithAuthor(user);
            builder.WithTitle("User gagged");
            builder.WithDescription($"{user.Mention} has been gagged.");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue gag to {user}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing gag");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the gag.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }

    [SlashCommand("gag", "Temporarily gags a user, so that a more final infraction can be issued.", false)]
    [SlashRequireGuild]
    public async Task GagAsync(
        InteractionContext context,
        [Option("user", "The user to gag.")] DiscordUser user,
        [Option("duration", "The duration of the gag. Defaults to 5 minutes")] TimeSpan? duration = null
    )
    {
        var builder = new DiscordEmbedBuilder();
        var message = new DiscordWebhookBuilder();
        DiscordMember staffMember = context.Member;

        if (staffMember is null)
        {
            await context.CreateResponseAsync("Cannot perform this action outside of a guild.", true).ConfigureAwait(false);
            return;
        }

        await context.DeferAsync(true).ConfigureAwait(false);

        try
        {
            await _infractionService.GagAsync(user, staffMember, duration: duration).ConfigureAwait(false);

            builder.WithColor(DiscordColor.Orange);
            builder.WithAuthor(user);
            builder.WithTitle("User gagged");
            builder.WithDescription($"{user.Mention} has been gagged.");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue gag to {user}");

            builder.WithColor(DiscordColor.Red);
            builder.WithTitle("⚠️ Error issuing gag");
            builder.WithDescription($"{exception.GetType().Name} was thrown while issuing the gag.");
            builder.WithFooter("See log for further details.");
        }

        message.AddEmbed(builder);
        await context.EditResponseAsync(message).ConfigureAwait(false);
    }
}
