using System.Threading;
using System.Threading.Tasks;
using DisCatSharp;
using DisCatSharp.EventArgs;
using Hammer.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which listens for message edits and deletions by tracked users, and logs these actions.
/// </summary>
internal sealed class MessageWatchdogService : BackgroundService
{
    private readonly DiscordClient _discordClient;
    private readonly MessageTrackingService _messageTrackingService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly UserTrackingService _userTrackingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageWatchdogService" /> class.
    /// </summary>
    public MessageWatchdogService(IServiceScopeFactory scopeFactory, DiscordClient discordClient,
        MessageTrackingService messageTrackingService, UserTrackingService userTrackingService)
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _messageTrackingService = messageTrackingService;
        _userTrackingService = userTrackingService;
    }

    /// <summary>
    ///     Tracks a message edit.
    /// </summary>
    /// <param name="args">The event arguments as provided by the Discord wrapper, providing pre- and post- edit states.</param>
    public async Task TrackMessageEdit(MessageUpdateEventArgs args)
    {
        MessageEdit messageEdit = MessageEdit.FromMessageUpdateEventArgs(args);

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.AddAsync(messageEdit);
        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     Tracks a message deletion.
    /// </summary>
    /// <param name="args">The event arguments as provided by the Discord wrapper, providing the message state.</param>
    public async Task TrackMessageDeletion(MessageDeleteEventArgs args)
    {
        await _messageTrackingService.GetTrackedMessageAsync(args.Message, true);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.MessageDeleted += DiscordClientOnMessageDeleted;
        _discordClient.MessageUpdated += DiscordClientOnMessageUpdated;
        return Task.CompletedTask;
    }

    private async Task DiscordClientOnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
    {
        if (e.Guild is not { } guild) return;
        if (!_userTrackingService.IsUserTracked(e.Message.Author, guild)) return;

        await TrackMessageDeletion(e);
    }

    private async Task DiscordClientOnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
    {
        if (e.Guild is not { } guild) return;
        if (!_userTrackingService.IsUserTracked(e.Author, guild)) return;

        await TrackMessageEdit(e);
    }
}
