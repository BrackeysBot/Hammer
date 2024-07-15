using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;

namespace Hammer.Interactivity;

/// <summary>
///     Provides shared state for a <see cref="Conversation" />.
/// </summary>
/// <remarks>This API is experimental, and is subject to sudden undocumented changes!</remarks>
public sealed class ConversationContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConversationContext" /> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="user">The user who initiated the conversation.</param>
    /// <param name="channel">The channel in which the conversation is taking place.</param>
    /// <exception cref="ArgumentNullException">
    ///     <para><paramref name="serviceProvider" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="user" /> is <see langword="null" />.</para>
    ///     -or-
    ///     <para><paramref name="channel" /> is <see langword="null" />.</para>
    /// </exception>
    public ConversationContext(IServiceProvider serviceProvider, DiscordUser user, DiscordChannel channel)
    {
        Services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Client = serviceProvider.GetRequiredService<DiscordClient>();
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        User = user ?? throw new ArgumentNullException(nameof(user));
        Member = user as DiscordMember;
    }

    /// <summary>
    ///     Gets the cancellation token source for this conversation.
    /// </summary>
    /// <value>The cancellation token source.</value>
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    ///     Gets the underlying <see cref="DiscordClient" /> owning the conversation.
    /// </summary>
    /// <value>The <see cref="DiscordClient" />.</value>
    public DiscordClient Client { get; }

    /// <summary>
    ///     Gets the channel in which the conversation is being held.
    /// </summary>
    /// <value>The channel.</value>
    public DiscordChannel Channel { get; }

    /// <summary>
    ///     Gets the guild in which the conversation is being held, if any.
    /// </summary>
    public DiscordGuild? Guild => Channel.Guild;

    /// <summary>
    ///     Gets the interaction which triggered this conversation, if any.
    /// </summary>
    /// <value>
    ///     The interaction, or <see langword="null" /> if this conversation was not initiated by an application command.
    /// </value>
    public DiscordInteraction? Interaction { get; private init; }

    /// <summary>
    ///     Gets the member who initiated the conversation, if this conversation is in a guild.
    /// </summary>
    /// <value>The member.</value>
    public DiscordMember? Member { get; }

    /// <summary>
    ///     Gets the message, sent by the user, that initiated the conversation, if any.
    /// </summary>
    /// <value>The original message, or <see langword="null" /> if no message is associated with this conversation.</value>
    /// <remarks>
    ///     This property will be the result of <see cref="CommandContext.Message" /> if this context was constructed via
    ///     <see cref="FromCommandContext" />, or the result of the first entry in
    ///     <see cref="DiscordInteractionResolvedCollection.Messages" /> if this context was constructed via
    ///     <see cref="FromInteractionContext" /> or <see cref="FromContextMenuContext" /> - which may be <see langword="null" />.
    /// </remarks>
    public DiscordMessage? Message { get; private init; }

    /// <summary>
    ///     Gets the service provider for the conversation.
    /// </summary>
    /// <value>The service provider.</value>
    public IServiceProvider Services { get; }

    /// <summary>
    ///     Gets the user who initiated the conversation.
    /// </summary>
    /// <value>The user.</value>
    public DiscordUser User { get; }

    /// <summary>
    ///     Constructs a new <see cref="ConversationContext" /> from a specified <see cref="ContextMenuContext" />.
    /// </summary>
    /// <param name="context">The <see cref="ContextMenuContext" /> from which the values should be pulled.</param>
    /// <returns>A new instance of <see cref="ConversationContext" />.</returns>
    public static ConversationContext FromContextMenuContext(ContextMenuContext context)
    {
        return new ConversationContext(context.Services, context.Member ?? context.User, context.Channel)
        {
            Interaction = context.Interaction,
            Message = context.Interaction?.Data?.Resolved?.Messages?.FirstOrDefault().Value
        };
    }

    /// <summary>
    ///     Constructs a new <see cref="ConversationContext" /> from a specified <see cref="InteractionContext" />.
    /// </summary>
    /// <param name="context">The <see cref="InteractionContext" /> from which the values should be pulled.</param>
    /// <returns>A new instance of <see cref="ConversationContext" />.</returns>
    public static ConversationContext FromInteractionContext(InteractionContext context)
    {
        return new ConversationContext(context.Services, context.Member ?? context.User, context.Channel)
        {
            Interaction = context.Interaction,
            Message = context.Interaction?.Data?.Resolved?.Messages?.FirstOrDefault().Value
        };
    }


    /// <summary>
    ///     Responds to the original message with the specified content. If <see cref="Message" /> is <see langword="null" />, a
    ///     new message is sent in <see cref="Channel" />; otherwise, a message as sent with a reply to <see cref="Message" />.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <returns>The message response.</returns>
    public Task<DiscordMessage> RespondAsync(string content)
    {
        var builder = new DiscordMessageBuilder();
        builder.WithContent(content);
        return RespondAsync(builder);
    }


    /// <summary>
    ///     Responds to the original message with the specified content and embed. If <see cref="Message" /> is
    ///     <see langword="null" />, a new message is sent in <see cref="Channel" />; otherwise, a message as sent with a reply to
    ///     <see cref="Message" />.
    /// </summary>
    /// <param name="content">The message content.</param>
    /// <param name="embed">The embed to attach.</param>
    /// <returns>The message response.</returns>
    public Task<DiscordMessage> RespondAsync(string content, DiscordEmbed embed)
    {
        var builder = new DiscordMessageBuilder();
        builder.WithContent(content);
        builder.WithEmbed(embed);
        return RespondAsync(builder);
    }

    /// <summary>
    ///     Responds to the original message with the specified embed. If <see cref="Message" /> is <see langword="null" />, a
    ///     new message is sent in <see cref="Channel" />; otherwise, a message as sent with a reply to <see cref="Message" />.
    /// </summary>
    /// <param name="embed">The embed to attach.</param>
    /// <returns>The message response.</returns>
    public Task<DiscordMessage> RespondAsync(DiscordEmbed embed)
    {
        var builder = new DiscordMessageBuilder();
        builder.WithEmbed(embed);
        return RespondAsync(builder);
    }

    /// <summary>
    ///     Responds to the original message with the specified message. If <see cref="Message" /> is <see langword="null" />, a
    ///     new message is sent in <see cref="Channel" />; otherwise, a message as sent with a reply to <see cref="Message" />.
    /// </summary>
    /// <param name="builder">The Discord message builder.</param>
    /// <returns>The message response.</returns>
    public Task<DiscordMessage> RespondAsync(Action<DiscordMessageBuilder> builder)
    {
        return Message is null ? Channel.SendMessageAsync(builder) : Message.RespondAsync(builder);
    }

    /// <summary>
    ///     Responds to the original message with the specified message. If <see cref="Message" /> is <see langword="null" />, a
    ///     new message is sent in <see cref="Channel" />; otherwise, a message as sent with a reply to <see cref="Message" />.
    /// </summary>
    /// <param name="builder">The Discord message builder.</param>
    /// <returns>The message response.</returns>
    public Task<DiscordMessage> RespondAsync(DiscordMessageBuilder builder)
    {
        return Message is null ? Channel.SendMessageAsync(builder) : Message.RespondAsync(builder);
    }
}
