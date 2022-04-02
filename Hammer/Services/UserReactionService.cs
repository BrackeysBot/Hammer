﻿using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Hammer.Configuration;
using Hammer.Resources;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which listens for user reactions.
/// </summary>
internal sealed class UserReactionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ConfigurationService _configurationService;
    private readonly DiscordClient _discordClient;
    private readonly MessageReportService _messageReportService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserReactionService" /> class.
    /// </summary>
    public UserReactionService(
        ConfigurationService configurationService,
        DiscordClient discordClient,
        MessageReportService messageReportService
    )
    {
        _configurationService = configurationService;
        _discordClient = discordClient;
        _messageReportService = messageReportService;
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.MessageReactionAdded += DiscordClientOnMessageReactionAdded;
        return Task.CompletedTask;
    }

    private async Task DiscordClientOnMessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e)
    {
        if (e.Guild is null)
            return;

        ReactionConfiguration reactionConfiguration = _configurationService.GetGuildConfiguration(e.Guild).ReactionConfiguration;
        string reaction = e.Emoji.GetDiscordName();
        if (reaction == reactionConfiguration.ReportReaction)
        {
            DiscordUser user = e.User;

            if (_messageReportService.IsUserBlocked(user, e.Guild))
            {
                Logger.Info(LoggerMessages.MessageReportBlocked.FormatSmart(new {user}));
                return;
            }

            await _messageReportService.ReportMessageAsync(e.Message, (DiscordMember) user);
        }
    }
}
