// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Represents a glyph metric from a particular Compact Font Face.
    /// </summary>
    internal class CffGlyphMetrics : GlyphMetrics
    {
        private static readonly Vector2 MirrorScale = new(1, -1);
        private CffGlyphData glyphData;

        public CffGlyphMetrics(
            StreamFontMetrics font,
            CodePoint codePoint,
            CffGlyphData glyphData,
            Bounds bounds,
            ushort advanceWidth,
            ushort advanceHeight,
            short leftSideBearing,
            short topSideBearing,
            ushort unitsPerEM,
            ushort glyphId,
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
            : base(font, codePoint, bounds, advanceWidth, advanceHeight, leftSideBearing, topSideBearing, unitsPerEM, glyphId, glyphType, glyphColor)
            => this.glyphData = glyphData;

        /// <inheritdoc/>
        internal override GlyphMetrics CloneForRendering(TextRun textRun, CodePoint codePoint)
        {
            StreamFontMetrics fontMetrics = this.FontMetrics;
            Vector2 offset = this.Offset;
            Vector2 scaleFactor = this.ScaleFactor;
            if (textRun.TextAttributes.HasFlag(TextAttributes.Subscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SubscriptXSize / units, fontMetrics.SubscriptYSize / units);
                offset = new(this.FontMetrics.SubscriptXOffset, this.FontMetrics.SubscriptYOffset);
            }
            else if (textRun.TextAttributes.HasFlag(TextAttributes.Superscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SuperscriptXSize / units, fontMetrics.SuperscriptYSize / units);
                offset = new(fontMetrics.SuperscriptXOffset, -fontMetrics.SuperscriptYOffset);
            }

            return new CffGlyphMetrics(
                fontMetrics,
                codePoint,
                this.glyphData,
                this.Bounds,
                this.AdvanceWidth,
                this.AdvanceHeight,
                this.LeftSideBearing,
                this.TopSideBearing,
                this.UnitsPerEm,
                this.GlyphId,
                this.GlyphType,
                this.GlyphColor)
            {
                Offset = offset,
                ScaleFactor = scaleFactor,
                TextRun = textRun
            };
        }

        /// <inheritdoc/>
        internal override void RenderTo(IGlyphRenderer renderer, float pointSize, Vector2 location, TextOptions options)
        {
            // https://www.unicode.org/faq/unsup_char.html
            if (ShouldSkipGlyphRendering(this.CodePoint))
            {
                return;
            }

            float dpi = options.Dpi;
            location *= dpi;
            float scaledPPEM = dpi * pointSize;
            bool forcePPEMToInt = (this.FontMetrics.HeadFlags & HeadTable.HeadFlags.ForcePPEMToInt) != 0;

            if (forcePPEMToInt)
            {
                scaledPPEM = MathF.Round(scaledPPEM);
            }

            FontRectangle box = this.GetBoundingBox(location, scaledPPEM);

            // TextRun is never null here as rendering is only accessable via a Glyph which
            // uses the cloned metrics instance.
            var parameters = new GlyphRendererParameters(this, this.TextRun!, pointSize, dpi);
            if (renderer.BeginGlyph(box, parameters))
            {
                if (!ShouldRenderWhiteSpaceOnly(this.CodePoint))
                {
                    if (this.GlyphColor.HasValue && renderer is IColorGlyphRenderer colorSurface)
                    {
                        colorSurface.SetColor(this.GlyphColor.Value);
                    }

                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor * MirrorScale;
                    Vector2 offset = location + (this.Offset * scale * MirrorScale);
                    this.glyphData.RenderTo(renderer, scale, offset);
                }

                this.RenderDecorationsTo(renderer, location, scaledPPEM);
            }

            renderer.EndGlyph();
        }
    }
}
