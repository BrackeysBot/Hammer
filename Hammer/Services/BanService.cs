using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Hammer.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using X10D.Text;
using Timer = System.Timers.Timer;

namespace Hammer.Services;

/// <summary>
///     Represents a service that handles guild bans and kicks.
/// </summary>
internal sealed class BanService : BackgroundService
{
    private static readonly TimeSpan QueryInterval = TimeSpan.FromSeconds(30);
    private readonly List<TemporaryBan> _temporaryBans = new();
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly DiscordClient _discordClient;
    private readonly DiscordLogService _logService;
    private readonly InfractionService _infractionService;
    private readonly MailmanService _mailmanService;
    private readonly RuleService _ruleService;
    private readonly Timer _timer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="BanService" /> class.
    /// </summary>
    public BanService(
        IDbContextFactory<HammerContext> dbContextFactory,
        DiscordClient discordClient,
        DiscordLogService logService,
        InfractionService infractionService,
        MailmanService mailmanService,
        RuleService ruleService
    )
    {
        _dbContextFactory = dbContextFactory;
        _discordClient = discordClient;
        _logService = logService;
        _infractionService = infractionService;
        _mailmanService = mailmanService;
        _ruleService = ruleService;

        _timer.Interval = QueryInterval.TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
    }

    /// <summary>
    ///     Adds a temporary ban to the database.
    /// </summary>
    /// <param name="temporaryBan">The temporary ban to add.</param>
    /// <returns>The <see cref="TemporaryBan" /> entity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="temporaryBan" /> is <see langword="null" />.</exception>
    public async Task<TemporaryBan> AddTemporaryBanAsync(TemporaryBan temporaryBan)
    {
        ArgumentNullException.ThrowIfNull(temporaryBan);
        TemporaryBan? existingBan;

        lock (_temporaryBans)
        {
            existingBan = _temporaryBans.Find(b => b.UserId == temporaryBan.UserId && b.GuildId == temporaryBan.GuildId);
            if (existingBan is not null)
                return existingBan;
        }

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        existingBan = await context.TemporaryBans.FindAsync(temporaryBan.UserId, temporaryBan.GuildId).ConfigureAwait(false);
        if (existingBan is not null)
            return existingBan;

        lock (_temporaryBans)
            _temporaryBans.Add(temporaryBan);

        temporaryBan = (await context.TemporaryBans.AddAsync(temporaryBan).ConfigureAwait(false)).Entity;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return temporaryBan;
    }

