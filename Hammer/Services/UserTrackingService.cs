using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DisCatSharp;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which tracks misbehaving users.
/// </summary>
internal sealed class UserTrackingService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<DiscordGuild, List<TrackedUser>> _trackedUsers = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserTrackingService" /> class.
    /// </summary>
    public UserTrackingService(IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Returns a value indicating whether the member is currently being tracked.
    /// </summary>
    /// <param name="member">The member whose status to retrieve.</param>
    /// <returns><see langword="true" /> if this member is currently being tracked; otherwise, <see langword="false" />.</returns>
    public bool IsUserTracked(DiscordMember member)
    {
        return IsUserTracked(member, member.Guild);
    }

    /// <summary>
    ///     Returns a value indicating whether the user is currently being tracked in a specified guild.
    /// </summary>
    /// <param name="user">The user whose status to retrieve.</param>
    /// <param name="guild">The guild whose tracked users to search.</param>
    /// <returns><see langword="true" /> if this user is currently being tracked; otherwise, <see langword="false" />.</returns>
    public bool IsUserTracked(DiscordUser user, DiscordGuild guild)
    {
        if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
            return false;

        return trackedUsers.Exists(u => u.UserId == user.Id);
    }

    /// <summary>
    ///     Begins tracking a user for a specified duration.
    /// </summary>
    /// <param name="user">The user to start tracking.</param>
    /// <param name="guild">The guild in which the user should be tracked.</param>
    /// <param name="duration">The duration of the track.</param>
    public Task TrackUserAsync(DiscordUser user, DiscordGuild guild, TimeSpan duration)
    {
        return TrackUserAsync(user, guild, DateTimeOffset.UtcNow + duration);
    }

    /// <summary>
    ///     Begins tracking a user, optionally specifying a date and time at which the tracking will stop.
    /// </summary>
    /// <param name="user">The user to start tracking.</param>
    /// <param name="guild">The guild in which the user should be tracked.</param>
    /// <param name="expirationTime">
    ///     The expiration date and time of the track, or <see langword="null" /> to track indefinitely.
    /// </param>
    public async Task TrackUserAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset? expirationTime = null)
    {
        if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
        {
            trackedUsers = new List<TrackedUser>();
            _trackedUsers.Add(guild, trackedUsers);
        }

        var trackedUser = new TrackedUser {UserId = user.Id, GuildId = guild.Id, ExpirationTime = expirationTime};

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        EntityEntry<TrackedUser> entry = await context.AddAsync(trackedUser);
        await context.SaveChangesAsync();

        trackedUser = entry.Entity;
        trackedUsers.Add(trackedUser);

        Logger.Info(LoggerMessages.TrackingEnabledForUser.FormatSmart(new {user, guild}));
    }

    /// <summary>
    ///     Stops tracking a user in a specified guild.
    /// </summary>
    /// <param name="user">The user to stop tracking.</param>
    /// <param name="guild">The guild in which the user should no longer be tracked.</param>
    public async Task UntrackUserAsync(DiscordUser user, DiscordGuild guild)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        TrackedUser? trackedUser =
            await context.TrackedUsers.FirstOrDefaultAsync(u => u.UserId == user.Id && u.GuildId == guild.Id);

        if (trackedUser is not null)
        {
            context.Remove(trackedUser);
            await context.SaveChangesAsync();
        }

        if (_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
            trackedUsers.RemoveAll(u => u.UserId == user.Id);

        Logger.Info(LoggerMessages.TrackingDisabledForUser.FormatSmart(new {user, guild}));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateFromDatabaseAsync();

        _timer.Interval = 1000;
        _timer.Enabled = true;
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var untrackCache = new List<(DiscordGuild Guild, DiscordUser User)>();

        foreach ((DiscordGuild guild, List<TrackedUser> trackedUsers) in _trackedUsers)
            for (var index = 0; index < trackedUsers.Count; index++)
            {
                TrackedUser trackedUser = trackedUsers[index];
                DateTimeOffset? expirationTime = trackedUser.ExpirationTime;

                if (!expirationTime.HasValue)
                    continue;

                if (expirationTime <= DateTimeOffset.UtcNow)
                {
                    DiscordUser? user = await _discordClient.GetUserAsync(trackedUser.UserId);
                    untrackCache.Add((guild, user));
                }
            }

        if (untrackCache.Count > 0)
            await Task.WhenAll(untrackCache.Select(u => UntrackUserAsync(u.User, u.Guild)));
    }

    private async Task UpdateFromDatabaseAsync()
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        foreach (IGrouping<ulong, TrackedUser> trackedUsers in
                 context.TrackedUsers.Where(u => u.ExpirationTime == null || u.ExpirationTime > DateTimeOffset.UtcNow)
                     .GroupBy(u => u.GuildId))
        {
            DiscordGuild guild = await _discordClient.GetGuildAsync(trackedUsers.Key);

            if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? list))
            {
                list = new List<TrackedUser>();
                _trackedUsers.Add(guild, list);
            }

            list.AddRange(trackedUsers);
        }
    }
}
