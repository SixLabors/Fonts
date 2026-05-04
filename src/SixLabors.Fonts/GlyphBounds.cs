// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents measured bounds for a laid-out glyph entry.
/// </summary>
public readonly struct GlyphBounds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphBounds"/> struct.
    /// </summary>
    /// <param name="codePoint">The Unicode code point represented by the glyph entry.</param>
    /// <param name="bounds">The measured bounds.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text where the glyph entry begins.</param>
    public GlyphBounds(CodePoint codePoint, in FontRectangle bounds, int graphemeIndex, int stringIndex)
    {
        this.Codepoint = codePoint;
        this.Bounds = bounds;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the Unicode code point represented by the glyph entry.
    /// </summary>
    public CodePoint Codepoint { get; }

    /// <summary>
    /// Gets the measured bounds.
    /// </summary>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets the grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the UTF-16 index in the original text where the glyph entry begins.
    /// </summary>
    public int StringIndex { get; }

    /// <inheritdoc/>
    public override string ToString()
        => $"Codepoint: {this.Codepoint}, Bounds: {this.Bounds}.";
}
