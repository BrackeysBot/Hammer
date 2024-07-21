using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.AutocompleteProviders;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using PermissionLevel = Hammer.Data.PermissionLevel;

namespace Hammer.Commands.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("view", "Views a note.", false)]
    [SlashRequireGuild]
    public async Task ViewAsync(InteractionContext context,
        [Autocomplete(typeof(NoteAutocompleteProvider))] [Option("note", "The note to view.")]
        long noteId)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true);
            return;
        }

        DiscordGuild guild = context.Guild!;
        MemberNote? note = await _noteService.GetNoteAsync(noteId);
        DiscordEmbedBuilder embed = guild.CreateDefaultEmbed(guildConfiguration, false);

        if (note?.GuildId != guild.Id)
            // cannot view notes saved for other guilds
            note = null;

        if (note?.Type == MemberNoteType.Staff &&
            context.Member.GetPermissionLevel(guildConfiguration) < PermissionLevel.Moderator)
            // non-staff cannot see staff notes
            note = null;

        if (note is null)
        {
            embed.WithColor(0xFF0000);
            embed.WithTitle("No Such Note");
            embed.WithDescription($"No note with the ID {noteId} could be found.");
            await context.CreateResponseAsync(embed, true);
            return;
        }

        DiscordUser? author = await context.Client.GetUserAsync(note.AuthorId);
        DiscordUser? user = await context.Client.GetUserAsync(note.UserId);
        string timestamp = Formatter.Timestamp(note.CreationTimestamp, TimestampFormat.ShortDateTime);

        embed.WithAuthor(user);
        embed.AddField("Note ID", note.Id, true);
        embed.AddField("Note Type", note.Type.ToString("G"), true);
        embed.AddField("Author", author.Mention, true);
        embed.AddField("Creation Time", timestamp, true);
        embed.AddField("Content", note.Content);
        await context.CreateResponseAsync(embed, true);
    }
}
