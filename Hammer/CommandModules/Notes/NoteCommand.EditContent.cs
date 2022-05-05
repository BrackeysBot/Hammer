using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("editcontent", "Edits the content of a note.", false)]
    public async Task EditContentAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to edit.")] long noteId,
        [Option("content", "The new content of the note.")] string content)
    {
        var embed = new DiscordEmbedBuilder();

        if (string.IsNullOrWhiteSpace(content))
            return;

        MemberNote? note = await _noteService.GetNoteAsync(noteId).ConfigureAwait(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _noteService.EditNoteAsync(noteId, content);
        embed.WithTitle(EmbedTitles.NoteUpdated);
        embed.AddField(EmbedFieldNames.NoteID, noteId);
        embed.AddField(EmbedFieldNames.Content, content);
        embed.WithColor(0x4CAF50);
        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
