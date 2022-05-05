using System;
using System.Threading.Tasks;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;

namespace Hammer.CommandModules.Notes;

internal sealed partial class NoteCommand
{
    [SlashCommand("create", "Creates a new note", false)]
    public async Task CreateAsync(InteractionContext context,
        [Option("user", "The user for whom to create a note.")] DiscordUser user,
        [Option("content", "The content of the note.")] string content)
    {
        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);

        try
        {
            MemberNote note = await _noteService.CreateNoteAsync(user, context.Member, content).ConfigureAwait(false);

            embed.WithColor(0x4CAF50);
            embed.WithTitle("Note Created");
            embed.WithDescription($"Successfully created note for {user.Mention}");
            embed.WithFooter($"Note #{note.Id}");

            Logger.Info($"{context.User} created note for {user} in guild {context.Guild}: {content}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to save a note for {user} (\"{content}\")");

            embed.WithColor(0xFF0000);
            embed.WithTitle(exception.GetType().Name);
            embed.WithDescription(exception.Message);
            embed.WithFooter("See log for more details.");
        }

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
