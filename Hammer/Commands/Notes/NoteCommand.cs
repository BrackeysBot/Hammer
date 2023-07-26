using DSharpPlus.SlashCommands;
using Hammer.Services;
using Microsoft.Extensions.Logging;

namespace Hammer.Commands.Notes;

/// <summary>
///     Represents a class which implements the <c>note</c> command.
/// </summary>
[SlashCommandGroup("note", "Manages member notes.", false)]
internal sealed partial class NoteCommand : ApplicationCommandModule
{
    private readonly ILogger<NoteCommand> _logger;
    private readonly ConfigurationService _configurationService;
    private readonly MemberNoteService _noteService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NoteCommand" /> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="noteService">The note service.</param>
    public NoteCommand(ILogger<NoteCommand> logger, ConfigurationService configurationService, MemberNoteService noteService)
    {
        _logger = logger;
        _configurationService = configurationService;
        _noteService = noteService;
    }
}
