namespace Hammer.Interactivity;

/// <summary>
///     Represents a conversation that can be held between a bot and a user.
/// </summary>
/// <remarks>This API is experimental, and is subject to sudden undocumented changes!</remarks>
public sealed class Conversation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Conversation" /> class.
    /// </summary>
    /// <param name="services">The service provider.</param>
    public Conversation(IServiceProvider services)
    {
        Services = services;
    }

    /// <summary>
    ///     Gets or sets the cancellation token source.
    /// </summary>
    /// <value>The cancellation token source.</value>
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    /// <summary>
    ///     Gets the unique ID of this conversation.
    /// </summary>
    /// <value>The unique ID of this conversation.</value>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or initializes the service provider for this conversation.
    /// </summary>
    /// <value>The service provider.</value>
    public IServiceProvider Services { get; init; }

    /// <summary>
    ///     Initiates the conversation.
    /// </summary>
    /// <param name="initialState">The initial state.</param>
    /// <param name="context">The command context.</param>
    public async Task ConverseAsync(ConversationState initialState, ConversationContext context)
    {
        ConversationState? currentState = initialState;
        do
        {
            currentState = await currentState.InteractAsync(context, context.CancellationTokenSource.Token);
        } while (currentState is not null);
    }
}
