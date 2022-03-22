using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BrackeysBot.Core.API;
using DisCatSharp;
using DisCatSharp.Entities;
using Hammer.API;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.EventData;
using Hammer.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles temporarily muting users.
/// </summary>
/// <remarks>
///     This service DOES NOT assign roles, but rather keeps track of temporary mutes that are scheduled for deletion.
///     To assign the guild's Muted role, use <see cref="InfractionService.MuteAsync" />.
/// </remarks>
internal sealed class TemporaryMuteService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigurationService _configurationService;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly Dictionary<DiscordGuild, DiscordRole> _mutedRoles = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<DiscordGuild, List<TemporaryMute>> _temporaryMutes = new();
    private readonly Timer _temporaryMuteTimer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryMuteService" /> class.
    /// </summary>
    public TemporaryMuteService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient,
        ConfigurationService configurationService)
    {
        _scopeFactory = scopeFactory;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _configurationService = configurationService;

        _temporaryMuteTimer.Interval = 1000;
        _temporaryMuteTimer.Elapsed += TemporaryMuteTimerOnElapsed;
    }

    /// <summary>
    ///     Occurs when a user has been muted.
    /// </summary>
    public event EventHandler<MutedEventArgs> UserMuted = null!;

    /// <summary>
    ///     Occurs when a user has been unmuted.
    /// </summary>
    public event EventHandler<MutedEventArgs> UserUnmuted = null!;

    /// <summary>
    ///     Clears a temporary mute from the tracking list.
    /// </summary>
    /// <param name="user">The user whose temporary mute to revoke.</param>
    /// <param name="guild">The guild in which to revoke.</param>
    public void ClearTemporaryMute(DiscordUser user, DiscordGuild guild)
    {
        if (!TryGetTemporaryMute(user, guild, out TemporaryMute? temporaryMute)) return;

        GetTemporaryMutes(guild).Remove(temporaryMute);
    }

    /// <summary>
    ///     Starts tracking a new temporary mute on a user. If a temporary mute is already being tracked, it is replaced with the
    ///     new expiration time.
    /// </summary>
    /// <param name="user">The user to gag.</param>
    /// <param name="guild">The guild in which the gag should be issued.</param>
    /// <param name="duration">The duration of the mute.</param>
    /// <returns>The newly-created <see cref="TemporaryMute" />.</returns>
    public TemporaryMute CreateTemporaryMute(DiscordUser user, DiscordGuild guild, TimeSpan duration)
    {
        List<TemporaryMute> temporaryMutes = GetTemporaryMutes(guild);

        if (TryGetTemporaryMute(user, guild, out TemporaryMute? temporaryMute))
        {
            // Remove/Add because of reassign using 'with' would break all equality checks. I mean ...
            temporaryMutes.Remove(temporaryMute);

            // ... face it. with expression are pog!
            temporaryMute = temporaryMute with {ExpirationTime = DateTimeOffset.UtcNow + duration};
            temporaryMutes.Add(temporaryMute);
        }
        else
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            temporaryMute = new TemporaryMute(user, now, now + duration);
        }

        UserMuted?.Invoke(this, new MutedEventArgs(user, guild));
        temporaryMutes.Add(temporaryMute);
        return temporaryMute;
    }

    /// <summary>
    ///     Starts tracking a "gag" (a preset-duration temporary mute) so that staff can deal with a situation.
    /// </summary>
    /// <param name="user">The member to gag.</param>
    /// <param name="guild">The guild in which the gag should be issued.</param>
    public void Gag(DiscordUser user, DiscordGuild guild)
    {
        GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
        long gagDuration = guildConfiguration.MuteConfiguration.GagDuration;

        if (TryGetTemporaryMute(user, guild, out _)) return; // don't override a hard mute
        CreateTemporaryMute(user, guild, TimeSpan.FromMilliseconds(gagDuration));
    }

    /// <summary>
    ///     Gets the <c>Muted</c> role for a guild.
    /// </summary>
    /// <param name="guild">The guild whose <c>Muted</c> role to retrieve.</param>
    /// <returns>The <see cref="DiscordRole" />.</returns>
    public DiscordRole? GetMutedRole(DiscordGuild guild)
    {
        if (!_mutedRoles.TryGetValue(guild, out DiscordRole? role))
        {
            GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
            role = guild.GetRole(guildConfiguration.RoleConfiguration.MutedRoleId);
            _mutedRoles.Add(guild, role);
        }

        return role;
    }

    /// <summary>
    ///     Determines if a user is currently muted.
    /// </summary>
    /// <param name="user">The user whose mute status to check.</param>
    /// <param name="guild">The guild whose muted users to filter by.</param>
    /// <param name="result">When this method returns, contains the <see cref="TemporaryMute" /> object.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="user" /> is currently muted in <paramref name="guild" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool TryGetTemporaryMute(DiscordUser user, DiscordGuild guild, [NotNullWhen(true)] out TemporaryMute? result)
    {
        if (!_temporaryMutes.TryGetValue(guild, out List<TemporaryMute>? mutes))
        {
            result = null;
            return false;
        }

        result = mutes.Find(m => m.User == user);
        return result is not null;
    }

    /// <summary>
    ///     Stops tracking a temporary mute status on a user.
    /// </summary>
    /// <param name="user">The user whose mute status to revoke.</param>
    /// <param name="guild">The guild whose mutes to search.</param>
    public void Unmute(DiscordUser user, DiscordGuild guild)
    {
        if (!_temporaryMutes.TryGetValue(guild, out List<TemporaryMute>? mutes) || mutes.Count <= 0)
            return;

        TemporaryMute? mute = mutes.Find(m => m.User == user);
        if (mute is not null)
            mutes.Remove(mute);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdatedTemporaryMutesAsync();
        _temporaryMuteTimer.Start();
    }

    private List<TemporaryMute> GetTemporaryMutes(DiscordGuild guild)
    {
        if (!_temporaryMutes.TryGetValue(guild, out List<TemporaryMute>? mutes))
        {
            mutes = new List<TemporaryMute>();
            _temporaryMutes.Add(guild, mutes);
        }

        return mutes;
    }

    private async void TemporaryMuteTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach ((DiscordGuild guild, List<TemporaryMute> mutes) in _temporaryMutes)
        {
            GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
            DiscordRole? mutedRole = GetMutedRole(guild);

            for (var index = 0; index < mutes.Count; index++)
            {
                TemporaryMute mute = mutes[index];
                if (now < mute.ExpirationTime) continue;

                Logger.Info(LoggerMessages.TemporaryMuteExpired.FormatSmart(new {user = mute.User, guild}));
                await _corePlugin.LogAsync(guild, new DiscordEmbedBuilder().WithColor(guildConfiguration.SecondaryColor));

                if (mutedRole is null)
                    Logger.Warn(LoggerMessages.NoMutedRoleToRevoke.FormatSmart(new {guild}));
                else if (await guild.GetMemberAsync(mute.User.Id) is { } member)
                    await member.RevokeRoleAsync(mutedRole);

                mutes.RemoveAt(index--);

                UserUnmuted?.Invoke(this, new MutedEventArgs(mute.User, guild));
            }
        }
    }

    private async Task UpdatedTemporaryMutesAsync()
    {
        var guildCache = new Dictionary<ulong, DiscordGuild>();
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        IEnumerable<Infraction> infractions = context.Infractions.OrderByDescending(i => i.IssuedAt)
            .Where(i => i.Type == InfractionType.Gag).AsEnumerable().Where(i =>
            {
                GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(i.GuildId);
                MuteConfiguration muteConfiguration = guildConfiguration.MuteConfiguration;
                return (DateTimeOffset.UtcNow - i.IssuedAt).TotalMilliseconds < muteConfiguration.GagDuration;
            }).ToArray();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (Infraction infraction in infractions)
        {
            ulong guildId = infraction.GuildId;

            if (!guildCache.TryGetValue(guildId, out DiscordGuild? guild))
            {
                guild = await _discordClient.GetGuildAsync(guildId);
                guildCache.Add(guildId, guild);
            }

            DiscordUser user = await _discordClient.GetUserAsync(infraction.UserId);
            if (infraction.ExpirationTime.HasValue && !TryGetTemporaryMute(user, guild, out _))
                CreateTemporaryMute(user, guild, infraction.ExpirationTime.Value - now);
        }
    }
}
