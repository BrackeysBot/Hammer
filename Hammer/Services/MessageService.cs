using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which logs and relays messages from staff members.
/// </summary>
internal sealed class MessageService
{
    private readonly ILogger<MessageService> _logger;
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly DiscordClient _discordClient;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageService" /> class.
    /// </summary>
    public MessageService(
        ILogger<MessageService> logger,
        IDbContextFactory<HammerContext> dbContextFactory,
        DiscordClient discordClient,
        ConfigurationService configurationService,
        DiscordLogService logService
    )
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _discordClient = discordClient;
        _configurationService = configurationService;
        _logService = logService;
    }

    /// <summary>
    ///     Returns a staff message by its ID.
    /// </summary>
    /// <param name="id">The ID of the message to retrieve.</param>
    /// <returns>A <see cref="StaffMessage" />, or <see langword="null" /> if no such message was found.</returns>
    public async Task<StaffMessage?> GetStaffMessage(long id)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync();
        return await context.StaffMessages.FirstOrDefaultAsync(m => m.Id == id);
    }

    /// <summary>
    ///     Returns an enumerable collection of staff messages sent to the specified user.
    /// </summary>
    /// <param name="recipient">The recipient of the messages.</param>
    /// <param name="guild">The guild.</param>
    /// <returns>An asynchronously enumerable collection of <see cref="StaffMessage" /> values.</returns>
    public async IAsyncEnumerable<StaffMessage> GetStaffMessages(DiscordUser recipient, DiscordGuild guild)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync();

        foreach (StaffMessage staffMessage in
                 context.StaffMessages.Where(m => m.RecipientId == recipient.Id && m.GuildId == guild.Id)
                     .AsEnumerable()
                     .OrderBy(m => m.SentAt))
        {
            yield return staffMessage;
        }
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
    public async Task<bool> MessageMemberAsync(DiscordMember recipient, DiscordMember staffMember, string message)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(staffMember);
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be empty", nameof(message));

        message = message.Trim();

        if (recipient.Guild != staffMember.Guild)
            throw new ArgumentException(ExceptionMessages.StaffMemberRecipientGuildMismatch, nameof(recipient));

        StaffMessage staffMessage = await CreateStaffMessageAsync(recipient, staffMember, message);

        _logger.LogInformation("{StaffMember} sent a message to {Recipient} from {Guild}. Contents: {Message}",
            staffMember, recipient, staffMember.Guild, message);

        DiscordEmbed embed = await CreateUserEmbedAsync(staffMessage);

        try
        {
            await recipient.SendMessageAsync(embed);
        }
        catch (UnauthorizedException)
        {
            return false;
        }

        embed = await CreateStaffLogEmbedAsync(staffMessage);
        await _logService.LogAsync(recipient.Guild, embed);
        return true;
    }

    private async Task<DiscordEmbed> CreateStaffLogEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId);
        DiscordUser staffMember = await _discordClient.GetUserAsync(message.StaffMemberId);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed(guildConfiguration, false);

        embedBuilder.WithAuthor($"Message #{message.Id}");
        embedBuilder.WithTitle("Message Sent");
        embedBuilder.WithDescription($"Staff member {staffMember.Mention} sent a message to {user.Mention}.");
        embedBuilder.AddField("Message", message.Content);

        return embedBuilder;
    }

    private async Task<DiscordEmbed> CreateUserEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException(ExceptionMessages.NoConfigurationForGuild);

        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed(guildConfiguration);

        embedBuilder.WithTitle("Message");
        embedBuilder.WithDescription(EmbedMessages.MessageFromStaff.FormatSmart(new { user, guild }));
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

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync();
        EntityEntry<StaffMessage> entry = await context.AddAsync(staffMessage);
        await context.SaveChangesAsync();

        return entry.Entity;
    }
}