    /// <summary>
    ///     Bans a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="issuer">The staff member who issued the ban.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="ruleBroken">The rule which was broken, if any.</param>
    /// <param name="clearHistory">
    ///     <see langword="true" /> to clear the user's recent messages in text channels; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSuccess)> BanAsync(
        DiscordUser user,
        DiscordMember issuer,
        string? reason,
        Rule? ruleBroken,
        bool clearHistory
    )
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(issuer);

        var options = new InfractionOptions
        {
            NotifyUser = false,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        (Infraction infraction, bool success) = await _infractionService
            .CreateInfractionAsync(InfractionType.Ban, user, issuer, options)
            .ConfigureAwait(false);

        DiscordGuild guild = issuer.Guild;
        int infractionCount = _infractionService.GetInfractionCount(user, guild);

        Rule? rule = null;
        if (infraction.RuleId is { } ruleId && _ruleService.GuildHasRule(infraction.GuildId, ruleId))
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Banned by {issuer.GetUsernameWithDiscriminator()}: {reason}";

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User banned");
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", issuer.Mention, true);
        embed.AddFieldIf(infractionCount > 0, "Total User Infractions", infractionCount, true);
        embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _logService.LogAsync(guild, embed).ConfigureAwait(false);
        await _mailmanService.SendInfractionAsync(infraction, infractionCount, options).ConfigureAwait(false);

        await guild.BanMemberAsync(user.Id, reason: reason, delete_message_days: clearHistory ? 7 : 0).ConfigureAwait(false);
        return (infraction, success);
    }

    public TemporaryBan? GetTemporaryBan(DiscordUser user, DiscordGuild guild)
    {
        lock (_temporaryBans)
            return _temporaryBans.Find(b => b.UserId == user.Id && b.GuildId == guild.Id);
    }

    /// <summary>
    ///     Gets the temporary bans in a specified guild.
    /// </summary>
    /// <param name="guild">The guild whose temporary bans to return.</param>
    /// <returns>A read-only view of the temporary bans in the specified guild.</returns>
    public IReadOnlyList<TemporaryBan> GetTemporaryBans(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        var result = new List<TemporaryBan>();

        lock (_temporaryBans)
        {
            foreach (TemporaryBan temporaryBan in _temporaryBans)
            {
                if (temporaryBan.GuildId == guild.Id)
                    result.Add(temporaryBan);
            }
        }

        return result.AsReadOnly();
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
            if (_temporaryBans.Exists(x => x.UserId == user.Id && x.GuildId == guild.Id))
                return true;
        }

        IReadOnlyList<DiscordBan>? bans = await guild.GetBansAsync().ConfigureAwait(false);
        return bans.Any(b => b.User == user);
    }

    /// <summary>
    ///     Kicks members from the guild.
    /// </summary>
    /// <param name="member">The member to kick.</param>
    /// <param name="staffMember">The staff member who issued the kick.</param>
    /// <param name="reason">The reason for the kick.</param>
    /// <param name="ruleBroken">The rule which was broken, if any.</param>
    /// <param name="clearHistory">
    ///     <see langword="true" /> to clear the user's recent messages in text channels; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="member" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="member" /> and <paramref name="staffMember" /> are not in the same guild.
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSucess)> KickAsync(
        DiscordMember member,
        DiscordMember staffMember,
        string? reason,
        Rule? ruleBroken,
        bool clearHistory
    )
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(staffMember);

        DiscordGuild guild = staffMember.Guild;
        if (member.Guild != guild)
            throw new ArgumentException("The member and staff member must be in the same guild.");

        var options = new InfractionOptions
        {
            NotifyUser = false,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        (Infraction infraction, bool success) = await _infractionService
            .CreateInfractionAsync(InfractionType.Kick, member, staffMember, options)
            .ConfigureAwait(false);

        Rule? rule = null;
        if (infraction.RuleId is { } ruleId && _ruleService.GuildHasRule(infraction.GuildId, ruleId))
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Kicked by {staffMember.GetUsernameWithDiscriminator()}: {reason}";

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(member);
        embed.WithTitle("User kicked");
        embed.AddField("User", member.Mention, true);
        embed.AddField("User ID", member.Id, true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        embed.AddField("Messages Cleared", clearHistory ? "Yes" : "No", true);
        embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _logService.LogAsync(guild, embed).ConfigureAwait(false);

        int infractionCount = _infractionService.GetInfractionCount(member, guild);
        await _mailmanService.SendInfractionAsync(infraction, infractionCount, options).ConfigureAwait(false);

        await member.RemoveAsync(reason).ConfigureAwait(false);

        if (clearHistory)
        {
            _ = Task.Run(async () =>
            {
                IEnumerable<DiscordChannel> channels = guild.Channels.Values
                    .Concat(guild.Threads.Values)
                    .Where(c => c.Type is ChannelType.Text or ChannelType.PublicThread or ChannelType.PrivateThread);

                var tasks = new List<Task>();

                foreach (DiscordChannel channel in channels)
                {
                    var messagesToDelete = new List<DiscordMessage>();
                    IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync().ConfigureAwait(false);
                    messagesToDelete.AddRange(messages.Where(m => m.Author.Id == member.Id));
                    if (messagesToDelete.Count > 0)
                        tasks.Add(channel.DeleteMessagesAsync(messagesToDelete, "User was kicked"));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            });
        }

        return (infraction, success);
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
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(revoker);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        TemporaryBan? temporaryBan =
            await context.TemporaryBans.FirstOrDefaultAsync(b => b.UserId == user.Id && b.GuildId == revoker.Guild.Id)
                .ConfigureAwait(false);

        if (temporaryBan is not null)
        {
            lock (_temporaryBans)
                _temporaryBans.Remove(temporaryBan);

            context.Remove(temporaryBan);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.SpringGreen);
        embed.WithAuthor(user);
        embed.WithTitle("User unbanned");
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", revoker.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason.AsNullIfWhiteSpace()), "Reason", reason);
        await _logService.LogAsync(revoker.Guild, embed).ConfigureAwait(false);

        reason = reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Unbanned by {revoker.GetUsernameWithDiscriminator()}: {reason}";
        await revoker.Guild.UnbanMemberAsync(user, reason).ConfigureAwait(false);
    }

    /// <summary>
    ///     Temporarily bans a user.
    /// </summary>
    /// <param name="user">The user to ban.</param>
    /// <param name="issuer">The staff member who issued the ban.</param>
    /// <param name="reason">The reason for the ban.</param>
    /// <param name="duration">The duration of the ban.</param>
    /// <param name="ruleBroken">The rule which was broken, if any.</param>
    /// <param name="clearHistory">
    ///     <see langword="true" /> to clear the user's recent messages in text channels; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>
    ///     A tuple containing the created infraction, and a boolean indicating whether the user was successfully DMd.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="issuer" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<(Infraction Infraction, bool DmSuccess)> TemporaryBanAsync(
        DiscordUser user,
        DiscordMember issuer,
        string? reason,
        TimeSpan duration,
        Rule? ruleBroken,
        bool clearHistory
    )
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(issuer);

        var options = new InfractionOptions
        {
            NotifyUser = true,
            ExpirationTime = DateTimeOffset.UtcNow + duration,
            Reason = reason.AsNullIfWhiteSpace(),
            RuleBroken = ruleBroken
        };

        DiscordGuild guild = issuer.Guild;
        await CreateTemporaryBanAsync(user, guild, options.ExpirationTime.Value).ConfigureAwait(false);

        (Infraction infraction, bool success) = await _infractionService
            .CreateInfractionAsync(InfractionType.TemporaryBan, user, issuer, options)
            .ConfigureAwait(false);
        int infractionCount = _infractionService.GetInfractionCount(user, issuer.Guild);

        Rule? rule = null;
        if (infraction.RuleId is { } ruleId && _ruleService.GuildHasRule(infraction.GuildId, ruleId))
            rule = _ruleService.GetRuleById(infraction.GuildId, ruleId);

        reason = options.Reason.WithWhiteSpaceAlternative("No reason specified");
        reason = $"Temp-Banned by {issuer.GetUsernameWithDiscriminator()} ({duration.Humanize()}): {reason}";
        await guild.BanMemberAsync(user.Id, reason: reason).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User temporarily banned");
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", issuer.Mention, true);
        embed.AddField("Messages Cleared", clearHistory ? "Yes" : "No", true);
        embed.AddField("Expiration Time", Formatter.Timestamp(options.ExpirationTime.Value, TimestampFormat.ShortDateTime), true);
        embed.AddFieldIf(infractionCount > 0, "Total User Infractions", infractionCount, true);
        embed.AddFieldIf(rule is not null, "Rule Broken", () => $"{rule!.Id} - {rule.Brief ?? rule.Description}", true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(options.Reason), "Reason", options.Reason);
        embed.WithFooter($"Infraction {infraction.Id}");
        await _logService.LogAsync(guild, embed).ConfigureAwait(false);

        if (clearHistory)
        {
            _ = Task.Run(async () =>
            {
                IEnumerable<DiscordChannel> channels = guild.Channels.Values
                    .Concat(guild.Threads.Values)
                    .Where(c => c.Type is ChannelType.Text or ChannelType.PublicThread or ChannelType.PrivateThread);

                var tasks = new List<Task>();

                foreach (DiscordChannel channel in channels)
                {
                    var messagesToDelete = new List<DiscordMessage>();
                    IReadOnlyList<DiscordMessage> messages = await channel.GetMessagesAsync().ConfigureAwait(false);
                    messagesToDelete.AddRange(messages.Where(m => m.Author.Id == user.Id));
                    if (messagesToDelete.Count > 0)
                        tasks.Add(channel.DeleteMessagesAsync(messagesToDelete, "User was temp-banned"));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            });
        }

        return (infraction, success);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer.Start();
        return UpdateFromDatabaseAsync();
    }

    private async Task CreateTemporaryBanAsync(DiscordUser user, DiscordGuild guild, DateTimeOffset expirationTime)
    {
        var temporaryBan = TemporaryBan.Create(user, guild, expirationTime);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        EntityEntry<TemporaryBan> entry = await context.TemporaryBans.AddAsync(temporaryBan).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        temporaryBan = entry.Entity;

        lock (_temporaryBans)
            _temporaryBans.Add(temporaryBan);
    }

    private async void TimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        TemporaryBan[] temporaryBans;

        lock (_temporaryBans)
            temporaryBans = _temporaryBans.ToArray();

        foreach (TemporaryBan ban in temporaryBans.Where(b => b.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            if (!_discordClient.Guilds.TryGetValue(ban.GuildId, out DiscordGuild? guild))
                continue;

            try
            {
                DiscordMember botMember = await guild.GetMemberAsync(_discordClient.CurrentUser.Id).ConfigureAwait(false);
                DiscordUser? user = await _discordClient.GetUserAsync(ban.UserId).ConfigureAwait(false);
                await RevokeBanAsync(user, botMember, "Temporary ban expired").ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                // ignored
            }
        }
    }

    private async Task UpdateFromDatabaseAsync()
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        lock (_temporaryBans)
        {
            _temporaryBans.Clear();
            _temporaryBans.AddRange(context.TemporaryBans);
        }
    }
}
