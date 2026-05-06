// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Represents a segment between two Unicode word boundaries.
/// </summary>
public readonly ref struct WordSegment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WordSegment"/> struct.
    /// </summary>
    /// <param name="span">The UTF-16 span containing the word-boundary segment.</param>
    /// <param name="utf16Offset">The UTF-16 offset of the segment in the original source.</param>
    /// <param name="codePointOffset">The code point offset of the segment in the original source.</param>
    /// <param name="codePointCount">The number of Unicode scalar values in the segment.</param>
    public WordSegment(
        ReadOnlySpan<char> span,
        int utf16Offset,
        int codePointOffset,
        int codePointCount)
    {
        this.Span = span;
        this.Utf16Offset = utf16Offset;
        this.Utf16Length = span.Length;
        this.CodePointOffset = codePointOffset;
        this.CodePointCount = codePointCount;
    }

    /// <summary>
    /// Gets the UTF-16 span containing the word-boundary segment.
    /// </summary>
    public ReadOnlySpan<char> Span { get; }

    /// <summary>
    /// Gets the UTF-16 offset of the segment in the original source.
    /// </summary>
    public int Utf16Offset { get; }

    /// <summary>
    /// Gets the UTF-16 length of the segment.
    /// </summary>
    public int Utf16Length { get; }

    /// <summary>
    /// Gets the code point offset of the segment in the original source.
    /// </summary>
    public int CodePointOffset { get; }

    /// <summary>
    /// Gets the number of Unicode scalar values in the segment.
    /// </summary>
    public int CodePointCount { get; }
}
