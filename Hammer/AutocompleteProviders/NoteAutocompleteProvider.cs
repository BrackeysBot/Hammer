using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Hammer.Data;
using Hammer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.AutocompleteProviders;

/// <summary>
///     Provides autocomplete suggestions for notes.
/// </summary>
internal sealed class NoteAutocompleteProvider : IAutocompleteProvider
{
    /// <inheritdoc />
    public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext context)
    {
        var noteService = context.Services.GetRequiredService<MemberNoteService>();
        ConfiguredCancelableAsyncEnumerable<MemberNote> notes =
            context.Member.GetPermissionLevel(context.Guild) < PermissionLevel.Moderator
                ? noteService.GetNotesAsync(context.Guild, MemberNoteType.Guru).ConfigureAwait(false)
                : noteService.GetNotesAsync(context.Guild).ConfigureAwait(false);

        var choices = new List<DiscordAutoCompleteChoice>();

        await foreach (MemberNote note in notes)
        {
            if (choices.Count == 10) break;

            string content = note.Content;
            if (content.Length > 10)
                content = content[..10] + "...";

            string text = $"#{note.Id} (User {note.UserId}) - {content}";
            choices.Add(new DiscordAutoCompleteChoice(text, note.Id));
        }

        return choices;
    }
}
