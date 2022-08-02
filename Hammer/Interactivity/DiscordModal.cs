using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Hammer.Interactivity;

/// <summary>
///     Represents a modal that can be displayed to the user.
/// </summary>
public sealed class DiscordModal
{
    private readonly DiscordClient _discordClient;
    private readonly string _customId = Guid.NewGuid().ToString("N");
    private readonly Dictionary<string, DiscordModalTextInput> _inputs = new();
    private TaskCompletionSource _taskCompletionSource = new();

    internal DiscordModal(string title, IEnumerable<DiscordModalTextInput> inputs, DiscordClient discordClient)
    {
        _discordClient = discordClient;
        Title = title;
        discordClient.ModalSubmitted += OnModalSubmitted;

        foreach (DiscordModalTextInput input in inputs)
            _inputs.Add(input.CustomId, input);
    }

    /// <summary>
    ///     Gets the title of this modal.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; }

    /// <summary>
    ///     Responds with this modal to the specified interaction.
    /// </summary>
    /// <param name="interaction">The interaction to which the modal will respond.</param>
    /// <param name="timeout">How long to wait </param>
    /// <exception cref="ArgumentNullException"><paramref name="interaction" /> is <see langword="null" />.</exception>
    public async Task<DiscordModalResponse> RespondToAsync(DiscordInteraction interaction, TimeSpan timeout)
    {
        if (interaction is null) throw new ArgumentNullException(nameof(interaction));

        var builder = new DiscordInteractionResponseBuilder();
        builder.WithTitle(Title);
        builder.WithCustomId(_customId);

        foreach ((_, DiscordModalTextInput input) in _inputs)
            builder.AddComponents(input.InputComponent);

        _taskCompletionSource = new TaskCompletionSource();
        await interaction.CreateResponseAsync(InteractionResponseType.Modal, builder).ConfigureAwait(false);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Token.Register(() => _taskCompletionSource.TrySetCanceled());
        if (timeout != Timeout.InfiniteTimeSpan)
            cancellationTokenSource.CancelAfter(timeout);

        try
        {
            await _taskCompletionSource.Task.ConfigureAwait(false);
            return DiscordModalResponse.Success;
        }
        catch (TaskCanceledException)
        {
            return DiscordModalResponse.Timeout;
        }
    }

    private Task OnModalSubmitted(DiscordClient sender, ModalSubmitEventArgs e)
    {
        if (e.Interaction.Data.CustomId != _customId)
            return Task.CompletedTask;

        _discordClient.ModalSubmitted -= OnModalSubmitted;
        e.Handled = true;

        IEnumerable<DiscordComponent> components = e.Interaction.Data.Components.SelectMany(c => c.Components);
        IEnumerable<TextInputComponent> inputComponents = components.OfType<TextInputComponent>();
        foreach (TextInputComponent inputComponent in inputComponents)
        {
            if (_inputs.TryGetValue(inputComponent.CustomId, out DiscordModalTextInput? input))
                input.Value = inputComponent.Value;
        }

        _taskCompletionSource.TrySetResult();
        return e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource);
    }
}
