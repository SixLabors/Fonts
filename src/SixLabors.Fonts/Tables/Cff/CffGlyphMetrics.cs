// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents a glyph metric from a particular Compact Font Face.
/// </summary>
internal class CffGlyphMetrics : FontGlyphMetrics
{
    private CffGlyphData glyphData;

    /// <summary>
    /// Initializes a new instance of the <see cref="CffGlyphMetrics"/> class with text attribute parameters.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="glyphData">The CFF glyph data containing the charstring program.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="advanceWidth">The advance width.</param>
    /// <param name="advanceHeight">The advance height.</param>
    /// <param name="leftSideBearing">The left side bearing.</param>
    /// <param name="topSideBearing">The top side bearing.</param>
    /// <param name="unitsPerEM">The units per em.</param>
    /// <param name="textAttributes">The text attributes.</param>
    /// <param name="textDecorations">The text decorations.</param>
    /// <param name="glyphType">The glyph type.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CffGlyphMetrics"/> class with offset, scale, and text run parameters.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <param name="glyphData">The CFF glyph data containing the charstring program.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="advanceWidth">The advance width.</param>
    /// <param name="advanceHeight">The advance height.</param>
    /// <param name="leftSideBearing">The left side bearing.</param>
    /// <param name="topSideBearing">The top side bearing.</param>
    /// <param name="unitsPerEM">The units per em.</param>
    /// <param name="offset">The glyph offset.</param>
    /// <param name="scaleFactor">The scale factor.</param>
    /// <param name="textRun">The text run for rendering.</param>
    /// <param name="glyphType">The glyph type.</param>
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
    internal override FontGlyphMetrics CloneForRendering(TextRun textRun)
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
        Vector2 glyphOrigin,
        Vector2 decorationOrigin,
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

        glyphOrigin *= dpi;
        decorationOrigin *= dpi;
        float scaledPPEM = this.GetScaledSize(pointSize, dpi);

        Matrix3x2 rotation = GetRotationMatrix(mode);
        FontRectangle box = this.GetBoundingBox(mode, glyphOrigin, scaledPPEM);
        GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, mode, graphemeIndex);

        if (renderer.BeginGlyph(in box, in parameters))
        {
            if (!UnicodeUtility.ShouldRenderWhiteSpaceOnly(this.CodePoint))
            {
                Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;

                // Apply the CFF FontMatrix to convert charstring coordinates to design units.
                // The normalized FontMatrix (fontMatrix * unitsPerEM) is identity for the default
                // [0.001, 0, 0, 0.001, 0, 0] with upm=1000.
                if (this.glyphData.FontMatrix is double[] fm)
                {
                    float upm = this.UnitsPerEm;
                    scale *= new Vector2((float)(fm[0] * upm), (float)(fm[3] * upm));
                }

                Vector2 scaledOffset = this.Offset * scale;
                this.glyphData.RenderTo(renderer, glyphOrigin, scale, scaledOffset, rotation);
            }

            renderer.EndGlyph();
            this.RenderDecorationsTo(renderer, decorationOrigin, mode, rotation, scaledPPEM, options);
        }
    }
}
