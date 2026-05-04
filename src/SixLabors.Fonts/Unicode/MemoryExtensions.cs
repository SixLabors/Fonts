// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Contains extension methods for memory types.
/// </summary>
public static class MemoryExtensions
{
    /// <summary>
    /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid UTF-16 sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
    public static SpanCodePointEnumerator EnumerateCodePoints(this ReadOnlySpan<char> span)
        => new(span);

    /// <summary>
    /// Returns an enumeration of <see cref="CodePoint"/> from the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid UTF-16 sequences will be represented in the enumeration by <see cref="CodePoint.ReplacementChar"/>.
    /// </remarks>
    /// <returns>The <see cref="SpanCodePointEnumerator"/>.</returns>
    public static SpanCodePointEnumerator EnumerateCodePoints(this Span<char> span)
        => new(span);

    /// <summary>
    /// Returns the number of code points in the provided text.
    /// </summary>
    /// <param name="text">The text to enumerate.</param>
    /// <returns>The number of code points.</returns>
    public static int GetCodePointCount(this string text) => text.AsSpan().GetCodePointCount();

    /// <summary>
    /// Returns the number of code points in the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to enumerate.</param>
    /// <returns>The number of code points.</returns>
    public static int GetCodePointCount(this ReadOnlySpan<char> span) => CodePoint.GetCodePointCount(span);

    /// <summary>
    /// Returns the number of code points in the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <returns>The number of code points.</returns>
    public static int GetCodePointCount(this Span<char> span) => CodePoint.GetCodePointCount(span);

    /// <summary>
    /// Returns an enumeration of grapheme clusters from the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid UTF-16 sequences are treated as <see cref="CodePoint.ReplacementChar"/> while determining grapheme boundaries.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this ReadOnlySpan<char> span)
        => new(span);

    /// <summary>
    /// Returns an enumeration of grapheme clusters from the provided span with terminal-width metadata.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to enumerate.</param>
    /// <param name="terminalWidthOptions">
    /// The terminal width options used to resolve <see cref="GraphemeCluster.TerminalCellWidth"/>.
    /// </param>
    /// <remarks>
    /// Invalid UTF-16 sequences are treated as <see cref="CodePoint.ReplacementChar"/> while determining grapheme boundaries.
    /// Terminal width options affect only the width metadata on each returned cluster; they do not affect grapheme segmentation.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this ReadOnlySpan<char> span, TerminalWidthOptions terminalWidthOptions)
        => new(span, terminalWidthOptions);

    /// <summary>
    /// Returns an enumeration of grapheme clusters from the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <remarks>
    /// Invalid UTF-16 sequences are treated as <see cref="CodePoint.ReplacementChar"/> while determining grapheme boundaries.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this Span<char> span)
        => new(span);

    /// <summary>
    /// Returns an enumeration of grapheme clusters from the provided span with terminal-width metadata.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <param name="terminalWidthOptions">
    /// The terminal width options used to resolve <see cref="GraphemeCluster.TerminalCellWidth"/>.
    /// </param>
    /// <remarks>
    /// Invalid UTF-16 sequences are treated as <see cref="CodePoint.ReplacementChar"/> while determining grapheme boundaries.
    /// Terminal width options affect only the width metadata on each returned cluster; they do not affect grapheme segmentation.
    /// </remarks>
    /// <returns>The <see cref="SpanGraphemeEnumerator"/>.</returns>
    public static SpanGraphemeEnumerator EnumerateGraphemes(this Span<char> span, TerminalWidthOptions terminalWidthOptions)
        => new(span, terminalWidthOptions);

    /// <summary>
    /// Returns the terminal cell width of the provided text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this string text) => text.AsSpan().GetTerminalCellWidth();

    /// <summary>
    /// Returns the terminal cell width of the provided text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="terminalWidthOptions">The terminal width options to apply while measuring.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this string text, TerminalWidthOptions terminalWidthOptions)
        => text.AsSpan().GetTerminalCellWidth(terminalWidthOptions);

    /// <summary>
    /// Returns the terminal cell width of the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to measure.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this ReadOnlySpan<char> span)
        => span.GetTerminalCellWidth(default);

    /// <summary>
    /// Returns the terminal cell width of the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to measure.</param>
    /// <param name="terminalWidthOptions">The terminal width options to apply while measuring.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this ReadOnlySpan<char> span, TerminalWidthOptions terminalWidthOptions)
    {
        int width = 0;
        SpanGraphemeEnumerator enumerator = new(span, terminalWidthOptions);

        while (enumerator.MoveNext())
        {
            int terminalCellWidth = enumerator.Current.TerminalCellWidth;
            if (terminalCellWidth < 0)
            {
                return -1;
            }

            width += terminalCellWidth;
        }

        return width;
    }

    /// <summary>
    /// Returns the terminal cell width of the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to measure.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this Span<char> span)
        => ((ReadOnlySpan<char>)span).GetTerminalCellWidth();

    /// <summary>
    /// Returns the terminal cell width of the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to measure.</param>
    /// <param name="terminalWidthOptions">The terminal width options to apply while measuring.</param>
    /// <returns>
    /// The terminal cell width, or <c>-1</c> when the configured control-character policy treats
    /// any grapheme cluster in the text as non-printable.
    /// </returns>
    public static int GetTerminalCellWidth(this Span<char> span, TerminalWidthOptions terminalWidthOptions)
        => ((ReadOnlySpan<char>)span).GetTerminalCellWidth(terminalWidthOptions);

    /// <summary>
    /// Returns the number of grapheme clusters in the provided text.
    /// </summary>
    /// <param name="text">The text to enumerate.</param>
    /// <returns>The number of grapheme clusters.</returns>
    public static int GetGraphemeCount(this string text) => text.AsSpan().GetGraphemeCount();

    /// <summary>
    /// Returns the number of grapheme clusters in the provided span.
    /// </summary>
    /// <param name="span">The read-only span of char elements representing the text to enumerate.</param>
    /// <returns>The number of grapheme clusters.</returns>
    public static int GetGraphemeCount(this ReadOnlySpan<char> span)
    {
        int count = 0;
        SpanGraphemeEnumerator enumerator = new(span);
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Returns the number of grapheme clusters in the provided span.
    /// </summary>
    /// <param name="span">The span of char elements representing the text to enumerate.</param>
    /// <returns>The number of grapheme clusters.</returns>
    public static int GetGraphemeCount(this Span<char> span)
    {
        int count = 0;
        SpanGraphemeEnumerator enumerator = new(span);
        while (enumerator.MoveNext())
        {
            count++;
        }

        return count;
    }
}
