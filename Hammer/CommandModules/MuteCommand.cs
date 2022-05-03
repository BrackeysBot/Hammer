using System;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Services;
using Humanizer;
using NLog;
using X10D.Time;

namespace Hammer.CommandModules;

/// <summary>
///     Represents a class which implements the <c>mute</c> command.
/// </summary>
internal sealed class MuteCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigurationService _configurationService;
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="muteService">The mute service.</param>
    public MuteCommand(ConfigurationService configurationService, MuteService muteService)
    {
        _configurationService = configurationService;
        _muteService = muteService;
    }

    [SlashCommand("mute", "Temporarily or permanently mutes a user", false)]
    [SlashRequireGuild]
    public async Task MuteSlashCommandAsync(InteractionContext context,
        [Option("user", "The user to mute")] DiscordUser user,
        [Option("reason", "The reason for the mute")] string? reason = null,
        [Option("duration", "The duration of the mute")] string? durationRaw = null)
    {
        await context.DeferAsync(true).ConfigureAwait(false);
        TimeSpan? duration = durationRaw?.ToTimeSpan() ?? null;

        if (context.User.GetPermissionLevel(context.Guild) < PermissionLevel.Administrator)
        {
            GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(context.Guild);
            if (guildConfiguration.MuteConfiguration.MaxModeratorMuteDuration is { } maxModeratorMuteDuration and > 0)
            {
                if (duration is null || duration.Value.TotalMilliseconds > maxModeratorMuteDuration)
                {
                    duration = TimeSpan.FromMilliseconds(maxModeratorMuteDuration);
                }
            }
        }

        Task<Infraction> infractionTask = duration is null
            ? _muteService.MuteAsync(user, context.Member!, reason)
            : _muteService.TemporaryMuteAsync(user, context.Member!, reason, duration.Value);

        var embed = new DiscordEmbedBuilder();
        try
        {
            Infraction infraction = await infractionTask.ConfigureAwait(false);
            embed.WithAuthor(user);
            embed.WithColor(DiscordColor.Red);
            embed.WithDescription(reason);
            embed.WithFooter($"Infraction {infraction.Id} \u2022 User {user.Id}");

            if (duration is null)
            {
                embed.WithTitle("Muted user");
                Logger.Info($"{context.Member} muted {user}. Reason: {reason}");
            }
            else
            {
                embed.WithTitle("Temporarily muted user");
                Logger.Info($"{context.Member} temporarily muted {user} for {duration.Value.Humanize()}. " +
                            $"Reason: {reason.WithWhiteSpaceAlternative("None")}");
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Could not issue mute to {user}");

            embed.WithColor(DiscordColor.Red);
            embed.WithTitle("⚠️ Error issuing mute");
            embed.WithDescription($"{exception.GetType().Name} was thrown while issuing the mute.");
            embed.WithFooter("See log for further details.");
        }

        var builder = new DiscordWebhookBuilder();
        builder.AddEmbed(embed);
        await context.EditResponseAsync(builder).ConfigureAwait(false);
    }
}
