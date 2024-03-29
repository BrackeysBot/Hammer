using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hammer.Services;

/// <summary>
///     Represents a service which tracks specific user messages.
/// </summary>
internal sealed class MessageTrackingService : BackgroundService
{
    private readonly ILogger<MessageTrackingService> _logger;
    private readonly DiscordClient _discordClient;
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly List<TrackedMessage> _trackedMessages = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReportService" /> class.
    /// </summary>
    public MessageTrackingService(ILogger<MessageTrackingService> logger,
        IDbContextFactory<HammerContext> dbContextFactory,
        DiscordClient discordClient)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Enumerates the tracked messages written by a user in a specified guild.
    /// </summary>
    /// <param name="user">The user whose tracked messages to retrieve.</param>
    /// <param name="guild">The guild whose messages to search.</param>
    /// <returns>An enumerable collection of <see cref="TrackedMessage" /> instances.</returns>
    public async IAsyncEnumerable<TrackedMessage> EnumerateTrackedMessagesAsync(DiscordUser user, DiscordGuild guild)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (TrackedMessage message in context.TrackedMessages.Where(m => m.AuthorId == user.Id && m.GuildId == guild.Id))
            yield return message;
    }

    /// <summary>
    ///     Determines the current tracking state of a specified message ID.
    /// </summary>
    /// <param name="guildId">The guild ID.</param>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <returns>A <see cref="MessageTrackState" /> representing the tracked state of the specified message.</returns>
    public MessageTrackState GetMessageTrackState(ulong guildId, ulong channelId, ulong messageId)
    {
        TrackedMessage? trackedMessage = _trackedMessages.Find(m => messageId == m.Id
                                                                    && m.ChannelId == channelId
                                                                    && m.GuildId == guildId);

        if (trackedMessage is null) return MessageTrackState.NotTracked;
        if (trackedMessage.IsDeleted) return MessageTrackState.Tracked | MessageTrackState.Deleted;

        return MessageTrackState.Tracked;
    }

    /// <summary>
    ///     Determines the current tracking state of a specified message.
    /// </summary>
    /// <param name="message">The message whose status to retrieve.</param>
    /// <returns>A <see cref="MessageTrackState" /> representing the tracked state of the specified message.</returns>
    public MessageTrackState GetMessageTrackState(DiscordMessage message)
    {
        return GetMessageTrackState(message.Channel.Guild.Id, message.Channel.Id, message.Id);
    }

    /// <summary>
    ///     Gets the <see cref="TrackedMessage" /> for a specified <see cref="DiscordMessage" />, creating a new one if the
    ///     message is not already being tracked.
    /// </summary>
    /// <param name="message">The <see cref="DiscordMessage" /> to track.</param>
    /// <param name="deleted"><see langword="true" /> to mark the message as deleted; otherwise, <see langword="false" />.</param>
    /// <returns>
    ///     A <see cref="TrackedMessage" /> representing the tracked message mapping of <paramref name="message" />.
    /// </returns>
    public async Task<TrackedMessage> GetTrackedMessageAsync(DiscordMessage message, bool deleted = false)
    {
        TrackedMessage? trackedMessage = _trackedMessages.Find(m => m.Id == message.Id);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        if (trackedMessage is null)
        {
            trackedMessage = await context.TrackedMessages.FirstOrDefaultAsync(m => m.Id == message.Id).ConfigureAwait(false);

            if (trackedMessage is null)
            {
                trackedMessage = TrackedMessage.FromDiscordMessage(message);
                trackedMessage.IsDeleted = deleted;
                if (deleted) trackedMessage.DeletionTimestamp = DateTimeOffset.UtcNow;

                EntityEntry<TrackedMessage> entry = await context.AddAsync(trackedMessage).ConfigureAwait(false);
                trackedMessage = entry.Entity;
            }
            else
            {
                trackedMessage.IsDeleted = deleted;
                if (deleted) trackedMessage.DeletionTimestamp = DateTimeOffset.UtcNow;
                context.Update(trackedMessage);
            }

            _trackedMessages.Add(trackedMessage);
        }
        else
        {
            trackedMessage.IsDeleted = deleted;
            if (deleted) trackedMessage.DeletionTimestamp = DateTimeOffset.UtcNow;
            context.Update(trackedMessage);
        }

        try
        {
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception was thrown when saving TrackedMessage to the database");
        }

        return trackedMessage;
    }

    /// <inheritdoc />
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _discordClient.GuildAvailable -= DiscordClientOnGuildAvailable;
        _discordClient.MessageDeleted -= DiscordClientOnMessageDeleted;
        _discordClient.MessageUpdated -= DiscordClientOnMessageUpdated;

        return base.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        _discordClient.MessageDeleted += DiscordClientOnMessageDeleted;
        _discordClient.MessageUpdated += DiscordClientOnMessageUpdated;

        return Task.CompletedTask;
    }

    private Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        return RefreshFromDatabaseAsync(e.Guild);
    }

    private async Task DiscordClientOnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (GetMessageTrackState(e.Message) != MessageTrackState.Tracked)
            return;

        TrackedMessage trackedMessage = await GetTrackedMessageAsync(e.Message).ConfigureAwait(false);
        trackedMessage.IsDeleted = true;
        trackedMessage.DeletionTimestamp = DateTimeOffset.UtcNow;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Update(trackedMessage);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task DiscordClientOnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
    {
        if (e.Message.Channel.Guild is null) return;
        if (GetMessageTrackState(e.Message) != MessageTrackState.Tracked) return;

        TrackedMessage trackedMessage = await GetTrackedMessageAsync(e.Message).ConfigureAwait(false);
        trackedMessage.Content = e.Message.Content;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        context.Update(trackedMessage);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task RefreshFromDatabaseAsync(DiscordGuild guild)
    {
        ulong guildId = guild.Id;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        IEnumerable<TrackedMessage> messages = context.TrackedMessages.Where(m => m.GuildId == guildId).AsEnumerable();

        foreach (IGrouping<ulong, TrackedMessage> channelGroups in messages.GroupBy(m => m.ChannelId))
        {
            DiscordChannel channel = guild.GetChannel(channelGroups.Key);
            if (channel is null)
            {
                foreach (TrackedMessage trackedMessage in channelGroups) trackedMessage.IsDeleted = true;

                context.UpdateRange(channelGroups);
                continue;
            }

            foreach (TrackedMessage trackedMessage in channelGroups)
            {
                try
                {
                    DiscordMessage message = await channel.GetMessageAsync(trackedMessage.Id).ConfigureAwait(false);
                    if (message is null) trackedMessage.IsDeleted = true;
                    else _trackedMessages.Add(trackedMessage);
                }
                catch (NotFoundException)
                {
                    trackedMessage.IsDeleted = true;
                }
            }

            context.UpdateRange(channelGroups);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
