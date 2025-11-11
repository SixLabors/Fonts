// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a glyph metric from a particular Compact Font Face.
/// </summary>
internal class CffGlyphMetrics : GlyphMetrics
{
    private CffGlyphData glyphData;

    internal CffGlyphMetrics(
        StreamFontMetrics fontMetrics,
        ushort glyphId,
        CodePoint codePoint,
        CffGlyphData glyphData,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        TextAttributes textAttributes,
        TextDecorations textDecorations,
        GlyphType glyphType)
        : base(
              fontMetrics,
              glyphId,
              codePoint,
              bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              textAttributes,
              textDecorations,
              glyphType)
        => this.glyphData = glyphData;

    internal CffGlyphMetrics(
        StreamFontMetrics fontMetrics,
        ushort glyphId,
        CodePoint codePoint,
        CffGlyphData glyphData,
        Bounds bounds,
        ushort advanceWidth,
        ushort advanceHeight,
        short leftSideBearing,
        short topSideBearing,
        ushort unitsPerEM,
        Vector2 offset,
        Vector2 scaleFactor,
        TextRun textRun,
        GlyphType glyphType)
        : base(
              fontMetrics,
              glyphId,
              codePoint,
              bounds,
              advanceWidth,
              advanceHeight,
              leftSideBearing,
              topSideBearing,
              unitsPerEM,
              offset,
              scaleFactor,
              textRun,
              glyphType)
        => this.glyphData = glyphData;

    /// <inheritdoc/>
    internal override GlyphMetrics CloneForRendering(TextRun textRun)
        => new CffGlyphMetrics(
            this.FontMetrics,
            this.GlyphId,
            this.CodePoint,
            this.glyphData,
            this.Bounds,
            this.AdvanceWidth,
            this.AdvanceHeight,
            this.LeftSideBearing,
            this.TopSideBearing,
            this.UnitsPerEm,
            this.Offset,
            this.ScaleFactor,
            textRun,
            this.GlyphType);

    /// <inheritdoc/>
    internal override void RenderTo(
        IGlyphRenderer renderer,
        int graphemeIndex,
        Vector2 location,
        Vector2 offset,
        GlyphLayoutMode mode,
        TextOptions options)
    {
        // https://www.unicode.org/faq/unsup_char.html
        if (ShouldSkipGlyphRendering(this.CodePoint))
        {
            return;
        }

        float pointSize = this.TextRun.Font?.Size ?? options.Font.Size;
        float dpi = options.Dpi;

        // The glyph vector is rendered offset to the location.
        // For horizontal text, the offset is always zero but vertical or rotated text
        // will be offset against the location.
        location *= dpi;
        offset *= dpi;
        Vector2 renderLocation = location + offset;
        float scaledPPEM = this.GetScaledSize(pointSize, dpi);

        Matrix3x2 rotation = GetRotationMatrix(mode);
        FontRectangle box = this.GetBoundingBox(mode, renderLocation, scaledPPEM);
        GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, mode, graphemeIndex);

        if (renderer.BeginGlyph(in box, in parameters))
        {
            if (!UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint))
            {
                Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
                Vector2 scaledOffset = this.Offset * scale;
                this.glyphData.RenderTo(renderer, renderLocation, scale, scaledOffset, rotation);
            }

            renderer.EndGlyph();
            this.RenderDecorationsTo(renderer, location, mode, rotation, scaledPPEM, options);
        }
    }
}
