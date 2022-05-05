using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Hammer.Resources;
using PermissionLevel = BrackeysBot.Core.API.PermissionLevel;

namespace Hammer.CommandModules.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("viewall", "Views all notes for a given user.", false)]
    public async Task ViewAllAsync(InteractionContext context,
        [Option("user", "The user whose notes to view.")] DiscordUser user)
    {
        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);

        try
        {
            var builder = new StringBuilder();

            // guru can only retrieve guru notes
            ConfiguredCancelableAsyncEnumerable<MemberNote> notes =
                context.Member.GetPermissionLevel(context.Guild) >= PermissionLevel.Moderator
                    ? _noteService.GetNotesAsync(user, context.Guild).ConfigureAwait(false)
                    : _noteService.GetNotesAsync(user, context.Guild, MemberNoteType.Guru).ConfigureAwait(false);

            await foreach (MemberNote note in notes)
            {
                if (note.GuildId != context.Guild.Id) continue;
                builder.AppendLine($"\u2022 [{note.Id}] {note.Content}");
            }

            if (builder.Length == 0)
                builder.AppendLine($"No notes saved for {user.Mention}");

            embed.WithAuthor(user);
            embed.WithTitle("Saved Notes");
            embed.WithDescription(builder.ToString().Trim());
            embed.AddField(EmbedFieldNames.User, user.Mention);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to retrieve notes for {user}");
            embed.WithColor(0xFF0000);
            embed.WithTitle(exception.GetType().Name);
            embed.WithDescription(exception.Message);
            embed.WithFooter("See log for more details.");
        }

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
