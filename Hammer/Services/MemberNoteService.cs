using DSharpPlus.Entities;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using SmartFormat;
using PermissionLevel = Hammer.Data.PermissionLevel;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages staff/guru-written notes on users.
/// </summary>
internal sealed class MemberNoteService : BackgroundService
{
    private readonly IDbContextFactory<HammerContext> _dbContextFactory;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _logService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberNoteService" /> class.
    /// </summary>
    public MemberNoteService(
        IDbContextFactory<HammerContext> dbContextFactory,
        ConfigurationService configurationService,
        DiscordLogService logService
    )
    {
        _dbContextFactory = dbContextFactory;
        _configurationService = configurationService;
        _logService = logService;
    }

    /// <summary>
    ///     Creates a note on a user.
    /// </summary>
    /// <param name="user">The user to whom this note shall be saved.</param>
    /// <param name="author">The member who created the note.</param>
    /// <param name="content">The content of the note.</param>
    /// <returns>The newly-created note.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="author" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <paramref name="author" /> is not a <see cref="PermissionLevel.Guru" />.
    /// </exception>
    public async Task<MemberNote> CreateNoteAsync(DiscordUser user, DiscordMember author, string content)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(author);

        string? trimmedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent)) throw new ArgumentNullException(nameof(content));

        if (!_configurationService.TryGetGuildConfiguration(author.Guild, out GuildConfiguration? guildConfiguration))
            throw new InvalidOperationException("No guild configuration found for the guild.");

        DiscordGuild guild = author.Guild;
        PermissionLevel permissionLevel = author.GetPermissionLevel(guildConfiguration);

        var noteType = MemberNoteType.Staff;
        if (permissionLevel < PermissionLevel.Moderator)
        {
            if (permissionLevel < PermissionLevel.Guru)
                throw new InvalidOperationException();

            noteType = MemberNoteType.Guru;
        }

        var note = new MemberNote(noteType, user, author, guild, trimmedContent);

        await using (HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false))
        {
            EntityEntry<MemberNote> result = await context.AddAsync(note).ConfigureAwait(false);
            note = result.Entity;

            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);
        embed.WithTitle("Note Created");
        embed.AddField("Note ID", note.Id, true);
        embed.AddField("Note Type", note.Type.ToString("G"), true);
        embed.AddField("User", user.Mention, true);
        embed.AddField("Author", author.Mention, true);
        embed.AddField("Content", note.Content);

        await _logService.LogAsync(guild, embed);
        return note;
    }

    /// <summary>
    ///     Deletes the note with a specified ID.
    /// </summary>
    /// <param name="id">The ID of the note to delete.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> refers to a non-existing note.</exception>
    public async Task DeleteNoteAsync(long id)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        MemberNote? note = await context.MemberNotes.FirstOrDefaultAsync(n => n.Id == id).ConfigureAwait(false);
        if (note is null)
            throw new ArgumentException(ExceptionMessages.NoSuchNote.FormatSmart(new { id }), nameof(id));

        context.Remove(note);
        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     Modifies a note's content and/or type.
    /// </summary>
    /// <param name="id">The ID of the note to modify.</param>
    /// <param name="content">The new content of the note.</param>
    /// <param name="type">The new type of the note.</param>
    /// <exception cref="ArgumentException"><paramref name="id" /> refers to a non-existing note.</exception>
    public async Task EditNoteAsync(long id, string? content = null, MemberNoteType? type = null)
    {
        if (string.IsNullOrWhiteSpace(content) && type is null)
            return;

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        MemberNote? note = await context.MemberNotes.FirstOrDefaultAsync(n => n.Id == id).ConfigureAwait(false);
        if (note is null)
            throw new ArgumentException(ExceptionMessages.NoSuchNote.FormatSmart(new { id }), nameof(id));

        if (!string.IsNullOrWhiteSpace(content))
            note.Content = content;

        if (type is not null)
            note.Type = type.Value;

        context.Update(note);
        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     Finds a note by its ID.
    /// </summary>
    /// <param name="id">The ID of the note to retrieve.</param>
    /// <returns>
    ///     The <see cref="MemberNote" /> whose <see cref="MemberNote.Id" /> matches <paramref name="id" />, or
    ///     <see langword="null" /> if no match was found.
    /// </returns>
    public async Task<MemberNote?> GetNoteAsync(long id)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MemberNotes.FirstOrDefaultAsync(n => n.Id == id).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the count of notes saved for a user.
    /// </summary>
    /// <param name="user">The user whose notes to count.</param>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <returns>The count of notes saved for <paramref name="user" /> in <paramref name="guild" />.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public async Task<int> GetNoteCountAsync(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MemberNotes.CountAsync(n => n.UserId == user.Id && n.GuildId == guild.Id).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns the count of notes saved for a user whose type is a specified value..
    /// </summary>
    /// <param name="user">The user whose notes to count.</param>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <param name="type">The type of notes by which to filter.</param>
    /// <returns>
    ///     The count of notes saved for <paramref name="user" /> in <paramref name="guild" /> with the type
    ///     <paramref name="type" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="type" /> is not a value defined in <see cref="MemberNoteType" />.
    /// </exception>
    public async Task<int> GetNoteCountAsync(DiscordUser user, DiscordGuild guild, MemberNoteType type)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        return await context.MemberNotes.CountAsync(n => n.UserId == user.Id && n.GuildId == guild.Id && n.Type == type)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns an enumerable collection of the notes in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <returns>
    ///     An enumerable collection of <see cref="MemberNote" /> values representing the notes stored in
    ///     <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public async IAsyncEnumerable<MemberNote> GetNotesAsync(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (MemberNote note in context.MemberNotes.Where(n => n.GuildId == guild.Id))
            yield return note;
    }

    /// <summary>
    ///     Returns an enumerable collection of the notes in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <param name="type">The type of notes by which to filter.</param>
    /// <returns>
    ///     An enumerable collection of <see cref="MemberNote" /> values representing the notes stored in
    ///     <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="type" /> is not a value defined in <see cref="MemberNoteType" />.
    /// </exception>
    public async IAsyncEnumerable<MemberNote> GetNotesAsync(DiscordGuild guild, MemberNoteType type)
    {
        ArgumentNullException.ThrowIfNull(guild);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (MemberNote note in context.MemberNotes.Where(n => n.GuildId == guild.Id && n.Type == type))
            yield return note;
    }

    /// <summary>
    ///     Returns an enumerable collection of the notes for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose notes to retrieve.</param>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <returns>
    ///     An enumerable collection of <see cref="MemberNote" /> values representing the notes stored for
    ///     <paramref name="user" /> in <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    public async IAsyncEnumerable<MemberNote> GetNotesAsync(DiscordUser user, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (MemberNote note in context.MemberNotes.Where(n => n.UserId == user.Id && n.GuildId == guild.Id))
            yield return note;
    }

    /// <summary>
    ///     Returns an enumerable collection of the notes of a specified type for a user in the specified guild.
    /// </summary>
    /// <param name="user">The user whose notes to retrieve.</param>
    /// <param name="guild">The guild whose notes to search.</param>
    /// <param name="type">The type of notes by which to filter.</param>
    /// <returns>
    ///     An enumerable collection of <see cref="MemberNote" /> values representing the notes stored for
    ///     <paramref name="user" /> in <paramref name="guild" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="guild" /> is <see langword="null" />.</para>
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="type" /> is not a value defined in <see cref="MemberNoteType" />.
    /// </exception>
    public async IAsyncEnumerable<MemberNote> GetNotesAsync(DiscordUser user, DiscordGuild guild, MemberNoteType type)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(guild);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);

        foreach (MemberNote note in
                 context.MemberNotes.Where(n => n.UserId == user.Id && n.GuildId == guild.Id && n.Type == type))
            yield return note;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using HammerContext context = await _dbContextFactory.CreateDbContextAsync(stoppingToken).ConfigureAwait(false);
        await context.Database.EnsureCreatedAsync(stoppingToken).ConfigureAwait(false);
    }
}
