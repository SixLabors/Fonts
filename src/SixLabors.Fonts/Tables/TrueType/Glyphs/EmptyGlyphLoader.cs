// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// A <see cref="GlyphLoader"/> that produces an empty glyph outline.
/// Used for glyphs that have no outline data (e.g. space characters).
/// </summary>
internal class EmptyGlyphLoader : GlyphLoader
{
    private bool loop;
    private readonly Bounds fallbackEmptyBounds;
    private GlyphVector? glyph;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyGlyphLoader"/> class.
    /// </summary>
    /// <param name="fallbackEmptyBounds">The fallback bounds to use if glyph 0 cannot be resolved.</param>
    public EmptyGlyphLoader(Bounds fallbackEmptyBounds)
        => this.fallbackEmptyBounds = fallbackEmptyBounds;

    /// <inheritdoc/>
    public override GlyphVector CreateGlyph(GlyphTable table)
    {
        if (this.loop)
        {
            this.glyph ??= GlyphVector.Empty(this.fallbackEmptyBounds);
            return this.glyph.Value;
        }

        this.loop = true;
        this.glyph ??= GlyphVector.Empty(table.GetGlyph(0).Bounds);
        return this.glyph.Value;
    }
}
