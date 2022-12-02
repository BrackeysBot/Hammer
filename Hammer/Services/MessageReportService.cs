using System.Diagnostics.CodeAnalysis;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using X10D.DSharpPlus;
using ILogger = NLog.ILogger;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles message reports from users.
/// </summary>
internal sealed class MessageReportService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<BlockedReporter> _blockedReporters = new();
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;
    private readonly DiscordClient _discordClient;
    private readonly MessageTrackingService _messageTrackingService;
    private readonly List<ReportedMessage> _reportedMessages = new();
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReportService" /> class.
    /// </summary>
    public MessageReportService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService,
        MessageTrackingService messageTrackingService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
        _messageTrackingService = messageTrackingService;
    }

    /// <summary>
    ///     Blocks a user from making reports in a specified guild.
    /// </summary>
    /// <param name="user">The user whose reports to block.</param>
    /// <param name="staffMember">The staff member who issued the block.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task BlockUserAsync(DiscordUser user, DiscordMember staffMember)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(staffMember);

        if (IsUserBlocked(user, staffMember.Guild)) return;

        var blockedReporter = new BlockedReporter
        {
            UserId = user.Id,
            GuildId = staffMember.Guild.Id,
            StaffMemberId = staffMember.Id,
            BlockedAt = DateTimeOffset.UtcNow
        };

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        blockedReporter = (await context.AddAsync(blockedReporter).ConfigureAwait(false)).Entity;
        await context.SaveChangesAsync().ConfigureAwait(false);

        _blockedReporters.Add(blockedReporter);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithAuthor(user);
        embed.WithTitle("User reports blocked");
        embed.WithDescription($"Message reports will no longer be received by {user.Mention}.");
        embed.AddField("User", user.Mention, true);
        embed.AddField("User ID", user.Id, true);
        embed.AddField("Staff Member", staffMember.Mention, true);
        await _logService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a new message report.
    /// </summary>
    /// <param name="message">The message to report.</param>
    /// <param name="reporter">The user issuing the report.</param>
    /// <returns>The reported message.</returns>
    public async Task<ReportedMessage> CreateNewMessageReportAsync(DiscordMessage message, DiscordMember reporter)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(reporter);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        var reportedMessage = new ReportedMessage(message, reporter);
        EntityEntry<ReportedMessage> entry = await context.AddAsync(reportedMessage).ConfigureAwait(false);

        await context.SaveChangesAsync().ConfigureAwait(false);
        reportedMessage = entry.Entity;
        _reportedMessages.Add(reportedMessage);
        return reportedMessage;
    }

    /// <summary>
    ///     Enumerates all reports submitted against a member.
    /// </summary>
    /// <param name="member">The member whose received reports to retrieve.</param>
    /// <returns>An enumerable collection of <see cref="ReportedMessage" /> values.</returns>
    public IAsyncEnumerable<ReportedMessage> EnumerateReportsAsync(DiscordMember member)
    {
        return EnumerateReportsAsync(member, member.Guild);
    }

    /// <summary>
    ///     Enumerates all reports submitted against a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose received reports to retrieve.</param>
    /// <param name="guild">The guild whose reports to search.</param>
    /// <returns>An enumerable collection of <see cref="ReportedMessage" /> values.</returns>
    public async IAsyncEnumerable<ReportedMessage> EnumerateReportsAsync(DiscordUser user, DiscordGuild guild)
    {
        foreach (ReportedMessage reportedMessage in _reportedMessages)
        {
            if (reportedMessage.AuthorId == user.Id && reportedMessage.GuildId == guild.Id)
                yield return reportedMessage;
        }
    }

    /// <summary>
    ///     Enumerates all reports submitted by a member.
    /// </summary>
    /// <param name="member">The member whose submitted reports to retrieve.</param>
    /// <returns>An enumerable collection of <see cref="ReportedMessage" /> values.</returns>
    public IAsyncEnumerable<ReportedMessage> EnumerateSubmittedReportsAsync(DiscordMember member)
    {
        return EnumerateSubmittedReportsAsync(member, member.Guild);
    }

    /// <summary>
    ///     Enumerates all reports submitted by a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose submitted reports to retrieve.</param>
    /// <param name="guild">The guild whose reports to search.</param>
    /// <returns>An enumerable collection of <see cref="ReportedMessage" /> values.</returns>
    public async IAsyncEnumerable<ReportedMessage> EnumerateSubmittedReportsAsync(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        foreach (ReportedMessage reportedMessage in _reportedMessages)
        {
            if (reportedMessage.ReporterId == user.Id && reportedMessage.GuildId == guild.Id)
                yield return reportedMessage;
        }
    }

    /// <summary>
    ///     Returns the count of reports on a specified message.
    /// </summary>
    /// <param name="message">The message whose report count to retrieve.</param>
    /// <returns>The number of reports made on <paramref name="message" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
    public int GetReportCount(DiscordMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _reportedMessages.Count(m => m.MessageId == message.Id);
    }

    /// <summary>
    ///     Gets all reports submitted against a member.
    /// </summary>
    /// <param name="member">The member whose received reports to retrieve.</param>
    /// <returns>A read-only view of <see cref="ReportedMessage" /> values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public Task<IReadOnlyList<ReportedMessage>> GetReportsAsync(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return GetReportsAsync(member, member.Guild);
    }

    /// <summary>
    ///     Gets all reports submitted against a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose received reports to retrieve.</param>
    /// <param name="guild">The guild whose reports to search.</param>
    /// <returns>A read-only view of <see cref="ReportedMessage" /> values.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<IReadOnlyList<ReportedMessage>> GetReportsAsync(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        var list = new List<ReportedMessage>();

        await foreach (ReportedMessage reportedMessage in EnumerateReportsAsync(user, guild).ConfigureAwait(false))
            list.Add(reportedMessage);

        return list.AsReadOnly();
    }

    /// <summary>
    ///     Gets all reports submitted by a member.
    /// </summary>
    /// <param name="member">The member whose submitted reports to retrieve.</param>
    /// <returns>A read-only view of <see cref="ReportedMessage" /> values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public Task<IReadOnlyList<ReportedMessage>> GetSubmittedReportsAsync(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        return GetSubmittedReportsAsync(member, member.Guild);
    }

    /// <summary>
    ///     Gets all reports submitted by a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose submitted reports to retrieve.</param>
    /// <param name="guild">The guild whose reports to search.</param>
    /// <returns>A read-only view of <see cref="ReportedMessage" /> values.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<IReadOnlyList<ReportedMessage>> GetSubmittedReportsAsync(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        var list = new List<ReportedMessage>();

        await foreach (ReportedMessage reportedMessage in EnumerateSubmittedReportsAsync(user, guild).ConfigureAwait(false))
            list.Add(reportedMessage);

        return list.AsReadOnly();
    }

    /// <summary>
    ///     Determines if a specified reporter has already
    /// </summary>
    /// <param name="message">The reported message.</param>
    /// <param name="reporter">The user issuing the report.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="reporter" /> has already reported <paramref name="message" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="reporter" /> is <see langword="null" />.</para>
    /// </exception>
    public bool HasUserReportedMessage(DiscordMessage message, DiscordMember reporter)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(reporter);

        return _reportedMessages.Exists(m => m.MessageId == message.Id && m.ReporterId == reporter.Id);
    }

    /// <summary>
    ///     Returns a value indicating whether the user is blocked from making reports in the specified guild.
    /// </summary>
    /// <param name="user">The user whose block status to retrieve.</param>
    /// <param name="guild">The guild whose blocked users by which to filter.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="user" /> is blocked from making reports in <paramref name="guild" />;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public bool IsUserBlocked(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        return _blockedReporters.Exists(r => r.UserId == user.Id && r.GuildId == guild.Id);
    }

    /// <summary>
    ///     Reports a message to staff members.
    /// </summary>
    /// <param name="message">The message to report.</param>
    /// <param name="reporter">The member who reported the message.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="reporter" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<bool> ReportMessageAsync(DiscordMessage message, DiscordMember reporter)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(reporter);

        if (IsUserBlocked(reporter, reporter.Guild))
        {
            Logger.Info($"{reporter} reported a message, but is blocked from doing so");
            return false;
        }

        if (message.Author is null)
            message = await message.Channel.GetMessageAsync(message.Id).ConfigureAwait(false);

        MessageTrackState trackState = _messageTrackingService.GetMessageTrackState(message);
        if ((trackState & MessageTrackState.Deleted) != 0)
        {
            Logger.Warn($"{reporter} attempted to report {message} but the message is deleted!");

            // we can stop tracking reports for a message which is deleted
            _reportedMessages.RemoveAll(m => m.MessageId == message.Id);
            return false;
        }

        bool duplicateReport = HasUserReportedMessage(message, reporter);
        if (duplicateReport)
        {
            Logger.Info($"{reporter} attempted to create a duplicate report on " +
                        $"{message} by {message.Author} in {message.Channel} - this report will not be logged in Discord.");
            return false;
        }

        Logger.Info($"{reporter} reported {message} by {message.Author} in {message.Channel}");
        await CreateNewMessageReportAsync(message, reporter).ConfigureAwait(false);

        if (!_configurationService.TryGetGuildConfiguration(message.Channel.Guild, out GuildConfiguration? guildConfiguration))
            return false;

        int urgentReportThreshold = guildConfiguration.UrgentReportThreshold;
        int reportCount = GetReportCount(message);

        StaffNotificationOptions notificationOptions;

        if (reportCount >= urgentReportThreshold)
            notificationOptions = StaffNotificationOptions.Administrator | StaffNotificationOptions.Moderator;
        else
            notificationOptions = StaffNotificationOptions.Here;

        await _logService.LogAsync(reporter.Guild, CreateStaffReportEmbed(message, reporter), notificationOptions)
            .ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Returns the count of reports on a specified message.
    /// </summary>
    /// <param name="id">The ID of the report to retrieve.</param>
    /// <param name="reportedMessage">
    ///     When this method returns, contains the report whose ID matches <paramref name="id" />, or <see langword="null" /> if
    ///     no such report was found.
    /// </param>
    /// <returns><see langword="true" /> if a matching report was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetReport(long id, [NotNullWhen(true)] out ReportedMessage? reportedMessage)
    {
        reportedMessage = _reportedMessages.Find(r => r.Id == id);
        return reportedMessage is not null;
    }

    /// <summary>
    ///     Unblocks a user from making reports in a specified guild, allowing them to report again.
    /// </summary>
    /// <param name="user">The user whose reports to unblock.</param>
    /// <param name="staffMember">The staff member who unblocked the user.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task UnblockUserAsync(DiscordUser user, DiscordMember staffMember)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(staffMember);

        if (!IsUserBlocked(user, staffMember.Guild)) return;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        BlockedReporter? blockedReporter =
            await context.BlockedReporters.FirstOrDefaultAsync(r => r.UserId == user.Id && r.GuildId == staffMember.Guild.Id)
                .ConfigureAwait(false);

        if (blockedReporter is null)
            Logger.Warn($"Could not unblock {user}: was allegedly blocked, but dind't find BlockedReporter entity!");
        else
        {
            _blockedReporters.Remove(blockedReporter);
            context.Remove(blockedReporter);
            await context.SaveChangesAsync().ConfigureAwait(false);

            var embed = new DiscordEmbedBuilder();
            embed.WithColor(DiscordColor.Green);
            embed.WithAuthor(user);
            embed.WithTitle("User reports unblocked");
            embed.WithDescription($"Message reports by {user.Mention} will now be logged.");
            embed.AddField("User", user.Mention, true);
            embed.AddField("User ID", user.Id, true);
            embed.AddField("Staff Member", staffMember.Mention, true);
            await _logService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);

        _blockedReporters.Clear();
        _blockedReporters.AddRange(context.BlockedReporters);

        _reportedMessages.Clear();
        _discordClient.GuildAvailable += OnGuildAvailable;
    }

    private async Task OnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        foreach (ReportedMessage reportedMessage in context.ReportedMessages.Where(r => r.GuildId == e.Guild.Id))
        {
            DiscordChannel? channel = e.Guild.GetChannel(reportedMessage.ChannelId);
            if (channel is null)
            {
                context.Entry(reportedMessage).State = EntityState.Deleted;
            }
            else
            {
                try
                {
                    await channel.GetMessageAsync(reportedMessage.MessageId).ConfigureAwait(false);
                    _reportedMessages.Add(reportedMessage);
                }
                catch (NotFoundException)
                {
                    context.Entry(reportedMessage).State = EntityState.Deleted;
                }
            }
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private DiscordEmbed CreateStaffReportEmbed(DiscordMessage message, DiscordMember reporter)
    {
        DiscordColor color = DiscordColor.Orange;

        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return new DiscordEmbedBuilder()
            .WithColor(color)
            .WithTitle("Message Reported")
            .WithDescription($"{reporter.Mention} reported a message in {message.Channel.Mention}")
            .AddField("Channel", message.Channel.Mention, true)
            .AddField("Author", message.Author.Mention, true)
            .AddField("Reporter", reporter.Mention, true)
            .AddField("Message ID", Formatter.MaskedUrl(message.Id.ToString(), message.JumpLink), true)
            .AddField("Message Time", Formatter.Timestamp(message.CreationTimestamp, TimestampFormat.ShortDateTime),
                true)
            .AddFieldIf(hasContent, "Content", content)
            .AddFieldIf(hasAttachments, "Attachments", attachments);
    }
}
