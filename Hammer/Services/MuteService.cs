using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.API;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Data.Infractions;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles temporary mutes.
/// </summary>
internal sealed class MuteService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan QueryInterval = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<DiscordGuild, DiscordRole> _mutedRoles = new();
    private readonly List<Mute> _mutes = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConfigurationService _configurationService;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly InfractionService _infractionService;
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteService" /> class.
    /// </summary>
    public MuteService(IServiceScopeFactory scopeFactory, ConfigurationService configurationService, ICorePlugin corePlugin,
        DiscordClient discordClient, InfractionService infractionService)
    {
        _scopeFactory = scopeFactory;
        _configurationService = configurationService;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _infractionService = infractionService;

        _timer.Interval = QueryInterval.TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
    }

    /// <summary>
    ///     Returns a value indicating whether a user is muted in a specified guild.
    /// </summary>
    /// <param name="user">The user whose mute status to retrieve.</param>
    /// <param name="guild">The guild whose mute list to search.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="user" /> is muted in <paramref name="guild" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public bool IsUserMuted(DiscordUser user, DiscordGuild guild)
    {
        lock (_mutes)
            return _mutes.Exists(x => x.User == user && x.Guild == guild);
    }

    /// <summary>
    ///     Mutes a user.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="issuer">The staff member who issued the mute.</param>
    /// <param name="reason">The reason for the mute.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<Infraction> MuteAsync(DiscordUser user, DiscordMember issuer, string? reason)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        user = await user.NormalizeClientAsync(_discordClient).ConfigureAwait(false);
        issuer = await issuer.NormalizeClientAsync(_discordClient).ConfigureAwait(false);

        lock (_mutes)
        {
            Mute? mute = _mutes.Find(x => x.User == user && x.Guild == issuer.Guild);
            if (mute is not null)
                _mutes.Remove(mute);
        }

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace()
        };

        Infraction infraction = await _infractionService.CreateInfractionAsync(InfractionType.Mute, user, issuer, options)
            .ConfigureAwait(false);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.MutedUser.FormatSmart(new {staffMember = issuer, reason});

        if (TryGetMutedRole(issuer.Guild, out DiscordRole? mutedRole))
        {
            try
            {
                DiscordMember member = await issuer.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
                await member.GrantRoleAsync(mutedRole, reason).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not NotFoundException)
            {
                Logger.Error(exception, $"Could not grant muted role to {user}");
            }
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User muted");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, issuer.Mention, true);
        embed.AddFieldIf(infractionCount > 0, EmbedFieldNames.TotalUserInfractions, infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), EmbedFieldNames.Reason, options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _corePlugin.LogAsync(issuer.Guild, embed).ConfigureAwait(false);

        return infraction;
    }

    /// <summary>
    ///     Revokes a mute for a user.
    /// </summary>
    /// <param name="user">The user whose mute to revoke.</param>
    /// <param name="revoker">The member who revoked the mute.</param>
    /// <param name="reason">The reason for the revocation.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="revoker" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task RevokeMuteAsync(DiscordUser user, DiscordMember revoker, string? reason)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (revoker is null) throw new ArgumentNullException(nameof(revoker));

        user = await user.NormalizeClientAsync(_discordClient).ConfigureAwait(false);
        revoker = await revoker.NormalizeClientAsync(_discordClient).ConfigureAwait(false);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        Mute? mute = await context.Mutes.FirstOrDefaultAsync(b => b.User == user && b.Guild == revoker.Guild)
            .ConfigureAwait(false);

        if (mute is not null)
        {
            lock (_mutes)
                _mutes.Remove(mute);

            context.Remove(mute);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.SpringGreen);
        embed.WithAuthor(user);
        embed.WithTitle("User unmuted");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, revoker.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), EmbedFieldNames.Reason, reason);
        await _corePlugin.LogAsync(revoker.Guild, embed).ConfigureAwait(false);

        reason = reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.UnmutedUser.FormatSmart(new {staffMember = revoker, reason});

        if (TryGetMutedRole(revoker.Guild, out DiscordRole? mutedRole))
        {
            try
            {
                DiscordMember member = await revoker.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
                await member.RevokeRoleAsync(mutedRole, reason).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not NotFoundException)
            {
                Logger.Error(exception, $"Could not revoke muted role from {user}");
            }
        }
    }

    /// <summary>
    ///     Temporarily mutes a user.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="issuer">The staff member who issued the mute.</param>
    /// <param name="reason">The reason for the mute.</param>
    /// <param name="duration">The duration of the mute.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<Infraction> TemporaryMuteAsync(DiscordUser user, DiscordMember issuer, string? reason, TimeSpan duration)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        user = await user.NormalizeClientAsync(_discordClient).ConfigureAwait(false);
        issuer = await issuer.NormalizeClientAsync(_discordClient).ConfigureAwait(false);
        DiscordGuild guild = issuer.Guild;

        if (issuer.GetPermissionLevel(guild) == PermissionLevel.Moderator)
        {
            GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(guild);
            long maxModeratorMuteDuration = guildConfiguration.MuteConfiguration.MaxModeratorMuteDuration;

            if (maxModeratorMuteDuration > 0 && duration.TotalMilliseconds > maxModeratorMuteDuration)
                duration = TimeSpan.FromMilliseconds(maxModeratorMuteDuration);
        }

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace(),
            ExpirationTime = DateTimeOffset.UtcNow + duration
        };

        await CreateTemporaryMuteAsync(user, guild, options.ExpirationTime.Value).ConfigureAwait(false);

        Infraction infraction =
            await _infractionService.CreateInfractionAsync(InfractionType.TemporaryMute, user, issuer, options);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.TempMutedUser.FormatSmart(new {staffMember = issuer, reason, duration = duration.Humanize()});

        if (TryGetMutedRole(issuer.Guild, out DiscordRole? mutedRole))
        {
            try
            {
                DiscordMember member = await issuer.Guild.GetMemberAsync(user.Id).ConfigureAwait(false);
                await member.GrantRoleAsync(mutedRole, reason).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not NotFoundException)
            {
                Logger.Error(exception, $"Could not grant muted role to {user}");
            }
        }

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User temporarily muted");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, issuer.Mention, true);
        embed.AddField(EmbedFieldNames.ExpirationTime,
            Formatter.Timestamp(options.ExpirationTime.Value, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(infractionCount > 0, EmbedFieldNames.TotalUserInfractions, infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), EmbedFieldNames.Reason, options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _corePlugin.LogAsync(guild, embed).ConfigureAwait(false);

        return infraction;
    }

    /// <summary>
    ///     Gets the <c>Muted</c> role for a specified guild, returning a value indicating the success of the operation.
    /// </summary>
    /// <param name="guild">The guild whose <c>Muted</c> role to retrieve.</param>
    /// <param name="result">When this method returns, contains the muted role for <paramref name="guild" />.</param>
    /// <returns><see langword="true" /> if the role was successfully retrieved; otherwise, <see langword="false" />.</returns>
    public bool TryGetMutedRole(DiscordGuild guild, [NotNullWhen(true)] out DiscordRole? result)
    {
        if (!_mutedRoles.TryGetValue(guild, out result))
        {
            var configuration = _corePlugin.Configuration.Get<GuildConfiguration>($"guilds.{guild.Id}");
            configuration ??= new GuildConfiguration();

            result = guild.GetRole(configuration.RoleConfiguration.MutedRoleId);
            _mutedRoles.TryAdd(guild, result);
        }

        return result is not null;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildMemberAdded += DiscordClientOnGuildMemberAdded;

        _timer.Start();
        return Task.CompletedTask;
    }

    private Task DiscordClientOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        if (IsUserMuted(e.Member, e.Guild) && TryGetMutedRole(e.Guild, out DiscordRole? mutedRole))
            return e.Member.GrantRoleAsync(mutedRole, "Reapplying muted role for rejoined user");

        return Task.CompletedTask;
    }

    private async Task CreateTemporaryMuteAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset expirationTime)
    {
        var temporaryMute = new Mute(user, guild, expirationTime);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        EntityEntry<Mute> entry = await context.Mutes.AddAsync(temporaryMute).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        temporaryMute = entry.Entity;

        lock (_mutes)
            _mutes.Add(temporaryMute);
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        foreach (Mute mute in _mutes.ToArray().Where(b => b.ExpiresAt.HasValue && b.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            DiscordMember botMember = await mute.Guild.GetMemberAsync(_discordClient.CurrentUser.Id).ConfigureAwait(false);
            await RevokeMuteAsync(mute.User, botMember, "Temporary mute expired").ConfigureAwait(false);
        }
    }
}
