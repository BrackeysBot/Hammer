using System;
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
    [SlashCommand("edittype", "Edits the type of a note.", false)]
    public async Task EditTypeAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to edit.")] long noteId,
        [Option("type", "The new type of the note.")] MemberNoteType type)
    {
        var embed = new DiscordEmbedBuilder();

        if (!Enum.IsDefined(type))
        {
            string validTypes = string.Join(", ", Enum.GetNames<MemberNoteType>());
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.InvalidNoteType);
            embed.WithDescription(EmbedMessages.InvalidNoteType.FormatSmart(new {type, validTypes}));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        MemberNote? note = await _noteService.GetNoteAsync(noteId).ConfigureAwait(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _noteService.EditNoteAsync(noteId, type: type).ConfigureAwait(false);
        embed.WithTitle(EmbedTitles.NoteUpdated);
        embed.AddField(EmbedFieldNames.NoteID, noteId);
        embed.AddField(EmbedFieldNames.NoteType, type.ToString("G"));
        embed.WithColor(0x4CAF50);
        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
