// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Generic;
using System.Linq;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a shaped and line-broken block of text.
/// </summary>
internal sealed class TextBox
{
    private float? scaledMaxAdvance;

    private float? minY;

    private int glyphLayoutCount;

    private bool hasGlyphLayoutCounts;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBox"/> class.
    /// </summary>
    /// <param name="textLines">The shaped, line-broken lines that make up this text box.</param>
    public TextBox(IReadOnlyList<TextLine> textLines)
        => this.TextLines = textLines;

    /// <summary>
    /// Gets the shaped and line-broken lines that make up the text.
    /// </summary>
    public IReadOnlyList<TextLine> TextLines { get; }

    /// <summary>
    /// Returns the widest scaled line advance across all lines. The result is memoized.
    /// </summary>
    /// <returns>The widest scaled line advance.</returns>
    public float ScaledMaxAdvance()
        => this.scaledMaxAdvance ??= this.TextLines.Max(x => x.ScaledLineAdvance);

    /// <summary>
    /// Returns the smallest (most negative) scaled Y position encountered across all lines.
    /// Used to detect ink that extends above the typographic ascender (stacked marks in Tibetan etc.).
    /// The result is memoized.
    /// </summary>
    /// <returns>The smallest scaled Y position in the text box.</returns>
    public float ScaledMinY()
        => this.minY ??= this.TextLines.Min(x => x.ScaledMinY);

    /// <summary>
    /// Counts all glyph entries emitted from this text box. The result is memoized.
    /// </summary>
    /// <returns>The number of glyph entries that layout will emit.</returns>
    public int CountGlyphLayouts()
        => this.hasGlyphLayoutCounts ? this.glyphLayoutCount : this.CountGlyphLayoutsCore();

    /// <summary>
    /// Computes the glyph-layout count in one pass.
    /// </summary>
    /// <returns>The number of glyph entries that layout will emit.</returns>
    private int CountGlyphLayoutsCore()
    {
        int count = 0;
        for (int i = 0; i < this.TextLines.Count; i++)
        {
            count += this.TextLines[i].CountGlyphLayouts();
        }

        this.glyphLayoutCount = count;
        this.hasGlyphLayoutCounts = true;
        return count;
    }

    /// <summary>
    /// Returns the resolved text direction of the first glyph in the first line. Used as the
    /// block-level direction for alignment calculations.
    /// </summary>
    /// <returns>The block-level text direction.</returns>
    public TextDirection TextDirection() => this.TextLines[0][0].TextDirection;
}
