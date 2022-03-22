using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Hammer.Data;
using Hammer.Resources;
using Humanizer;
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
    private readonly DiscordClient _discordClient;
    private readonly MessageTrackingService _messageTrackingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Timer _timer = new();
    private readonly Dictionary<DiscordGuild, List<TrackedUser>> _trackedUsers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserTrackingService" /> class.
    /// </summary>
    public UserTrackingService(IServiceScopeFactory scopeFactory, DiscordClient discordClient,
        MessageTrackingService messageTrackingService)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _messageTrackingService = messageTrackingService;
    }

    /// <summary>
    ///     Enumerates the join/leave events of a user in a specified guild.
    /// </summary>
    /// <param name="user">The user whose join/leave events to retrieve.</param>
    /// <param name="guild">The guild whose join/leave events to search.</param>
    /// <returns>An enumerable collection of <see cref="TrackedJoinLeave" /> instances.</returns>
    public async IAsyncEnumerable<TrackedJoinLeave> EnumerateJoinLeavesAsync(DiscordUser user, DiscordGuild guild)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        foreach (TrackedJoinLeave joinLeave in context.JoinLeaves.Where(e => e.UserId == user.Id && e.GuildId == guild.Id))
            yield return joinLeave;
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

        return trackedUsers.Exists(u =>
            u.UserId == user.Id && u.GuildId == guild.Id && u.ExpirationTime.HasValue &&
            u.ExpirationTime.Value > DateTimeOffset.UtcNow);
    }

    /// <summary>
    ///     Returns a value indicating whether the user has been tracked at any point in a specified guild.
    /// </summary>
    /// <param name="user">The user whose status to retrieve.</param>
    /// <param name="guild">The guild whose tracked users to search.</param>
    /// <returns><see langword="true" /> if this user has been tracked in the past; otherwise, <see langword="false" />.</returns>
    public bool UserHasTrackHistory(DiscordUser user, DiscordGuild guild)
    {
        if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
            return false;

        return trackedUsers.Exists(u => u.UserId == user.Id && u.GuildId == guild.Id);
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
    ///     Begins tracking a user, optionally specifying a date and time at which the tracking will stop. If the user is already
    ///     being tracked, no action will occur.
    /// </summary>
    /// <param name="user">The user to start tracking.</param>
    /// <param name="guild">The guild in which the user should be tracked.</param>
    /// <param name="expirationTime">
    ///     The expiration date and time of the track, or <see langword="null" /> to track indefinitely.
    /// </param>
    public async Task TrackUserAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset? expirationTime = null)
    {
        if (IsUserTracked(user, guild)) return;

        if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
        {
            trackedUsers = new List<TrackedUser>();
            _trackedUsers.Add(guild, trackedUsers);
        }

        TrackedUser? trackedUser;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        if (UserHasTrackHistory(user, guild))
        {
            trackedUser = await context.TrackedUsers.FirstOrDefaultAsync(u => u.UserId == user.Id && u.GuildId == guild.Id);
            trackedUser ??= new TrackedUser {UserId = user.Id, GuildId = guild.Id};
            trackedUser.ExpirationTime = expirationTime;
        }
        else
        {
            trackedUser = new TrackedUser {UserId = user.Id, GuildId = guild.Id};
            EntityEntry<TrackedUser> entry = await context.AddAsync(trackedUser);
            trackedUser = entry.Entity;
            trackedUsers.Add(trackedUser);
        }

        await context.SaveChangesAsync();

        Logger.Info(LoggerMessages.TrackingEnabledForUser.FormatSmart(new
        {
            user,
            guild,
            duration = expirationTime.HasValue ? (expirationTime.Value - DateTimeOffset.UtcNow).Humanize() : "indefinite"
        }));
    }

    /// <summary>
    ///     Stops tracking a user in a specified guild. If the user is not currently being tracked, no action will occur.
    /// </summary>
    /// <param name="user">The user to stop tracking.</param>
    /// <param name="guild">The guild in which the user should no longer be tracked.</param>
    public async Task UntrackUserAsync(DiscordUser user, DiscordGuild guild)
    {
        if (!IsUserTracked(user, guild)) return;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        TrackedUser? trackedUser =
            await context.TrackedUsers.FirstOrDefaultAsync(u => u.UserId == user.Id && u.GuildId == guild.Id);

        if (trackedUser is not null)
        {
            trackedUser.ExpirationTime = DateTimeOffset.UtcNow;
            context.Update(trackedUser);
            await context.SaveChangesAsync();

            _trackedUsers[guild].Remove(trackedUser);
        }

        Logger.Info(LoggerMessages.TrackingDisabledForUser.FormatSmart(new {user, guild}));
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer.Interval = 1000;
        _timer.Enabled = true;
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();

        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        _discordClient.GuildMemberAdded += DiscordClientOnGuildMemberAdded;
        _discordClient.GuildMemberRemoved += DiscordClientOnGuildMemberRemoved;
        _discordClient.MessageDeleted += DiscordClientOnMessageDeleted;

        return Task.CompletedTask;
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return UpdateFromDatabaseAsync(e.Guild);
    }

    private Task DiscordClientOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        if (!IsUserTracked(e.Member, e.Guild)) return Task.CompletedTask;
        return TrackUserJoin(e);
    }

    private Task DiscordClientOnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        if (!IsUserTracked(e.Member, e.Guild)) return Task.CompletedTask;
        return TrackUserLeave(e);
    }

    private Task DiscordClientOnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (!IsUserTracked(e.Message.Author, e.Guild)) return Task.CompletedTask;
        return TrackMessageDeletion(e);
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var untrackCache = new List<(DiscordGuild Guild, DiscordUser User)>();

        foreach ((DiscordGuild guild, List<TrackedUser> trackedUsers) in _trackedUsers)
        {
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
        }

        if (untrackCache.Count > 0)
            await Task.WhenAll(untrackCache.Select(u => UntrackUserAsync(u.User, u.Guild)));
    }

    private async Task TrackMessageDeletion(MessageDeleteEventArgs args)
    {
        Logger.Info(LoggerMessages.TrackedMessageDeleted.FormatSmart(new {user = args.Message.Author, message = args.Message}));
        await _messageTrackingService.GetTrackedMessageAsync(args.Message, true);
    }

    private async Task TrackUserJoin(GuildMemberAddEventArgs args)
    {
        Logger.Info(LoggerMessages.TrackedUserJoinedGuild.FormatSmart(new {user = args.Member, guild = args.Guild}));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        var trackedJoin = new TrackedJoinLeave
        {
            GuildId = args.Guild.Id,
            UserId = args.Member.Id,
            OccuredAt = DateTimeOffset.UtcNow,
            Type = TrackedJoinLeave.JoinLeaveType.Join
        };

        await context.AddAsync(trackedJoin);
        await context.SaveChangesAsync();
    }

    private async Task TrackUserLeave(GuildMemberRemoveEventArgs args)
    {
        Logger.Info(LoggerMessages.TrackedUserLeftGuild.FormatSmart(new {user = args.Member, guild = args.Guild}));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        var trackedJoin = new TrackedJoinLeave
        {
            GuildId = args.Guild.Id,
            UserId = args.Member.Id,
            OccuredAt = DateTimeOffset.UtcNow,
            Type = TrackedJoinLeave.JoinLeaveType.Leave
        };

        await context.AddAsync(trackedJoin);
        await context.SaveChangesAsync();
    }

    private async Task UpdateFromDatabaseAsync(DiscordGuild guild)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        if (!_trackedUsers.TryGetValue(guild, out List<TrackedUser>? trackedUsers))
        {
            trackedUsers = new List<TrackedUser>();
            _trackedUsers.Add(guild, trackedUsers);
        }

        trackedUsers.Clear();
        trackedUsers.AddRange(context.TrackedUsers.Where(u => u.GuildId == guild.Id));

        Logger.Info(LoggerMessages.TrackedUsersRetrieved.FormatSmart(new {count = trackedUsers.Count, guild}));
    }
}
