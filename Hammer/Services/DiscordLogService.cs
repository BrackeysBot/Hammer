using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Hammer.Configuration;
using Hammer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which can send embeds to a log channel.
/// </summary>
internal sealed class DiscordLogService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly Dictionary<DiscordGuild, DiscordChannel> _logChannels = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiscordLogService" /> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="configurationService">The configuration service.</param>
    public DiscordLogService(IConfiguration configuration, DiscordClient discordClient, ConfigurationService configurationService)
    {
        _configuration = configuration;
        _discordClient = discordClient;
        _configurationService = configurationService;
    }

    /// <summary>
    ///     Sends an embed to the log channel of the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose log channel in which to post the embed.</param>
    /// <param name="embed">The embed to post.</param>
    /// <param name="notificationOptions">
    ///     Optional. The staff notification options. Defaults to <see cref="StaffNotificationOptions.None" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="embed" /> is <see langword="null" />.
    /// </exception>
    public async Task LogAsync(DiscordGuild guild, DiscordEmbed embed,
        StaffNotificationOptions notificationOptions = StaffNotificationOptions.None)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(embed);

        if (_logChannels.TryGetValue(guild, out DiscordChannel? logChannel))
        {
            if (embed.Timestamp is null)
                embed = new DiscordEmbedBuilder(embed).WithTimestamp(DateTimeOffset.UtcNow);

            await logChannel.SendMessageAsync(BuildMentionString(guild, notificationOptions), embed: embed).ConfigureAwait(false);
        }
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
        ArgumentNullException.ThrowIfNull(guild);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? configuration))
        {
            channel = null;
            return false;
        }

        if (!_logChannels.TryGetValue(guild, out channel))
        {
            channel = guild.GetChannel(configuration.LogChannel);
            _logChannels.Add(guild, channel);
        }

        return channel is not null;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += OnGuildAvailable;
        return Task.CompletedTask;
    }

    private string? BuildMentionString(DiscordGuild guild, StaffNotificationOptions notificationOptions)
    {
        if (!TryGetLogChannel(guild, out DiscordChannel? logChannel)) return null;
        if (notificationOptions == StaffNotificationOptions.None) return null;
        if (!_configurationService.TryGetGuildConfiguration(logChannel.Guild, out GuildConfiguration? configuration)) return null;

        RoleConfiguration roleConfiguration = configuration.Roles;
        DiscordRole? administratorRole = logChannel.Guild.GetRole(roleConfiguration.AdministratorRoleId);
        DiscordRole? moderatorRole = logChannel.Guild.GetRole(roleConfiguration.ModeratorRoleId);

        var mentions = new List<string>();

        if ((notificationOptions & StaffNotificationOptions.Administrator) != 0) mentions.Add(administratorRole.Mention);
        if ((notificationOptions & StaffNotificationOptions.Moderator) != 0) mentions.Add(moderatorRole.Mention);
        if ((notificationOptions & StaffNotificationOptions.Here) != 0) mentions.Add("@here");
        if ((notificationOptions & StaffNotificationOptions.Everyone) != 0) mentions.Add("@everyone");

        return string.Join(' ', mentions);
    }

    private async Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        var logChannel = _configuration.GetSection(e.Guild.Id.ToString())?.GetSection("logChannel")?.Get<ulong>();
        if (!logChannel.HasValue) return;

        try
        {
            DiscordChannel? channel = await _discordClient.GetChannelAsync(logChannel.Value).ConfigureAwait(false);

            if (channel is not null)
                _logChannels[e.Guild] = channel;
        }
        catch
        {
            // ignored
        }
    }
}
