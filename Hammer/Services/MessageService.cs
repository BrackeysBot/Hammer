using System;
using System.Threading.Tasks;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which logs and relays messages from staff members.
/// </summary>
internal sealed class MessageService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageService" /> class.
    /// </summary>
    public MessageService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
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

        StaffMessage staffMessage = await CreateStaffMessageAsync(recipient, staffMember, message);

        Logger.Info(LoggerMessages.StaffMessagedMember.FormatSmart(new
            {staffMember, recipient, guild = staffMember.Guild, message}));
        await recipient.SendMessageAsync(await CreateUserEmbedAsync(staffMessage));
        await _corePlugin.LogAsync(recipient.Guild, await CreateStaffLogEmbedAsync(staffMessage));
    }

    private async Task<DiscordEmbed> CreateStaffLogEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId);
        DiscordUser staffMember = await _discordClient.GetUserAsync(message.RecipientId);
        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed();

        embedBuilder.WithAuthor($"Message #{message.Id}");
        embedBuilder.WithTitle(EmbedTitles.MessageSent);
        embedBuilder.WithDescription(EmbedMessages.StaffSentMessage.FormatSmart(new {staffMember, user}));
        embedBuilder.AddField(EmbedFieldNames.Message, message.Content);

        return embedBuilder;
    }

    private async Task<DiscordEmbed> CreateUserEmbedAsync(StaffMessage message)
    {
        DiscordGuild guild = await _discordClient.GetGuildAsync(message.GuildId);
        DiscordUser user = await _discordClient.GetUserAsync(message.RecipientId);
        DiscordEmbedBuilder embedBuilder = guild.CreateDefaultEmbed();

        embedBuilder.WithTitle(EmbedTitles.Message);
        embedBuilder.WithDescription(EmbedMessages.MessageFromStaff.FormatSmart(new {user, guild}));
        embedBuilder.AddField(EmbedFieldNames.Message, message.Content);
        embedBuilder.AddModMailNotice();

        return embedBuilder;
    }

    private async Task<StaffMessage> CreateStaffMessageAsync(DiscordUser recipient, DiscordMember staffMember, string message)
    {
        var staffMessage = new StaffMessage
        {
            Content = message,
            GuildId = staffMember.Guild.Id,
            StaffMemberId = staffMember.Id,
            RecipientId = recipient.Id
        };

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        EntityEntry<StaffMessage> entry = await context.AddAsync(staffMessage);
        await context.SaveChangesAsync();

        return entry.Entity;
    }
}
