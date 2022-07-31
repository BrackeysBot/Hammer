using System.Runtime.CompilerServices;
using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Configuration;
using Hammer.Data;
using Hammer.Extensions;
using X10D.DSharpPlus;

namespace Hammer.Commands.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("viewall", "Views all notes for a given user.", false)]
    [SlashRequireGuild]
    public async Task ViewAllAsync(InteractionContext context,
        [Option("user", "The user whose notes to view.")] DiscordUser user)
    {
        if (!_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            await context.CreateResponseAsync("This guild is not configured.", true).ConfigureAwait(false);
            return;
        }

        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(guildConfiguration, false);

        try
        {
            var builder = new StringBuilder();

            // guru can only retrieve guru notes
            ConfiguredCancelableAsyncEnumerable<MemberNote> notes =
                context.Member.GetPermissionLevel(guildConfiguration) >= PermissionLevel.Moderator
                    ? _noteService.GetNotesAsync(user, context.Guild).ConfigureAwait(false)
                    : _noteService.GetNotesAsync(user, context.Guild, MemberNoteType.Guru).ConfigureAwait(false);

            await foreach (MemberNote note in notes)
            {
                if (note.GuildId != context.Guild.Id) continue;
                builder.AppendLine($"\u2022 [{note.Id}] [{note.Type:G}] {note.Content}");
            }

            if (builder.Length == 0)
                builder.AppendLine($"No notes saved for {user.Mention}");

            embed.WithAuthor(user);
            embed.WithTitle("Saved Notes");
            embed.WithDescription(builder.ToString().Trim());
            embed.AddField("User", user.Mention);
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
