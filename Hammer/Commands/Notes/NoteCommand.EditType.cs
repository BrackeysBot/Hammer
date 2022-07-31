using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Data;
using X10D.DSharpPlus;

namespace Hammer.Commands.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("edittype", "Edits the type of a note.", false)]
    [SlashRequireGuild]
    public async Task EditTypeAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to edit.")] long noteId,
        [Option("type", "The new type of the note.")] MemberNoteType type)
    {
        var embed = new DiscordEmbedBuilder();

        if (!Enum.IsDefined(type))
        {
            string validTypes = string.Join(", ", Enum.GetNames<MemberNoteType>());
            embed.WithColor(0xFF0000);
            embed.WithTitle("Invalid Note Type");
            embed.WithDescription($"The specified note type {type} is invalid. " +
                                  $"Please use one of the following types: {validTypes}");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        MemberNote? note = await _noteService.GetNoteAsync(noteId).ConfigureAwait(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No Such Note");
            embed.WithDescription($"No note with the ID {noteId} could be found.");
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _noteService.EditNoteAsync(noteId, type: type).ConfigureAwait(false);
        embed.WithTitle("Note Updated");
        embed.AddField("Note ID", noteId);
        embed.AddField("Note Type", type.ToString("G"));
        embed.WithColor(0x4CAF50);
        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
