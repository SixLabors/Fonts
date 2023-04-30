// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Represents a glyph metric from a particular Compact Font Face.
    /// </summary>
    internal class CffGlyphMetrics : GlyphMetrics
    {
        private static readonly Vector2 YInverter = new(1, -1);
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
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
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
                  glyphType,
                  glyphColor)
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
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
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
                  glyphType,
                  glyphColor)
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
                this.GlyphType,
                this.GlyphColor);

        /// <inheritdoc/>
        internal override void RenderTo(IGlyphRenderer renderer, Vector2 location, Vector2 offset, TextOptions options)
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

            bool rotated = this.TryGetRotationMatrix(options.LayoutMode, out Matrix3x2 rotation);
            FontRectangle box = this.GetBoundingBox(Vector2.Zero, scaledPPEM);
            box = FontRectangle.Transform(in box, rotation);
            box = new FontRectangle(box.X + renderLocation.X, box.Y + renderLocation.Y, box.Width, box.Height);

            GlyphRendererParameters parameters = new(this, this.TextRun, pointSize, dpi, options.LayoutMode);
            if (renderer.BeginGlyph(in box, in parameters))
            {
                if (!ShouldRenderWhiteSpaceOnly(this.CodePoint))
                {
                    if (this.GlyphColor.HasValue && renderer is IColorGlyphRenderer colorSurface)
                    {
                        colorSurface.SetColor(this.GlyphColor.Value);
                    }

                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
                    Vector2 scaledOffset = this.Offset * scale;
                    this.glyphData.RenderTo(renderer, renderLocation, scale, scaledOffset, rotation);
                }

                this.RenderDecorationsTo(renderer, location, options.LayoutMode, rotated, rotation, scaledPPEM);
            }

            renderer.EndGlyph();
        }
    }
}
