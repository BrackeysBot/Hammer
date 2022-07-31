namespace Hammer.Interactivity;

/// <summary>
///     Represents a state of a <see cref="Conversation" />.
/// </summary>
/// <remarks>This API is experimental, and is subject to sudden undocumented changes!</remarks>
public abstract class ConversationState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConversationState" /> class.
    /// </summary>
    protected ConversationState(Conversation conversation)
    {
        Conversation = conversation;
    }

    /// <summary>
    ///     Gets the owning <see cref="Interactivity.Conversation" />.
    /// </summary>
    /// <value>The owning <see cref="Interactivity.Conversation" /></value>
    public Conversation Conversation { get; }

    /// <summary>
    ///     Executes this state.
    /// </summary>
    /// <param name="context">The conversation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The next state, or <see langword="null" /> if the conversation is complete.</returns>
    public abstract Task<ConversationState?> InteractAsync(ConversationContext context, CancellationToken cancellationToken);
}
