using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.Entities;
using DisCatSharp.EventArgs;
using Hammer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Hammer.Services;

/// <summary>
///     Represents a service which tracks specific user messages.
/// </summary>
internal sealed class MessageTrackingService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly DiscordClient _discordClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly List<TrackedMessage> _trackedMessages = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageReportService" /> class.
    /// </summary>
    public MessageTrackingService(IServiceScopeFactory scopeFactory, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
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
        TrackedMessage? trackedMessage = _trackedMessages.Find(m => m == message);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        if (trackedMessage is null)
        {
            trackedMessage = await context.TrackedMessages.FirstOrDefaultAsync(m => m.Id == message.Id);

            if (trackedMessage is null)
            {
                trackedMessage = TrackedMessage.FromDiscordMessage(message);
                trackedMessage.IsDeleted = deleted;
                EntityEntry<TrackedMessage> entry = await context.AddAsync(trackedMessage);
                trackedMessage = entry.Entity;
            }
            else
            {
                trackedMessage.IsDeleted = deleted;
                context.Update(trackedMessage);
            }

            _trackedMessages.Add(trackedMessage);
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "An exception was thrown when saving TrackedMessage to the database");
        }

        return trackedMessage;
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

        TrackedMessage trackedMessage = await GetTrackedMessageAsync(e.Message);
        trackedMessage.IsDeleted = true;
        trackedMessage.DeletionTimestamp = DateTimeOffset.UtcNow;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        context.Update(trackedMessage);
        await context.SaveChangesAsync();
    }

    private async Task DiscordClientOnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
    {
        if (GetMessageTrackState(e.Message) != MessageTrackState.Tracked)
            return;

        TrackedMessage trackedMessage = await GetTrackedMessageAsync(e.Message);
        trackedMessage.Content = e.Message.Content;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        context.Update(trackedMessage);
        await context.SaveChangesAsync();
    }

    private async Task RefreshFromDatabaseAsync(DiscordGuild guild)
    {
        ulong guildId = guild.Id;

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        IQueryable<TrackedMessage> messages = context.TrackedMessages.Where(m => m.GuildId == guildId);

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
                DiscordMessage message = await channel.GetMessageAsync(trackedMessage.Id);
                if (message is null)
                    trackedMessage.IsDeleted = true;
                else
                    _trackedMessages.Add(trackedMessage);
            }

            context.UpdateRange(channelGroups);
        }

        await context.SaveChangesAsync();
    }
}
