using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("view", "Views a note.", false)]
    [SlashRequireGuild]
    public async Task CreateAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to view.")] long noteId)
    {
        DiscordGuild guild = context.Guild!;
        MemberNote? note = await _noteService.GetNoteAsync(noteId).ConfigureAwait(false);
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);

        if (note?.GuildId != guild.Id)
            // cannot view notes saved for other guilds
            note = null;

        if (note?.Type == MemberNoteType.Staff && context.Member.GetPermissionLevel(guild) < PermissionLevel.Moderator)
            // non-staff cannot see staff notes
            note = null;

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        DiscordUser? author = await context.Client.GetUserAsync(note.AuthorId).ConfigureAwait(false);
        DiscordUser? user = await context.Client.GetUserAsync(note.UserId).ConfigureAwait(false);
        string timestamp = Formatter.Timestamp(note.CreationTimestamp, TimestampFormat.ShortDateTime);

        embed.WithAuthor(user);
        embed.AddField(EmbedFieldNames.NoteID, note.Id, true);
        embed.AddField(EmbedFieldNames.NoteType, note.Type.ToString("G"), true);
        embed.AddField(EmbedFieldNames.Author, author.Mention, true);
        embed.AddField(EmbedFieldNames.CreationTime, timestamp, true);
        embed.AddField(EmbedFieldNames.Content, note.Content);
        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
