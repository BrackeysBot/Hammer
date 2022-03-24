using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.Services;

/// <summary>
///     Represents a service which manages staff/guru-written notes on users.
/// </summary>
internal sealed class MemberNoteService : BackgroundService
{
    private readonly ICorePlugin _corePlugin;
    private readonly DiscordClient _discordClient;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MemberNoteService" /> class.
    /// </summary>
    public MemberNoteService(IServiceScopeFactory scopeFactory, ICorePlugin corePlugin, DiscordClient discordClient)
    {
        _scopeFactory = scopeFactory;
        _corePlugin = corePlugin;
        _discordClient = discordClient;
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
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<MemberNote> CreateNoteAsync(DiscordUser user, DiscordMember author, string content)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (author is null) throw new ArgumentNullException(nameof(author));

        string? trimmedContent = content?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedContent)) throw new ArgumentNullException(nameof(content));

        user = await user.NormalizeClientAsync(_discordClient);
        author = await author.NormalizeClientAsync(_discordClient);

        DiscordGuild guild = author.Guild;
        PermissionLevel permissionLevel = author.GetPermissionLevel(guild);

        var noteType = MemberNoteType.Staff;
        if (permissionLevel < PermissionLevel.Moderator)
        {
            if (permissionLevel < PermissionLevel.Guru)
                throw new InvalidOperationException();

            noteType = MemberNoteType.Guru;
        }

        var note = new MemberNote(noteType, user, author, guild, trimmedContent);
        await using (AsyncServiceScope scope = _scopeFactory.CreateAsyncScope())
        {
            await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

            EntityEntry<MemberNote> result = await context.AddAsync(note);
            note = result.Entity;

            await context.SaveChangesAsync();
        }

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);
        embed.WithTitle("Note Created");
        embed.AddField(EmbedFieldNames.NoteID, note.Id, true);
        embed.AddField(EmbedFieldNames.User, user.Mention, true);
        embed.AddField(EmbedFieldNames.Author, author.Mention, true);
        embed.AddField(EmbedFieldNames.Content, note.Content);

        await _corePlugin.LogAsync(guild, embed);
        return note;
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
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        return await context.MemberNotes.FirstOrDefaultAsync(n => n.Id == id);
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
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        return await context.MemberNotes.CountAsync(n => n.UserId == user.Id && n.GuildId == guild.Id);
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
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        return await context.MemberNotes.CountAsync(n => n.UserId == user.Id && n.GuildId == guild.Id && n.Type == type);
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
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

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
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (guild is null) throw new ArgumentNullException(nameof(guild));
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();

        foreach (MemberNote note in
                 context.MemberNotes.Where(n => n.UserId == user.Id && n.GuildId == guild.Id && n.Type == type))
            yield return note;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<HammerContext>();
        await context.Database.EnsureCreatedAsync(stoppingToken);
    }
}
