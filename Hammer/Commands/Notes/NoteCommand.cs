using DSharpPlus.SlashCommands;
using Hammer.Services;
using NLog;
using ILogger = NLog.ILogger;

namespace Hammer.Commands.Notes;

/// <summary>
///     Represents a class which implements the <c>note</c> command.
/// </summary>
[SlashCommandGroup("note", "Manages member notes.", false)]
internal sealed partial class NoteCommand : ApplicationCommandModule
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private readonly ConfigurationService _configurationService;
    private readonly MemberNoteService _noteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NoteCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="noteService">The note service.</param>
    public NoteCommand(ConfigurationService configurationService, MemberNoteService noteService)
    {
        _configurationService = configurationService;
        _noteService = noteService;
    }
}
