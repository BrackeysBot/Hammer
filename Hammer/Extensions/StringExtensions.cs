using System;
using System.Diagnostics.CodeAnalysis;

namespace Hammer.Extensions;

// so I'm just gonna let you know that these methods exist in X10D 3.0.0 but since I haven't even published a preview nuget yet,
// I'm gonna just implement them here. the following methods are verbatim as they appear in X10D
// https://github.com/oliverbooth/X10D/blob/45804c2da6fb61555fadf5503cd5d0d1bbde88cc/X10D/src/StringExtensions/StringExtensions.cs

/// <summary>
///     Extension methods for <see cref="string" />.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    ///     Normalizes a string which may be either <see langword="null" /> or empty to <see langword="null" />.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>
    ///     <see langword="null" /> if <paramref name="value" /> is <see langword="null" /> or empty; otherwise,
    ///     <paramref name="value" />.
    /// </returns>
    [return: NotNullIfNotNull("value")]
    public static string? AsNullIfEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>
    ///     Normalizes a string which may be either <see langword="null" />, empty, or consisting of only whitespace, to
    ///     <see langword="null" />.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>
    ///     <see langword="null" /> if <paramref name="value" /> is <see langword="null" />, empty, or consists of only
    ///     whitespace; otherwise, <paramref name="value" />.
    /// </returns>
    [return: NotNullIfNotNull("value")]
    public static string? AsNullIfWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    ///     Normalizes a string which may be either <see langword="null" /> or empty to a specified alternative.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>
    ///     <paramref name="alternative" /> if <paramref name="value" /> is <see langword="null" /> or empty; otherwise,
    ///     <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="alternative" /> is <see langword="null" />.</exception>
    public static string WithEmptyAlternative(this string? value, string alternative)
    {
        if (alternative is null)
        {
            throw new ArgumentNullException(nameof(alternative));
        }
        
        return string.IsNullOrEmpty(value) ? alternative : value;
    }

    /// <summary>
    ///     Normalizes a string which may be either <see langword="null" />, empty, or consisting of only whitespace, to a
    ///     specified alternative.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <param name="alternative">The alternative string.</param>
    /// <returns>
    ///     <paramref name="alternative" /> if <paramref name="value" /> is <see langword="null" />, empty, or consists of only
    ///     whitespace; otherwise, <paramref name="value" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="alternative" /> is <see langword="null" />.</exception>
    public static string WithWhiteSpaceAlternative(this string? value, string alternative)
    {
        if (alternative is null)
        {
            throw new ArgumentNullException(nameof(alternative));
        }

        return string.IsNullOrWhiteSpace(value) ? alternative : value;
    }
}
