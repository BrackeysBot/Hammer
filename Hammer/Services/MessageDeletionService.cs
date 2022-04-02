﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Extensions;
using Hammer.Resources;
using NLog;
using SmartFormat;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles message deletions from staff.
/// </summary>
internal sealed class MessageDeletionService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly MessageTrackingService _messageTrackingService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageDeletionService" /> class.
    /// </summary>
    public MessageDeletionService(ICorePlugin corePlugin, DiscordClient discordClient,
        MessageTrackingService messageTrackingService)
    {
        _corePlugin = corePlugin;
        _discordClient = discordClient;
        _messageTrackingService = messageTrackingService;
    }

    /// <summary>
    ///     Deletes a specified message, logging the deletion in the staff log and optionally notifying the author.
    /// </summary>
    /// <param name="message">The message to delete.</param>
    /// <param name="staffMember">The staff member responsible for the deletion.</param>
    /// <param name="notifyAuthor">
    ///     <see langword="true" /> to notify the author of the deletion; otherwise, <see langword="false" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="message" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="NotSupportedException">The message does not belong to a guild.</exception>
    /// <exception cref="ArgumentException">
    ///     The guild in which the message appears does not match the guild of <paramref name="staffMember" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para><paramref name="staffMember" /> is not a staff member.</para>
    ///     -or-
    ///     <para><paramref name="staffMember" /> is a lower level than the author of <paramref name="message" />.</para>
    /// </exception>
    public async Task DeleteMessageAsync(DiscordMessage message, DiscordMember staffMember, bool notifyAuthor = true)
    {
        if (message is null) throw new ArgumentNullException(nameof(message));
        if (staffMember is null) throw new ArgumentNullException(nameof(staffMember));

        message = await message.NormalizeClientAsync(_discordClient);
        staffMember = await staffMember.NormalizeClientAsync(_discordClient);

        DiscordGuild guild = message.Channel.Guild;

        if (guild != staffMember.Guild)
            throw new ArgumentException(ExceptionMessages.MessageStaffMemberGuildMismatch);

        if (message.Author is not DiscordMember author)
            throw new NotSupportedException(ExceptionMessages.CannotDeleteNonGuildMessage);

        if (!staffMember.IsStaffMember(guild))
        {
            throw new InvalidOperationException(
                ExceptionMessages.NotAStaffMember.FormatSmart(new {user = staffMember, guild}));
        }

        if (author.IsHigherLevelThan(staffMember, guild))
        {
            throw new InvalidOperationException(
                ExceptionMessages.StaffIsHigherLevel.FormatSmart(new {lower = staffMember, higher = author}));
        }

        await _messageTrackingService.GetTrackedMessageAsync(message, true);
        await message.DeleteAsync();

        Logger.Info(LoggerMessages.MessageDeleted.FormatSmart(new {message, staffMember}));
        DiscordEmbed staffLogEmbed = CreateMessageDeletionToStaffLogEmbed(message, staffMember);
        await _corePlugin.LogAsync(guild, staffLogEmbed);

        if (notifyAuthor)
        {
            DiscordEmbed toAuthorEmbed = CreateMessageDeletionToAuthorEmbed(message);
            await author.SendMessageAsync(toAuthorEmbed);
        }
    }

    private static DiscordEmbed CreateMessageDeletionToAuthorEmbed(DiscordMessage message)
    {
        var formatObject = new {user = message.Author, channel = message.Channel};
        string description = EmbedMessages.MessageDeletionDescription.FormatSmart(formatObject);

        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return message.Channel.Guild.CreateDefaultEmbed()
            .WithColor(0xFF0000)
            .WithTitle(EmbedTitles.MessageDeleted)
            .WithDescription(description)
            .AddFieldIf(hasContent, EmbedFieldNames.Content, content)
            .AddFieldIf(hasAttachments, EmbedFieldNames.Attachments, attachments)
            .AddModMailNotice();
    }

    private static DiscordEmbed CreateMessageDeletionToStaffLogEmbed(DiscordMessage message, DiscordMember staffMember)
    {
        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return message.Channel.Guild.CreateDefaultEmbed()
            .WithColor(0xFF0000)
            .WithTitle(EmbedTitles.MessageDeleted)
            .WithDescription(EmbedMessages.MessageDeleted.FormatSmart(new {channel = message.Channel}))
            .AddField(EmbedFieldNames.Channel, message.Channel.Mention, true)
            .AddField(EmbedFieldNames.Author, message.Author.Mention, true)
            .AddField(EmbedFieldNames.StaffMember, staffMember.Mention, true)
            .AddField(EmbedFieldNames.MessageID, message.Id, true)
            .AddField(EmbedFieldNames.MessageTime, Formatter.Timestamp(message.CreationTimestamp, TimestampFormat.ShortDateTime),
                true)
            .AddFieldIf(hasContent, EmbedFieldNames.Content, content)
            .AddFieldIf(hasAttachments, EmbedFieldNames.Attachments, attachments);
    }
}
