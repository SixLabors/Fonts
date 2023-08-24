// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Contains extensions methods for memory types.
/// </summary>
public static class MemoryExtensions
{
    /// <summary>
    /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
    /// </summary>
    /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
    public static SpanCodePointEnumerator EnumerateCodePoints(this ReadOnlySpan<char> span)
        => new(span);

    /// <summary>
    /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
    public static SpanCodePointEnumerator EnumerateCodePoints(this Span<char> span)
        => new(span);

    /// <summary>
    /// Returns the number of code points in the provided text.
    /// </summary>
    /// <param name="text">The text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetCodePointCount(this string text) => text.AsSpan().GetCodePointCount();

    /// <summary>
    /// Returns the number of code points in the provided span.
    /// </summary>
    /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetCodePointCount(this ReadOnlySpan<char> span) => CodePoint.GetCodePointCount(span);

    /// <summary>
    /// Returns the number of code points in the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetCodePointCount(this Span<char> span) => CodePoint.GetCodePointCount(span);

    /// <summary>
    /// Returns an enumeration of Grapheme instances from the provided span.
    /// </summary>
    /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid sequences will be represented in the enumeration by <see cref="GraphemeClusterClass.Any"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this ReadOnlySpan<char> span)
        => new(span);

    /// <summary>
    /// Returns an enumeration of Grapheme instances from the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid sequences will be represented in the enumeration by <see cref="GraphemeClusterClass.Any"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this Span<char> span)
        => new(span);

    /// <summary>
    /// Returns the number of graphemes in the provided text.
    /// </summary>
    /// <param name="text">The text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetGraphemeCount(this string text) => text.AsSpan().GetGraphemeCount();

    /// <summary>
    /// Returns the number of graphemes in the provided span.
    /// </summary>
    /// <param name="span">The readonly span of char elements representing the text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetGraphemeCount(this ReadOnlySpan<char> span)
    {
        int count = 0;
        var enumerator = new SpanGraphemeEnumerator(span);
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Returns the number of graphemes in the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <returns>The <see cref="int"/> count.</returns>
    public static int GetGraphemeCount(this Span<char> span)
    {
        int count = 0;
        var enumerator = new SpanGraphemeEnumerator(span);
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }
}
