using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SmartFormat;
using ILogger = NLog.ILogger;

namespace Hammer.Services;

/// <summary>
///     Represents a service which logs and relays messages from staff members.
/// </summary>
internal sealed class MessageService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageService" /> class.
    /// </summary>
    public MessageService(
        IServiceScopeFactory scopeFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService
    )
    {
        _scopeFactory = scopeFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
    }

    /// <summary>
    ///     Sends a message to a member.
    /// </summary>
    /// <param name="recipient">The member to message.</param>
    /// <param name="staffMember">The staff member which sent the message.</param>
    /// <param name="message">The message to send.</param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="recipient" /> and <paramref name="staffMember" /> do not belong to the same guild.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="recipient" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="message" /> is <see langword="null" />, empty, or consists of only whitespace characters.</para>
    /// </exception>
    public async Task MessageMemberAsync(DiscordMember recipient, DiscordMember staffMember, string message)
    {
        if (recipient is null) throw new ArgumentNullException(nameof(recipient));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

        message = message.Trim();

        if (recipient.Guild != staffMember.Guild)
            throw new ArgumentException(ExceptionMessages.StaffMemberRecipientGuildMismatch, nameof(recipient));

        StaffMessage staffMessage = await CreateStaffMessageAsync(recipient, staffMember, message).ConfigureAwait(false);

        Logger.Info($"{staffMember} sent a message to {recipient} from {staffMember.Guild}. Contents: {message}");

        DiscordEmbed embed = await CreateUserEmbedAsync(staffMessage).ConfigureAwait(false);
        await recipient.SendMessageAsync(embed).ConfigureAwait(false);
        
        embed = await CreateStaffLogEmbedAsync(staffMessage).ConfigureAwait(false);
        await _logService.LogAsync(recipient.Guild, embed).ConfigureAwait(false);
    }

    private async Task<DiscordEmbed> CreateStaffLogEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId).ConfigureAwait(false);
        DiscordUser staffMember = await _discordClient.GetUserAsync(message.StaffMemberId).ConfigureAwait(false);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId).ConfigureAwait(false);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed(guildConfiguration);

        embedBuilder.WithAuthor($"Message #{message.Id}");
        embedBuilder.WithTitle("Message Sent");
        embedBuilder.WithDescription($"Staff member {staffMember.Mention} sent a message to {user.Mention}.");
        embedBuilder.AddField("Message", message.Content);

        return embedBuilder;
    }

    private async Task<DiscordEmbed> CreateUserEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId).ConfigureAwait(false);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId).ConfigureAwait(false);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed(guildConfiguration);

        embedBuilder.WithTitle("Message");
        embedBuilder.WithDescription(EmbedMessages.MessageFromStaff.FormatSmart(new {user, guild}));
        embedBuilder.AddField("Message", message.Content);
        embedBuilder.AddModMailNotice();

        return embedBuilder;
    }

    private async Task<StaffMessage> CreateStaffMessageAsync(DiscordUser recipient, DiscordMember staffMember, string message)
    {
        var staffMessage = new StaffMessage
        {
            StaffMemberId = staffMember.Id,
            RecipientId = recipient.Id,
            GuildId = staffMember.Guild.Id,
            Content = message,
            SentAt = DateTimeOffset.Now
        };

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        EntityEntry<StaffMessage> entry = await context.AddAsync(staffMessage).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return entry.Entity;
    }
}
