// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Base class for loading glyph outlines from the 'glyf' table.
/// Subclasses handle simple, composite, and empty glyph descriptions.
/// </summary>
internal abstract class GlyphLoader
{
    /// <summary>
    /// Creates a <see cref="GlyphVector"/> representing this glyph's outline.
    /// </summary>
    /// <param name="table">The glyph table used to resolve component glyphs in composite descriptions.</param>
    /// <returns>The <see cref="GlyphVector"/>.</returns>
    public abstract GlyphVector CreateGlyph(GlyphTable table);

    /// <summary>
    /// Reads a glyph description from the binary reader and returns the appropriate loader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the glyph description.</param>
    /// <returns>A <see cref="GlyphLoader"/> for the glyph (simple or composite).</returns>
    public static GlyphLoader Load(BigEndianBinaryReader reader)
    {
        short contoursCount = reader.ReadInt16();
        var bounds = Bounds.Load(reader);

        if (contoursCount >= 0)
        {
            return SimpleGlyphLoader.LoadSimpleGlyph(reader, contoursCount, bounds);
        }

        return CompositeGlyphLoader.LoadCompositeGlyph(reader, bounds);
    }
}
