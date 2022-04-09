using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles message reports from users.
/// </summary>
internal sealed class MessageReportService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<BlockedReporter> _blockedReporters = new();
    private readonly ConfigurationService _configurationService;
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly MessageTrackingService _messageTrackingService;
    private readonly List<ReportedMessage> _reportedMessages = new();
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReportService" /> class.
    /// </summary>
    public MessageReportService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient,
        ConfigurationService configurationService, MessageTrackingService messageTrackingService)
    {
        _scopeFactory = scopeFactory;
        _configurationService = configurationService;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _messageTrackingService = messageTrackingService;
    }

    /// <summary>
    ///     Blocks a user from making reports in a specified guild.
    /// </summary>
    /// <param name="user">The user whose reports to block.</param>
    /// <param name="staffMember">The staff member who issued the block.</param>
    public async Task BlockUserAsync(DiscordUser user, DiscordMember staffMember)
    {
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

        blockedReporter = (await context.AddAsync(blockedReporter)).Entity;
        await context.SaveChangesAsync();

        _blockedReporters.Add(blockedReporter);
    }

    /// <summary>
    ///     Creates a new message report.
    /// </summary>
    /// <param name="message">The message to report.</param>
    /// <param name="reporter">The user issuing the report.</param>
    /// <returns>The reported message.</returns>
    public async Task<ReportedMessage> CreateNewMessageReportAsync(DiscordMessage message, DiscordMember reporter)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (reporter is null) throw new ArgumentNullException(nameof(reporter));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        var reportedMessage = new ReportedMessage(message, reporter);
        EntityEntry<ReportedMessage> entry = await context.AddAsync(reportedMessage);

        await context.SaveChangesAsync();
        reportedMessage = entry.Entity;
        _reportedMessages.Add(reportedMessage);
        return reportedMessage;
    }

    /// <summary>
    ///     Returns the count of reports on a specified message.
    /// </summary>
    /// <param name="message">The message whose report count to retrieve.</param>
    /// <returns>The number of reports made on <paramref name="message" />.</returns>
    public int GetReportCount(DiscordMessage message)
    {
        return _reportedMessages.Count(m => m.MessageId == message.Id);
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
    public bool HasUserReportedMessage(DiscordMessage message, DiscordMember reporter)
    {
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
    public bool IsUserBlocked(DiscordUser user, DiscordGuild guild)
    {
        return _blockedReporters.Exists(r => r.UserId == user.Id && r.GuildId == guild.Id);
    }

    /// <summary>
    ///     Reports a message to staff members.
    /// </summary>
    /// <param name="message">The message to report.</param>
    /// <param name="reporter">The member who reported the message.</param>
    public async Task<bool> ReportMessageAsync(DiscordMessage message, DiscordMember reporter)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (reporter is null) throw new ArgumentNullException(nameof(reporter));

        if (IsUserBlocked(reporter, reporter.Guild))
        {
            Logger.Info(LoggerMessages.MessageReportBlocked.FormatSmart(new {user = reporter}));
            return false;
        }

        if (message.Author is null)
            message = await message.Channel.GetMessageAsync(message.Id);
        else
            message = await message.NormalizeClientAsync(_discordClient);

        reporter = await reporter.NormalizeClientAsync(_discordClient);

        MessageTrackState trackState = _messageTrackingService.GetMessageTrackState(message);
        if ((trackState & MessageTrackState.Deleted) != 0)
        {
            Logger.Warn(LoggerMessages.UserReportedDeletedMessage.FormatSmart(new {reporter, message}));

            // we can stop tracking reports for a message which is deleted
            _reportedMessages.RemoveAll(m => m.MessageId == message.Id);
            return false;
        }

        bool duplicateReport = HasUserReportedMessage(message, reporter);
        if (duplicateReport)
        {
            Logger.Info(LoggerMessages.DuplicateMessageReport.FormatSmart(new {user = reporter, message}));
            return false;
        }

        Logger.Info(LoggerMessages.MessageReported.FormatSmart(new {user = reporter, message}));
        await CreateNewMessageReportAsync(message, reporter);

        GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(message.Channel.Guild);
        int urgentReportThreshold = guildConfiguration.UrgentReportThreshold;
        int reportCount = GetReportCount(message);

        StaffNotificationOptions notificationOptions;

        if (reportCount >= urgentReportThreshold)
            notificationOptions = StaffNotificationOptions.Administrator | StaffNotificationOptions.Moderator;
        else
            notificationOptions = StaffNotificationOptions.Here;

        _ = _corePlugin.LogAsync(reporter.Guild, CreateStaffReportEmbed(message, reporter), notificationOptions);
        return true;
    }

    /// <summary>
    ///     Unblocks a user from making reports in a specified guild, allowing them to report again.
    /// </summary>
    /// <param name="user">The user whose reports to unblock.</param>
    /// <param name="guild">The guild in which to unblock the user.</param>
    public async Task UnblockUserAsync(DiscordUser user, DiscordGuild guild)
    {
        if (!IsUserBlocked(user, guild)) return;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        BlockedReporter? blockedReporter =
            await context.BlockedReporters.FirstOrDefaultAsync(r => r.UserId == user.Id && r.GuildId == guild.Id);

        if (blockedReporter is null)
            Logger.Warn($"Could not unblock {user}: was allegedly blocked, but dind't find BlockedReporter entity!");
        else
        {
            _blockedReporters.Remove(blockedReporter);
            context.Remove(blockedReporter);
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken);

        _blockedReporters.Clear();
        _blockedReporters.AddRange(context.BlockedReporters);

        _reportedMessages.Clear();
        await foreach (ReportedMessage reportedMessage in context.ReportedMessages)
        {
            if (!_discordClient.Guilds.TryGetValue(reportedMessage.GuildId, out DiscordGuild? guild))
                continue;

            try
            {
                DiscordChannel channel = guild.GetChannel(reportedMessage.ChannelId);
                await channel.GetMessageAsync(reportedMessage.MessageId);

                _reportedMessages.Add(reportedMessage);
            }
            catch (NotFoundException)
            {
                context.Entry(reportedMessage).State = EntityState.Deleted;
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    private DiscordEmbed CreateStaffReportEmbed(DiscordMessage message, DiscordMember reporter)
    {
        GuildConfiguration guildConfiguration = _configurationService.GetGuildConfiguration(reporter.Guild);

        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return reporter.Guild.CreateDefaultEmbed()
            .WithColor(guildConfiguration.TertiaryColor)
            .WithTitle(EmbedTitles.MessageReported)
            .WithDescription(EmbedMessages.MessageReported.FormatSmart(new {user = reporter, channel = message.Channel}))
            .AddField(EmbedFieldNames.Channel, message.Channel.Mention, true)
            .AddField(EmbedFieldNames.Author, message.Author.Mention, true)
            .AddField(EmbedFieldNames.Reporter, reporter.Mention, true)
            .AddField(EmbedFieldNames.MessageID, Formatter.MaskedUrl(message.Id.ToString(), message.JumpLink), true)
            .AddField(EmbedFieldNames.MessageTime, Formatter.Timestamp(message.CreationTimestamp, TimestampFormat.ShortDateTime),
                true)
            .AddFieldIf(hasContent, EmbedFieldNames.Content, content)
            .AddFieldIf(hasAttachments, EmbedFieldNames.Attachments, attachments);
    }
}
