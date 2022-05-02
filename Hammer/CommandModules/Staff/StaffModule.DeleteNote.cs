using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("deletenote")]
    [Aliases("delnote")]
    [Description("Deletes a note.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task DeleteNoteCommandAsync(CommandContext context,
        [Description("The ID of the note to delete.")]
        long noteId)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        MemberNote? note = await _memberNoteService.GetNoteAsync(noteId).ConfigureAwait(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.RespondAsync(embed).ConfigureAwait(false);
            return;
        }

        await _memberNoteService.DeleteNoteAsync(note.Id).ConfigureAwait(false);
        embed.WithTitle(EmbedTitles.NoteDeleted);
        embed.AddField(EmbedFieldNames.NoteID, note.Id);
        embed.AddField(EmbedFieldNames.Content, note.Content);
        embed.WithColor(0x4CAF50);

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
