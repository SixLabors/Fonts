// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a run of text spanning a series of graphemes within a string.
/// </summary>
public class TextRun
{
    /// <summary>
    /// Gets or sets the inclusive start index of the first grapheme in this <see cref="TextRun"/>.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the exclusive end index of the last grapheme in this <see cref="TextRun"/>.
    /// </summary>
    public int End { get; set; }

    /// <summary>
    /// Gets or sets the font for this run.
    /// </summary>
    public Font? Font { get; set; }

    /// <summary>
    /// Gets or sets the text attributes applied to this run.
    /// </summary>
    public TextAttributes TextAttributes { get; set; }

    /// <summary>
    /// Gets or sets the text decorations applied to this run.
    /// </summary>
    public TextDecorations TextDecorations { get; set; }

    /// <summary>
    /// Returns the slice of the given text representing this <see cref="TextRun"/>.
    /// </summary>
    /// <param name="text">The text to slice.</param>
    /// <returns>The <see cref="ReadOnlySpan{Char}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> Slice(ReadOnlySpan<char> text)
    {
        ValidateRange(this.Start, this.End);

        // Convert grapheme indices into char indices so we can slice
        int chars = 0;
        int count = 0;
        int start = 0;
        int length = 0;
        SpanGraphemeEnumerator graphemeEnumerator = new(text);
        while (graphemeEnumerator.MoveNext())
        {
            if (count == this.Start)
            {
                start = chars;
            }

            SpanCodePointEnumerator codePointEnumerator = new(graphemeEnumerator.Current);
            while (codePointEnumerator.MoveNext())
            {
                chars += codePointEnumerator.Current.Utf16SequenceLength;
                length = chars - start;
            }

            if (++count == this.End)
            {
                break;
            }
        }

        return text.Slice(start, length);
    }

    /// <inheritdoc/>
    public override string ToString()
        => $"[TextRun: Start={this.Start}, End={this.End}, TextAttributes={this.TextAttributes}]";

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidateRange(int start, int end)
    {
        if (start < 0 || end < 0)
        {
            throw new ArgumentOutOfRangeException($"Start '{start}' and End '{end}' must be greater or equal to zero.");
        }

        if (end <= start)
        {
            throw new ArgumentOutOfRangeException($"End '{end}' must be greater than Start '{start}'.");
        }
    }
}
