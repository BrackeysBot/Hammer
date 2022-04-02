using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.Conversations;

/// <summary>
///     Represents a conversation between the bot and a user.
/// </summary>
internal sealed class Conversation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Conversation" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="user">The user with whom this conversation is behind held.</param>
    public Conversation(IServiceProvider serviceProvider, DiscordUser user)
    {
        Interactivity = serviceProvider.GetRequiredService<DiscordClient>().GetInteractivity();
        ServiceProvider = serviceProvider;
        User = user;
    }

    /// <summary>
    ///     Gets the ID of this conversation.
    /// </summary>
    /// <value>The conversation ID.</value>
    public Guid ConversationId { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets the service provider for this conversation.
    /// </summary>
    /// <value>The service provider.</value>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Gets the user with whom this conversation is being held.
    /// </summary>
    /// <value>The user.</value>
    public DiscordUser User { get; }

    /// <summary>
    ///     Gets the interactivity extension.
    /// </summary>
    /// <value>The interactivity extension.</value>
    public InteractivityExtension Interactivity { get; }

    /// <summary>
    ///     Initiates the conversation.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <param name="context">The command context.</param>
    /// <typeparam name="T">The conversation type.</typeparam>
    public async Task ConverseAsync(ConversationState initialState, CommandContext context)
    {
        ConversationState? currentState = initialState;
        do
        {
            currentState = await currentState.InteractAsync(context);
        } while (currentState is not null);
    }
}
