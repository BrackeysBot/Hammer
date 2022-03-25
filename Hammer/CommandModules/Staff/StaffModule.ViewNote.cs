using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Attributes;
using BrackeysBot.Core.API.Extensions;
using DisCatSharp;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("note")]
    [Description("Views the details about a specified note.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task ViewNoteCommandAsync(CommandContext context,
        [Description("The ID of the note to retrieve.")]
        long noteId)
    {
        await context.AcknowledgeAsync();
        await context.TriggerTypingAsync();
        MemberNote? note = await _memberNoteService.GetNoteAsync(noteId);

        DiscordGuild guild = context.Guild;

        if (note?.GuildId != guild.Id)
            // cannot view notes saved for other guilds
            note = null;

        if (note?.Type == MemberNoteType.Staff && context.Member.GetPermissionLevel(guild) < PermissionLevel.Moderator)
            // guru cannot see staff notes
            note = null;

        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(false);

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchNote);
            embed.WithDescription(EmbedMessages.NoSuchNote.FormatSmart(new {id = noteId}));
        }
        else
        {
            DiscordUser? author = await context.Client.GetUserAsync(note.AuthorId);
            DiscordUser? user = await context.Client.GetUserAsync(note.UserId);
            string timestamp = Formatter.Timestamp(note.CreationTimestamp, TimestampFormat.ShortDateTime);

            embed.WithAuthor(user);
            embed.AddField(EmbedFieldNames.NoteID, note.Id, true);
            embed.AddField(EmbedFieldNames.NoteType, note.Type.ToString("G"), true);
            embed.AddField(EmbedFieldNames.Author, author.Mention, true);
            embed.AddField(EmbedFieldNames.CreationTime, timestamp, true);
            embed.AddField(EmbedFieldNames.Content, note.Content);
        }

        await context.RespondAsync(embed);
    }
}
