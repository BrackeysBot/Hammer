using DSharpPlus.SlashCommands;
using Hammer.Services;
using NLog;

namespace Hammer.CommandModules.Notes;

/// <summary>
///     Represents a class which implements the <c>note</c> command.
/// </summary>
[SlashCommandGroup("note", "Manages member notes.", false)]
internal sealed partial class NoteCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly MemberNoteService _noteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NoteCommand" /> class.
    /// </summary>
    /// <param name="noteService">The note service.</param>
    public NoteCommand(MemberNoteService noteService)
    {
        _noteService = noteService;
    }
}
