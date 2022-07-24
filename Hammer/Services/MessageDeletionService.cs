using DSharpPlus;
using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using SmartFormat;
using X10D.DSharpPlus;
using ILogger = NLog.ILogger;

namespace Hammer.Services;

/// <summary>
///     Represents a service which handles message deletions from staff.
/// </summary>
internal sealed class MessageDeletionService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageDeletionService" /> class.
    /// </summary>
    public MessageDeletionService(
        IServiceScopeFactory scopeFactory,
        ConfigurationService configurationService,
        DiscordLogService logService
    )
    {
        _scopeFactory = scopeFactory;
        _configurationService = configurationService;
        _logService = logService;
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

        DiscordGuild guild = message.Channel.Guild;

        if (guild != staffMember.Guild)
            throw new ArgumentException(ExceptionMessages.MessageStaffMemberGuildMismatch);

        DiscordMember? author;
        try
        {
            author = await staffMember.Guild.GetMemberAsync(message.Author.Id).ConfigureAwait(false);
        }
        catch
        {
            author = null;
        }

        if (author is null)
            throw new NotSupportedException(ExceptionMessages.CannotDeleteNonGuildMessage);

        if (!_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration))
            return;

        if (!staffMember.IsStaffMember(guildConfiguration))
        {
            throw new InvalidOperationException(
                ExceptionMessages.NotAStaffMember.FormatSmart(new {user = staffMember, guild}));
        }

        if (author.IsHigherLevelThan(staffMember, guildConfiguration))
        {
            throw new InvalidOperationException(
                ExceptionMessages.StaffIsHigherLevel.FormatSmart(new {lower = staffMember, higher = author}));
        }

        if (message.Author.IsBot && message.Interaction is not null)
            author = await message.Channel.Guild.GetMemberAsync(message.Interaction.User.Id).ConfigureAwait(false);

        if (notifyAuthor)
        {
            DiscordEmbed toAuthorEmbed = CreateMessageDeletionToAuthorEmbed(message, guildConfiguration);
            await author.SendMessageAsync(toAuthorEmbed).ConfigureAwait(false);
        }

        DiscordEmbed staffLogEmbed = CreateMessageDeletionToStaffLogEmbed(message, staffMember, guildConfiguration);

        var deletedMessage = DeletedMessage.Create(message, staffMember);
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.AddAsync(deletedMessage).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        Logger.Info($"{message} in channel {message.Channel} was deleted by {staffMember}");
        await message.DeleteAsync($"Deleted by {staffMember.GetUsernameWithDiscriminator()}").ConfigureAwait(false);
        await _logService.LogAsync(guild, staffLogEmbed).ConfigureAwait(false);
    }

    private static DiscordEmbed CreateMessageDeletionToAuthorEmbed(DiscordMessage message, GuildConfiguration guildConfiguration)
    {
        DiscordUser author = message.Author;
        if (message.Interaction is not null)
            author = message.Interaction.User;

        var formatObject = new {user = author, channel = message.Channel};
        string description = EmbedMessages.MessageDeletionDescription.FormatSmart(formatObject);

        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return message.Channel.Guild.CreateDefaultEmbed(guildConfiguration)
            .WithColor(0xFF0000)
            .WithTitle("Message Deleted")
            .WithDescription(description)
            .AddFieldIf(hasContent, "Content", content)
            .AddFieldIf(hasAttachments, "Attachments", attachments)
            .AddModMailNotice();
    }

    private static DiscordEmbed CreateMessageDeletionToStaffLogEmbed(
        DiscordMessage message,
        DiscordMember staffMember,
        GuildConfiguration guildConfiguration
    )
    {
        bool hasContent = !string.IsNullOrWhiteSpace(message.Content);
        bool hasAttachments = message.Attachments.Count > 0;

        string? content = hasContent ? Formatter.BlockCode(Formatter.Sanitize(message.Content)) : null;
        string? attachments = hasAttachments ? string.Join('\n', message.Attachments.Select(a => a.Url)) : null;

        return message.Channel.Guild.CreateDefaultEmbed(guildConfiguration, false)
            .WithColor(0xFF0000)
            .WithTitle("Message Deleted")
            .WithDescription($"A message in {message.Channel.Mention} was deleted by a staff member.")
            .AddField("Channel", message.Channel.Mention, true)
            .AddField("Author", () =>
            {
                if (message.Author.IsBot && message.Interaction is not null)
                    return $"{message.Interaction.User.Mention} via {message.Author.Mention}";

                return message.Author.Mention;
            }, true)
            .AddField("Staff Member", staffMember.Mention, true)
            .AddField("Message ID", message.Id, true)
            .AddField("Message Time", Formatter.Timestamp(message.CreationTimestamp, TimestampFormat.ShortDateTime),
                true)
            .AddFieldIf(hasContent, "Content", content)
            .AddFieldIf(hasAttachments, "Attachments", attachments);
    }
}
