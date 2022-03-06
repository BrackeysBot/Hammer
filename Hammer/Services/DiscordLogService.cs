using System.Diagnostics.CodeAnalysis;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Resources;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages staff log channels, and allows the posting of embeds and messages in a guild's staff
///     log channel.
/// </summary>
internal sealed class DiscordLogService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiscordLogService" /> class.
    /// </summary>
    public DiscordLogService(DiscordClient discordClient, ConfigurationService configurationService)
    {
        _discordClient = discordClient;
        _configurationService = configurationService;
    }

    /// <summary>
    ///     Gets the log channel for a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose log channel to retrieve.</param>
    /// <param name="channel">
    ///     When this method returns, contains the log channel; or <see langword="null" /> if no such channel is found.
    /// </param>
    /// <returns><see langword="true" /> if the log channel was successfully found; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public bool TryGetLogChannel(DiscordGuild guild, [NotNullWhen(true)] out DiscordChannel? channel)
    {
        if (guild is null)
            throw new ArgumentNullException(nameof(guild));

        ChannelConfiguration channelConfiguration = _configurationService.GetGuildConfiguration(guild).ChannelConfiguration;
        channel = guild.GetChannel(channelConfiguration.LogChannelId);
        return channel is not null;
    }

    /// <summary>
    ///     Logs a message to the staff log channel.
    /// </summary>
    /// <param name="guild">The guild in which to log.</param>
    /// <param name="messageBuilder">The message builder.</param>
    /// <param name="notificationOptions">
    ///     Optional. The staff notification options. Defaults to <see cref="StaffNotificationOptions.None" />.
    /// </param>
    public async Task<DiscordMessage?> LogAsync(
        DiscordGuild guild,
        DiscordMessageBuilder messageBuilder,
        StaffNotificationOptions notificationOptions = StaffNotificationOptions.None
    )
    {
        if (!TryGetLogChannel(guild, out DiscordChannel? logChannel))
            return null;

        string? mention = BuildMentionString(guild, notificationOptions);
        if (!string.IsNullOrWhiteSpace(mention))
            messageBuilder.WithContent(mention);

        return await logChannel.SendMessageAsync(messageBuilder);
    }

    /// <summary>
    ///     Logs an embed to the staff log channel.
    /// </summary>
    /// <param name="guild">The guild in which to log.</param>
    /// <param name="embed">The embed to log.</param>
    /// <param name="notificationOptions">
    ///     Optional. The staff notification options. Defaults to <see cref="StaffNotificationOptions.None" />.
    /// </param>
    public async Task<DiscordMessage?> LogAsync(
        DiscordGuild guild,
        DiscordEmbed embed,
        StaffNotificationOptions notificationOptions = StaffNotificationOptions.None
    )
    {
        if (!TryGetLogChannel(guild, out DiscordChannel? logChannel))
            return null;

        return await logChannel.SendMessageAsync(BuildMentionString(guild, notificationOptions), embed);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(e.Guild);
        ulong logChannelId = guildConfiguration.ChannelConfiguration.LogChannelId;

        if (logChannelId != 0)
        {
            if (e.Guild.GetChannel(logChannelId) is { } channel)
                Logger.Warn(LoggerMessages.LogChannelFound.FormatSmart(new {channel, guild = e.Guild}));
            else
                Logger.Warn(LoggerMessages.LogChannelNotFound.FormatSmart(new {guild = e.Guild}));
        }
        else
        {
            Logger.Warn(LoggerMessages.LogChannelNotDefined.FormatSmart(new {guild = e.Guild}));
        }

        return Task.CompletedTask;
    }

    private string? BuildMentionString(DiscordGuild guild, StaffNotificationOptions notificationOptions)
    {
        if (!TryGetLogChannel(guild, out DiscordChannel? logChannel)) return null;
        if (notificationOptions == StaffNotificationOptions.None) return null;

        RoleConfiguration roleConfiguration = _configurationService.GetGuildConfiguration(logChannel.Guild).RoleConfiguration;
        DiscordRole? administratorRole = logChannel.Guild.GetRole(roleConfiguration.AdministratorRoleId);
        DiscordRole? moderatorRole = logChannel.Guild.GetRole(roleConfiguration.ModeratorRoleId);

        var mentions = new List<string>();

        if ((notificationOptions & StaffNotificationOptions.Administrator) != 0) mentions.Add(administratorRole.Mention);
        if ((notificationOptions & StaffNotificationOptions.Moderator) != 0) mentions.Add(moderatorRole.Mention);
        if ((notificationOptions & StaffNotificationOptions.Here) != 0) mentions.Add("@here");
        if ((notificationOptions & StaffNotificationOptions.Everyone) != 0) mentions.Add("@everyone");

        return string.Join(' ', mentions);
    }
}
