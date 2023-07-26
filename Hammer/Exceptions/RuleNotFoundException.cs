namespace Hammer.Exceptions;

/// <summary>
///     Represents an exception that is thrown when a specified rule could not be found.
/// </summary>
internal sealed class RuleNotFoundException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleNotFoundException" /> class.
    /// </summary>
    /// <param name="id">The ID of the rule which could not be found.</param>
    public RuleNotFoundException(int id) : this(id, $"No rule with ID {id} was found.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RuleNotFoundException" /> class.
    /// </summary>
    /// <param name="id">The ID of the rule which could not be found.</param>
    /// <param name="message">The exception message.</param>
    public RuleNotFoundException(int id, string message) : base(message)
    {
        Id = id;
    }

    /// <summary>
    ///     Gets the ID of the rule which was not found.
    /// </summary>
    /// <value>The rule ID.</value>
    public int Id { get; }
}
