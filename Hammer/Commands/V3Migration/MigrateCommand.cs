using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Hammer.Interactivity;

namespace Hammer.Commands.V3Migration;

/// <summary>
///     Represents a class which implements the <c>migrate</c> command.
/// </summary>
internal sealed class MigrateCommand : ApplicationCommandModule
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MigrateCommand" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public MigrateCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [SlashCommand("migrate", "Migrates a v3 infraction database to a v4 infraction database.", false)]
    [SlashRequireGuild]
    public async Task MigrateAsync(
        InteractionContext context,
        [Option("fullMigration", "Whether or not to do a full migration.")] bool fullMigration = true
    )
    {
        await context.DeferAsync().ConfigureAwait(false);

        var conversation = new Conversation(_serviceProvider);
        var state = new MigrationWelcomeState(fullMigration, conversation);

        await conversation.ConverseAsync(state, ConversationContext.FromInteractionContext(context)).ConfigureAwait(false);
    }
}
