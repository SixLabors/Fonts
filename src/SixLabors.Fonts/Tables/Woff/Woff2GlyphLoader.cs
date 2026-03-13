// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.Woff;

/// <summary>
/// A glyph loader for WOFF2 fonts that wraps a pre-parsed <see cref="GlyphVector"/>
/// from the transformed glyph data stream.
/// See: <see href="https://www.w3.org/TR/WOFF2/#glyf_table_format"/>.
/// </summary>
internal sealed class Woff2GlyphLoader : GlyphLoader
{
    /// <summary>
    /// The glyph vector containing the decoded outline data.
    /// </summary>
    private GlyphVector glyphVector;

    /// <summary>
    /// Initializes a new instance of the <see cref="Woff2GlyphLoader"/> class.
    /// </summary>
    /// <param name="glyphVector">The pre-parsed glyph vector from the WOFF2 transformed glyph stream.</param>
    public Woff2GlyphLoader(GlyphVector glyphVector) => this.glyphVector = glyphVector;

    /// <summary>
    /// Creates a glyph vector, computing bounding box on demand if not already set.
    /// </summary>
    /// <param name="table">The glyph table.</param>
    /// <returns>The <see cref="GlyphVector"/>.</returns>
    public override GlyphVector CreateGlyph(GlyphTable table)
    {
        if (this.glyphVector.Bounds == default)
        {
            this.glyphVector.Bounds = Bounds.Load(this.glyphVector.ControlPoints);
        }

        return this.glyphVector;
    }
}
