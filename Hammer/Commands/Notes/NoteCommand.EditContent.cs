using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using Hammer.Extensions;

namespace Hammer.Commands.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("editcontent", "Edits the content of a note.", false)]
    [SlashRequireGuild]
    public async Task EditContentAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to edit.")]
        long noteId,
        [Option("content", "The new content of the note.")]
        string content)
    {
        var embed = new DiscordEmbedBuilder();

        if (string.IsNullOrWhiteSpace(content))
            return;

        MemberNote? note = await _noteService.GetNoteAsync(noteId);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No Such Note");
            embed.WithDescription($"No note with the ID {noteId} could be found.");
            await context.CreateResponseAsync(embed, true);
            return;
        }

        await _noteService.EditNoteAsync(noteId, content);
        embed.WithTitle("Note Updated");
        embed.AddField("Note ID", note.Id);
        embed.AddField("Content", note.Content);
        embed.WithColor(0x4CAF50);
        await context.CreateResponseAsync(embed, true);
    }
}
