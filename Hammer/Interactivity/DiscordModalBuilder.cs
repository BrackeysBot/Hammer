using DSharpPlus;
using DSharpPlus.Entities;

namespace Hammer.Interactivity;

/// <summary>
///     Represents a class which can construct a <see cref="DiscordModal" />.
/// </summary>
public sealed class DiscordModalBuilder
{
    private readonly DiscordClient _discordClient;
    private readonly List<DiscordModalTextInput> _inputs = new();
    private string _title = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DiscordModalBuilder" /> class.
    /// </summary>
    /// <param name="discordClient">The discord client.</param>
    public DiscordModalBuilder(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Gets or sets the title of this modal.
    /// </summary>
    /// <value>The title.</value>
    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

            _title = value;
        }
    }

    /// <summary>
    ///     Adds a new text input component to this modal.
    /// </summary>
    /// <param name="label">The label of the input.</param>
    /// <param name="placeholder">The placeholder text.</param>
    /// <param name="initialValue">The initial value of the input.</param>
    /// <param name="isRequired">
    ///     <see langword="true" /> if this input requires a value; otherwise, <see langword="false" />.
    /// </param>
    /// <param name="inputStyle">The input style.</param>
    /// <param name="minLength">The minimum length of the input.</param>
    /// <param name="maxLength">The maximum length of the input.</param>
    /// <returns>The <see cref="DiscordModalTextInput" /> which was created.</returns>
    public DiscordModalTextInput AddInput(string label, string? placeholder = null, string? initialValue = null,
        bool isRequired = true, TextInputStyle inputStyle = TextInputStyle.Short, int minLength = 0, int? maxLength = null)
    {
        var customId = Guid.NewGuid().ToString("N");
        var input = new DiscordModalTextInput(new TextInputComponent(label, customId, placeholder, initialValue, isRequired,
            inputStyle, minLength, maxLength));
        _inputs.Add(input);
        return input;
    }

    /// <summary>
    ///     Sets the title of this modal.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>The current instance of <see cref="DiscordModalBuilder" />.</returns>
    public DiscordModalBuilder WithTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    ///     Implicitly converts a <see cref="DiscordModalBuilder" /> to a <see cref="DiscordModal" />.
    /// </summary>
    /// <param name="builder">The builder to build.</param>
    /// <returns>The converted <see cref="DiscordModal" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static implicit operator DiscordModal(DiscordModalBuilder builder)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        return builder.Build();
    }

    /// <summary>
    ///     Builds the modal.
    /// </summary>
    /// <returns>The newly-constructed <see cref="DiscordModal" />.</returns>
    public DiscordModal Build()
    {
        if (string.IsNullOrWhiteSpace(_title)) throw new InvalidOperationException("Title cannot be null or whitespace.");
        return new DiscordModal(Title, _inputs, _discordClient);
    }
}
