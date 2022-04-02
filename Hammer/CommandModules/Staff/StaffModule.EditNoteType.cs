using System;
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
    [Command("editnotetype")]
    [Description("Modifies a note's type.")]
    [RequirePermissionLevel(PermissionLevel.Administrator)]
    public async Task EditNoteTypeCommandAsync(CommandContext context,
        [Description("The ID of the note to modify.")]
        long noteId,
        [Description("The new type of the note.")] [RemainingText]
        string newType)
    {
        await context.AcknowledgeAsync();

        var embed = new DiscordEmbedBuilder();

        if (!Enum.TryParse(newType, true, out MemberNoteType type))
        {
            string validTypes = string.Join(", ", Enum.GetNames<MemberNoteType>());
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.InvalidNoteType);
            embed.WithDescription(EmbedMessages.InvalidNoteType.FormatSmart(new {type = newType, validTypes}));
            await context.RespondAsync(embed);
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

        await _memberNoteService.EditNoteAsync(noteId, type: type);
        embed.WithTitle(EmbedTitles.NoteUpdated);
        embed.AddField(EmbedFieldNames.NoteID, noteId);
        embed.AddField(EmbedFieldNames.NoteType, type.ToString("G"));
        embed.WithColor(0x4CAF50);

        await context.RespondAsync(embed);
    }
}
