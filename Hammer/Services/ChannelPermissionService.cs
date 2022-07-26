using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;

namespace Hammer.Services;

/// <summary>
///     Represents a service which listens for channel, thread, and category creations - applying Muted role permissions where
///     necessary.
/// </summary>
internal sealed class ChannelPermissionService : BackgroundService
{
    private readonly DiscordClient _discordClient;
    private readonly MuteService _muteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ChannelPermissionService" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="muteService">The Mute service.</param>
    public ChannelPermissionService(DiscordClient discordClient, MuteService muteService)
    {
        _discordClient = discordClient;
        _muteService = muteService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.ChannelCreated += ChannelCreated;
        _discordClient.ThreadCreated += ThreadCreated;
        return Task.CompletedTask;
    }

    private Task ThreadCreated(DiscordClient sender, ThreadCreateEventArgs e)
    {
        return ApplyMutedPermissionsAsync(e.Thread);
    }

    private Task ChannelCreated(DiscordClient sender, ChannelCreateEventArgs e)
    {
        return ApplyMutedPermissionsAsync(e.Channel);
    }

    private async Task ApplyMutedPermissionsAsync(DiscordChannel channel)
    {
        if (!_muteService.TryGetMutedRole(channel.Guild, out var mutedRole))
            return;

        await channel.ModifyAsync(model =>
        {
            var builder = new DiscordOverwriteBuilder(mutedRole);
            builder.Deny(Permissions.SendMessages |
                         Permissions.SendMessagesInThreads |
                         Permissions.CreatePrivateThreads |
                         Permissions.CreatePublicThreads |
                         Permissions.AddReactions |
                         Permissions.UseVoice |
                         Permissions.RequestToSpeak);

            model.PermissionOverwrites = new[] {builder};
        });
    }
}
