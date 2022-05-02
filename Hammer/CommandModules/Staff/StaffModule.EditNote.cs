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
    [Command("editnote")]
    [Description("Modifies a note's content.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task EditNoteCommandAsync(CommandContext context,
        [Description("The ID of the note to modify.")]
        long noteId,
        [Description("The new content of the note.")] [RemainingText]
        string content)
    {
        await context.AcknowledgeAsync().ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();

        if (string.IsNullOrWhiteSpace(content))
            return;

        MemberNote? note = await _memberNoteService.GetNoteAsync(noteId).ConfigureAwait(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.RespondAsync(embed).ConfigureAwait(false);
            return;
        }

        await _memberNoteService.EditNoteAsync(noteId, content);
        embed.WithTitle(EmbedTitles.NoteUpdated);
        embed.AddField(EmbedFieldNames.NoteID, noteId);
        embed.AddField(EmbedFieldNames.Content, content);
        embed.WithColor(0x4CAF50);

        await context.RespondAsync(embed).ConfigureAwait(false);
    }
}
