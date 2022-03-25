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
    [Command("editnote")]
    [Description("Modifies a note's content.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task EditNoteCommandAsync(CommandContext context,
        [Description("The ID of the note to modify.")]
        long noteId,
        [Description("The new content of the note.")] [RemainingText]
        string content)
    {
        await context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        MemberNote? note = await _memberNoteService.GetNoteAsync(noteId);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
            await context.RespondAsync(embed);
            return;
        }

        await _memberNoteService.EditNoteAsync(noteId, content);
        embed.WithTitle(EmbedTitles.NoteUpdated);
        embed.AddField(EmbedFieldNames.NoteID, noteId);
        embed.AddField(EmbedFieldNames.Content, content);
        embed.WithColor(0x4CAF50);

        await context.RespondAsync(embed);
    }
}
