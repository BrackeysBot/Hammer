using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
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
        await context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();
        MemberNote? note = await _memberNoteService.GetNoteAsync(noteId);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.RespondAsync(embed);
            return;
        }

        await _memberNoteService.DeleteNoteAsync(note.Id);
        embed.WithTitle(EmbedTitles.NoteDeleted);
        embed.AddField(EmbedFieldNames.NoteID, note.Id);
        embed.AddField(EmbedFieldNames.Content, note.Content);
        embed.WithColor(0x4CAF50);

        await context.RespondAsync(embed);
    }
}
