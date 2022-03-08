using System.Threading.Tasks;
using DisCatSharp.CommandsNext;

namespace Hammer.Conversations;

/// <summary>
///     Represents a state for a <see cref="Conversation" /> to be in.
/// </summary>
internal abstract class ConversationState
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConversationState" /> class.
    /// </summary>
    /// <param name="owningConversation">The owning conversation.</param>
    protected ConversationState(Conversation owningConversation)
    {
        OwningConversation = owningConversation;
    }

    /// <summary>
    ///     Gets the owning conversation.
    /// </summary>
    /// <value>The owning conversation.</value>
    public Conversation OwningConversation { get; }

    /// <summary>
    ///     Executes this state.
    /// </summary>
    /// <param name="context">The initial command context.</param>
    /// <returns>The next state, or <see langword="null" /> if the conversation is complete.</returns>
    public abstract Task<ConversationState?> InteractAsync(CommandContext context);
}
