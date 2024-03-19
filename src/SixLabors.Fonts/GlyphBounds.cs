// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Represents the bounds of a <see cref="Glyph"/> for a given <see cref="CodePoint"/>.
/// </summary>
public readonly struct GlyphBounds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphBounds"/> struct.
    /// </summary>
    /// <param name="codePoint">The Unicode codepoint for the glyph.</param>
    /// <param name="bounds">The glyph bounds.</param>
    /// <param name="graphemeIndex">The index of the grapheme in original text.</param>
    /// <param name="stringIndex">The index of the codepoint in original text..</param>
    public GlyphBounds(CodePoint codePoint, in FontRectangle bounds, int graphemeIndex, int stringIndex)
    {
        this.Codepoint = codePoint;
        this.Bounds = bounds;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
    }

    /// <summary>
    /// Gets the Unicode codepoint of the glyph.
    /// </summary>
    public CodePoint Codepoint { get; }

    /// <summary>
    /// Gets the glyph bounds.
    /// </summary>
    public FontRectangle Bounds { get; }

    /// <summary>
    /// Gets grapheme index of glyph in original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets string index of glyph in original text.
    /// </summary>
    public int StringIndex { get; }

    /// <inheritdoc/>
    public override string ToString()
        => $"Codepoint: {this.Codepoint}, Bounds: {this.Bounds}.";
}
