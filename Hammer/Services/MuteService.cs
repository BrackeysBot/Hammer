using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using X10D.DSharpPlus;
using X10D.Text;
using ILogger = NLog.ILogger;
using PermissionLevel = Hammer.Data.PermissionLevel;
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
    private readonly DiscordLogService _logService;
    private readonly DiscordClient _discordClient;
    private readonly InfractionService _infractionService;
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MuteService" /> class.
    /// </summary>
    public MuteService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService,
        InfractionService infractionService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
        _infractionService = infractionService;

        _timer.Interval = QueryInterval.TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
    }

    /// <summary>
    ///     Adds a mute to the database.
    /// </summary>
    /// <param name="mute">The mute to add.</param>
    /// <returns>The <see cref="Mute" /> entity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mute" /> is <see langword="null" />.</exception>
    public async Task<Mute> AddMuteAsync(Mute mute)
    {
        ArgumentNullException.ThrowIfNull(mute);
        Mute? existingMute;

        lock (_mutes)
        {
            existingMute = _mutes.Find(m => m.UserId == mute.UserId && m.GuildId == mute.GuildId);
            if (existingMute is not null)
                return existingMute;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        existingMute = await context.Mutes.FindAsync(mute.UserId, mute.GuildId).ConfigureAwait(false);
        if (existingMute is not null)
            return existingMute;

        lock (_mutes)
            _mutes.Add(mute);

        mute = (await context.Mutes.AddAsync(mute).ConfigureAwait(false)).Entity;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return mute;
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
            return _mutes.Exists(x => x.UserId == user.Id && x.GuildId == guild.Id);
    }

    /// <summary>
    ///     Mutes a user.
    /// </summary>
    /// <param name="user">The user to mute.</param>
    /// <param name="issuer">The staff member who issued the mute.</param>
    /// <param name="reason">The reason for the mute.</param>
    /// <param name="ruleBroken">The rule which was broken, if any.</param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSuccess)> MuteAsync(DiscordUser user, DiscordMember issuer, string? reason,
        Rule? ruleBroken)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        DiscordGuild guild = issuer.Guild;
        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        if (issuer.GetPermissionLevel(guildConfiguration) == PermissionLevel.Moderator)
        {
            long? maxModeratorMuteDuration = guildConfiguration.Mute.MaxModeratorMuteDuration;
            if (maxModeratorMuteDuration.HasValue)
                throw new InvalidOperationException(ExceptionMessages.ModeratorCannotPermanentlyMute);
        }

        lock (_mutes)
        {
            Mute? mute = _mutes.Find(x => x.UserId == user.Id && x.GuildId == issuer.Guild.Id);
            if (mute is not null)
                _mutes.Remove(mute);
        }

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        await CreateMuteAsync(user, guild, null).ConfigureAwait(false);

        (Infraction infraction, bool success) = await _infractionService
            .CreateInfractionAsync(InfractionType.Mute, user, issuer, options)
            .ConfigureAwait(false);

        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Muted by {issuer.GetUsernameWithDiscriminator()}: {reason}";

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
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", issuer.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.AddFieldIf(infractionCount > 0, "Total User Infractions", infractionCount, true);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _logService.LogAsync(issuer.Guild, embed).ConfigureAwait(false);

        return (infraction, success);
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

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        Mute? mute = await context.Mutes.FirstOrDefaultAsync(b => b.UserId == user.Id && b.GuildId == revoker.Guild.Id)
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
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", revoker.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), "Reason", reason);
        await _logService.LogAsync(revoker.Guild, embed).ConfigureAwait(false);

        reason = reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Unmuted by {revoker.GetUsernameWithDiscriminator()}: {reason}";

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
    /// <param name="ruleBroken">The rule which was broken, if any.</param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSuccess)> TemporaryMuteAsync(
        DiscordUser user,
        DiscordMember issuer,
        string? reason,
        TimeSpan duration,
        Rule? ruleBroken
    )
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        DiscordGuild guild = issuer.Guild;

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        if (issuer.GetPermissionLevel(guildConfiguration) == PermissionLevel.Moderator)
        {
            long? maxModeratorMuteDuration = guildConfiguration.Mute.MaxModeratorMuteDuration;

            if (maxModeratorMuteDuration > 0 && duration.TotalMilliseconds > maxModeratorMuteDuration)
                duration = TimeSpan.FromMilliseconds(maxModeratorMuteDuration.Value);
        }

        var options = new InfractionOptions
        {
            NotifyUser = true,
            ExpirationTime = DateTimeOffset.UtcNow + duration,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        await CreateMuteAsync(user, guild, options.ExpirationTime.Value).ConfigureAwait(false);


        (Infraction infraction, bool success) = await _infractionService
            .CreateInfractionAsync(InfractionType.TemporaryMute, user, issuer, options)
            .ConfigureAwait(false);
        int infractionCount = _infractionService.GetInfractionCount(user, guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Temp-Muted by {issuer.GetUsernameWithDiscriminator()} ({duration.Humanize()}): {reason}";

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
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", issuer.Mention, true);
        embed.AddField("Expiration Time", Formatter.Timestamp(options.ExpirationTime.Value, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(infractionCount > 0, "Total User Infractions", infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _logService.LogAsync(guild, embed).ConfigureAwait(false);

        return (infraction, success);
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
            var configuration = _configurationService.GetGuildConfiguration(guild);
            configuration ??= new GuildConfiguration();

            result = guild.GetRole(configuration.Roles.MutedRoleId);
            _mutedRoles.TryAdd(guild, result);
        }

        return result is not null;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildMemberAdded += DiscordClientOnGuildMemberAdded;

        _timer.Start();
        return UpdateFromDatabaseAsync();
    }

    private async Task UpdateFromDatabaseAsync()
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        lock (_mutes)
        {
            _mutes.Clear();
            _mutes.AddRange(context.Mutes);
        }
    }

    private Task DiscordClientOnGuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
    {
        if (IsUserMuted(e.Member, e.Guild))
        {
            if (!TryGetMutedRole(e.Guild, out DiscordRole? mutedRole))
            {
                Logger.Warn($"{e.Member} is muted, but no muted role was found in {e.Guild}!");
                return Task.CompletedTask;
            }

            Logger.Info($"{e.Member} is muted. Applying muted role");
            return e.Member.GrantRoleAsync(mutedRole, "Reapplying muted role for rejoined user");
        }

        return Task.CompletedTask;
    }

    private async Task CreateMuteAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset? expirationTime)
    {
        var temporaryMute = Mute.Create(user, guild, expirationTime);

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
        Mute[] mutes;

        lock (_mutes)
            mutes = _mutes.ToArray();

        foreach (Mute mute in mutes.Where(b => b.ExpiresAt.HasValue && b.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            if (!_discordClient.Guilds.TryGetValue(mute.GuildId, out DiscordGuild? guild))
                continue;

            try
            {
                DiscordMember botMember = await guild.GetMemberAsync(_discordClient.CurrentUser.Id).ConfigureAwait(false);
                DiscordUser? user = await _discordClient.GetUserAsync(mute.UserId).ConfigureAwait(false);
                await RevokeMuteAsync(user, botMember, "Temporary mute expired").ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                // ignored
            }
        }
    }
}
