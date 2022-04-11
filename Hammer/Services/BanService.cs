using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.API;
using Hammer.Data;
using Hammer.Data.Infractions;
using Hammer.Extensions;
using Hammer.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartFormat;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service that handles guild bans and kicks.
/// </summary>
internal sealed class BanService : BackgroundService
{
    private static readonly TimeSpan QueryInterval = TimeSpan.FromSeconds(30);
    private readonly List<TemporaryBan> _temporaryBans = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly InfractionService _infractionService;
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanService" /> class.
    /// </summary>
    public BanService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient,
        InfractionService infractionService)
    {
        _scopeFactory = scopeFactory;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _infractionService = infractionService;

        _timer.Interval = QueryInterval.TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
    }

    /// <summary>
    ///     Bans a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="issuer">The staff member who issued the ban.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<Infraction> BanAsync(DiscordUser user, DiscordMember issuer, string? reason)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        user = await user.NormalizeClientAsync(_discordClient);
        issuer = await issuer.NormalizeClientAsync(_discordClient);

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace()
        };

        Infraction infraction = await _infractionService.CreateInfractionAsync(InfractionType.Ban, user, issuer, options);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.BannedUser.FormatSmart(new {staffMember = issuer, reason});
        await issuer.Guild.BanMemberAsync(user.Id, reason: reason);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User banned");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, issuer.Mention, true);
        embed.AddFieldIf(infractionCount > 0, EmbedFieldNames.TotalUserInfractions, infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), EmbedFieldNames.Reason, options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        _ = _corePlugin.LogAsync(issuer.Guild, embed);

        return infraction;
    }

    /// <summary>
    ///     Returns a value indicating whether a user is banned from a specified guild.
    /// </summary>
    /// <param name="user">The user whose ban status to retrieve.</param>
    /// <param name="guild">The guild whose ban list to search.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="user" /> is banned in <paramref name="guild" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public async Task<bool> IsUserBannedAsync(DiscordUser user, DiscordGuild guild)
    {
        lock (_temporaryBans)
        {
            if (_temporaryBans.Exists(x => x.User == user && x.Guild == guild))
                return true;
        }

        IReadOnlyList<DiscordBan>? bans = await guild.GetBansAsync();
        if (bans.Any(b => b.User == user))
            return true;

        return false;
    }

    /// <summary>
    ///     Kicks members from the guild.
    /// </summary>
    /// <param name="member">The member to kick.</param>
    /// <param name="staffMember">The staff member who issued the kick.</param>
    /// <param name="reason">The reason for the kick.</param>
    /// <returns>The newly created infraction.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="member" /> and <paramref name="staffMember" /> are not in the same guild.
    /// </exception>
    public async Task<Infraction> KickAsync(DiscordMember member, DiscordMember staffMember, string? reason)
    {
        if (member is null) throw new ArgumentNullException(nameof(member));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        if (member.Guild != staffMember.Guild)
            throw new ArgumentException("The member and staff member must be in the same guild.");

        member = await member.NormalizeClientAsync(_discordClient);
        staffMember = await staffMember.NormalizeClientAsync(_discordClient);

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace()
        };

        Infraction infraction = await _infractionService.CreateInfractionAsync(InfractionType.Kick, member, staffMember, options);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.KickedUser.FormatSmart(new {staffMember, reason});
        _ = member.RemoveAsync(reason);

        return infraction;
    }

    /// <summary>
    ///     Revokes a ban for a user.
    /// </summary>
    /// <param name="user">The user whose ban to revoke.</param>
    /// <param name="revoker">The member who revoked the ban.</param>
    /// <param name="reason">The reason for the revocation.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="revoker" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task RevokeBanAsync(DiscordUser user, DiscordMember revoker, string? reason)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (revoker is null) throw new ArgumentNullException(nameof(revoker));

        user = await user.NormalizeClientAsync(_discordClient);
        revoker = await revoker.NormalizeClientAsync(_discordClient);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        TemporaryBan? temporaryBan =
            await context.TemporaryBans.FirstOrDefaultAsync(b => b.User == user && b.Guild == revoker.Guild);

        if (temporaryBan is not null)
        {
            lock (_temporaryBans)
                _temporaryBans.Remove(temporaryBan);

            context.Remove(temporaryBan);
        }

        await context.SaveChangesAsync();

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.SpringGreen);
        embed.WithAuthor(user);
        embed.WithTitle("User unbanned");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, revoker.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason.AsNullIfWhiteSpace()), EmbedFieldNames.Reason, reason);
        _ = _corePlugin.LogAsync(revoker.Guild, embed);

        reason = reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.UnbannedUser.FormatSmart(new {staffMember = revoker, reason});
        _ = revoker.Guild.UnbanMemberAsync(user, reason);
    }

    /// <summary>
    ///     Temporarily bans a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="issuer">The staff member who issued the ban.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="duration">The duration of the ban.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<Infraction> TemporaryBanAsync(DiscordUser user, DiscordMember issuer, string? reason, TimeSpan duration)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (issuer is null) throw new ArgumentNullException(nameof(issuer));

        user = await user.NormalizeClientAsync(_discordClient);
        issuer = await issuer.NormalizeClientAsync(_discordClient);

        var options = new InfractionOptions
        {
            NotifyUser = true,
            Reason = reason.AsNullIfWhiteSpace(),
            ExpirationTime = DateTimeOffset.UtcNow + duration
        };

        DiscordGuild guild = issuer.Guild;
        await CreateTemporaryBanAsync(user, guild, options.ExpirationTime.Value);

        Infraction infraction =
            await _infractionService.CreateInfractionAsync(InfractionType.TemporaryBan, user, issuer, options);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = AuditLogReasons.TempBannedUser.FormatSmart(new {staffMember = issuer, reason, duration = duration.Humanize()});
        await guild.BanMemberAsync(user.Id, reason: reason);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User temporarily banned");
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.UserID, user.Id, true);
        embed.AddField(EmbedFieldNames.StaffMember, issuer.Mention, true);
        embed.AddField(EmbedFieldNames.ExpirationTime,
            Formatter.Timestamp(options.ExpirationTime.Value, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(infractionCount > 0, EmbedFieldNames.TotalUserInfractions, infractionCount, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), EmbedFieldNames.Reason, options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        _ = _corePlugin.LogAsync(guild, embed);

        return infraction;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer.Start();
        return Task.CompletedTask;
    }

    private async Task CreateTemporaryBanAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset expirationTime)
    {
        var temporaryBan = new TemporaryBan(user, guild, expirationTime);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        EntityEntry<TemporaryBan> entry = await context.TemporaryBans.AddAsync(temporaryBan);
        await context.SaveChangesAsync();

        temporaryBan = entry.Entity;

        lock (_temporaryBans)
            _temporaryBans.Add(temporaryBan);
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        foreach (TemporaryBan ban in _temporaryBans.ToArray().Where(b => b.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            DiscordMember botMember = await ban.Guild.GetMemberAsync(_discordClient.CurrentUser.Id);
            await RevokeBanAsync(ban.User, botMember, "Temporary ban expired");
        }
    }
}
