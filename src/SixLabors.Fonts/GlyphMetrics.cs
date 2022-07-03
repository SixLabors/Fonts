// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a glyph metric from a particular font face.
    /// </summary>
    public abstract class GlyphMetrics
    {
        private static readonly Vector2 MirrorScale = new(1, -1);

        internal GlyphMetrics(
            StreamFontMetrics font,
            CodePoint codePoint,
            Bounds bounds,
            ushort advanceWidth,
            ushort advanceHeight,
            short leftSideBearing,
            short topSideBearing,
            ushort unitsPerEM,
            ushort glyphId,
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
        {
            this.FontMetrics = font;
            this.CodePoint = codePoint;
            this.UnitsPerEm = unitsPerEM;
            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.GlyphId = glyphId;
            this.Bounds = bounds;
            this.Width = bounds.Max.X - bounds.Min.X;
            this.Height = bounds.Max.Y - bounds.Min.Y;
            this.GlyphType = glyphType;
            this.LeftSideBearing = leftSideBearing;
            this.RightSideBearing = (short)(this.AdvanceWidth - this.LeftSideBearing - this.Width);
            this.TopSideBearing = topSideBearing;
            this.BottomSideBearing = (short)(this.AdvanceHeight - this.TopSideBearing - this.Height);
            this.ScaleFactor = new(unitsPerEM * 72F);
            this.GlyphColor = glyphColor;
        }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        internal StreamFontMetrics FontMetrics { get; }

        /// <summary>
        /// Gets the Unicode codepoint of the glyph.
        /// </summary>
        public CodePoint CodePoint { get; }

        /// <summary>
        /// Gets the advance width for horizontal layout, expressed in font units.
        /// </summary>
        public ushort AdvanceWidth { get; private set; }

        /// <summary>
        /// Gets the advance height for vertical layout, expressed in font units.
        /// </summary>
        public ushort AdvanceHeight { get; private set; }

        /// <summary>
        /// Gets the left side bearing for horizontal layout, expressed in font units.
        /// </summary>
        public short LeftSideBearing { get; }

        /// <summary>
        /// Gets the right side bearing for horizontal layout, expressed in font units.
        /// </summary>
        public short RightSideBearing { get; }

        /// <summary>
        /// Gets the top side bearing for vertical layout, expressed in font units.
        /// </summary>
        public short TopSideBearing { get; }

        /// <summary>
        /// Gets the bottom side bearing for vertical layout, expressed in font units.
        /// </summary>
        public short BottomSideBearing { get; }

        /// <summary>
        /// Gets the bounds, expressed in font units.
        /// </summary>
        internal Bounds Bounds { get; }

        /// <summary>
        /// Gets the width, expressed in font units.
        /// </summary>
        public float Width { get; }

        /// <summary>
        /// Gets the height, expressed in font units.
        /// </summary>
        public float Height { get; }

        /// <summary>
        /// Gets the glyph type.
        /// </summary>
        public GlyphType GlyphType { get; }

        /// <summary>
        /// Gets the color of this glyph when the <see cref="GlyphType"/> is <see cref="GlyphType.ColrLayer"/>
        /// </summary>
        public GlyphColor? GlyphColor { get; }

        /// <inheritdoc cref="FontMetrics.UnitsPerEm"/>
        public ushort UnitsPerEm { get; }

        /// <inheritdoc cref="FontMetrics.ScaleFactor"/>
        public Vector2 ScaleFactor { get; protected set; }

        internal Vector2 Offset { get; set; }

        internal TextRun? TextRun { get; set; }

        /// <summary>
        /// Gets the glyph Id.
        /// </summary>
        internal ushort GlyphId { get; }

        /// <summary>
        /// Performs a semi-deep clone (FontMetrics are not cloned) for rendering
        /// This allows caching the original in the font metrics.
        /// </summary>
        /// <param name="textRun">The text run this glyph is a member of.</param>
        /// <param name="codePoint">The codepoint for this glyph.</param>
        /// <returns>The new <see cref="GlyphMetrics"/>.</returns>
        internal abstract GlyphMetrics CloneForRendering(TextRun textRun, CodePoint codePoint);

        /// <summary>
        /// Apply an offset to the glyph.
        /// </summary>
        /// <param name="x">The x-offset.</param>
        /// <param name="y">The y-offset.</param>
        internal void ApplyOffset(short x, short y)
            => this.Offset = Vector2.Transform(this.Offset, Matrix3x2.CreateTranslation(x, y));

        /// <summary>
        /// Applies an advance to the glyph.
        /// </summary>
        /// <param name="x">The x-advance.</param>
        /// <param name="y">The y-advance.</param>
        internal void ApplyAdvance(short x, short y)
        {
            this.AdvanceWidth = (ushort)(this.AdvanceWidth + x);

            // AdvanceHeight values grow downward but font-space grows upward, hence negation
            this.AdvanceHeight = (ushort)(this.AdvanceHeight - y);
        }

        /// <summary>
        /// Sets a new advance width.
        /// </summary>
        /// <param name="x">The x-advance.</param>
        internal void SetAdvanceWidth(ushort x) => this.AdvanceWidth = x;

        /// <summary>
        /// Sets a new advance height.
        /// </summary>
        /// <param name="y">The y-advance.</param>
        internal void SetAdvanceHeight(ushort y) => this.AdvanceHeight = y;

        internal FontRectangle GetBoundingBox(Vector2 origin, float scaledPointSize)
        {
            Vector2 scale = new Vector2(scaledPointSize) / this.ScaleFactor;
            Bounds bounds = this.Bounds;
            Vector2 size = bounds.Size() * scale;
            Vector2 loc = (new Vector2(bounds.Min.X, bounds.Max.Y) + this.Offset) * scale * MirrorScale;
            loc = origin + loc;

            return new FontRectangle(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="renderer">The surface renderer.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="options">The options used to influence the rendering of this glyph.</param>
        internal abstract void RenderTo(IGlyphRenderer renderer, float pointSize, Vector2 location, TextOptions options);

        internal void RenderDecorationsTo(IGlyphRenderer renderer, Vector2 location, float scaledPPEM)
        {
            (Vector2 Start, Vector2 End, float Thickness) GetEnds(float thickness, float position)
            {
                Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor * MirrorScale;
                Vector2 offset = location + (this.Offset * scale * MirrorScale);

                // Calculate the correct advance for the line.
                float width = this.AdvanceWidth;
                if (width == 0)
                {
                    // For zero advance glyphs we must calculate our advance width from bearing + width;
                    width = this.LeftSideBearing + this.Width;
                }

                Vector2 tl = (new Vector2(0, position) * scale) + offset;
                Vector2 tr = (new Vector2(width, position) * scale) + offset;
                Vector2 bl = (new Vector2(0, position + thickness) * scale) + offset;

                return (tl, tr, tl.Y - bl.Y);
            }

            void DrawLine(float thickness, float position)
            {
                renderer.BeginFigure();

                (Vector2 start, Vector2 end, float finalThickness) = GetEnds(thickness, position);
                var halfHeight = new Vector2(0, -finalThickness * .5F);

                Vector2 tl = start - halfHeight;
                Vector2 tr = end - halfHeight;
                Vector2 bl = start + halfHeight;
                Vector2 br = end + halfHeight;

                // Clamp the horizontal components to a whole pixel.
                tl.Y = MathF.Ceiling(tl.Y);
                tr.Y = MathF.Ceiling(tr.Y);
                br.Y = MathF.Floor(br.Y);
                bl.Y = MathF.Floor(bl.Y);

                // Do the same for vertical components.
                tl.X = MathF.Floor(tl.X);
                tr.X = MathF.Floor(tr.X);
                br.X = MathF.Floor(br.X);
                bl.X = MathF.Floor(bl.X);

                renderer.MoveTo(tl);
                renderer.LineTo(bl);
                renderer.LineTo(br);
                renderer.LineTo(tr);

                renderer.EndFigure();
            }

            void SetDecoration(TextDecorations decorationType, float thickness, float position)
            {
                (Vector2 start, Vector2 end, float calcThickness) = GetEnds(thickness, position);
                ((IGlyphDecorationRenderer)renderer).SetDecoration(decorationType, start, end, calcThickness);
            }

            // There's no built in metrics for these values so we will need to infer them from the other metrics.
            // Offset to avoid clipping.
            float overlineThickness = this.FontMetrics.UnderlineThickness;

            // TODO: Check this. Segoe UI glyphs live outside the metrics so the overline covers the glyph.
            float overlinePosition = this.FontMetrics.Ascender - (overlineThickness * .5F);
            if (renderer is IGlyphDecorationRenderer decorationRenderer)
            {
                // Allow the rendered to override the decorations to attach
                TextDecorations decorations = decorationRenderer.EnabledDecorations();
                if ((decorations & TextDecorations.Underline) == TextDecorations.Underline)
                {
                    SetDecoration(TextDecorations.Underline, this.FontMetrics.UnderlineThickness, this.FontMetrics.UnderlinePosition);
                }

                if ((decorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
                {
                    SetDecoration(TextDecorations.Strikeout, this.FontMetrics.StrikeoutSize, this.FontMetrics.StrikeoutPosition);
                }

                if ((decorations & TextDecorations.Overline) == TextDecorations.Overline)
                {
                    SetDecoration(TextDecorations.Overline, overlineThickness, overlinePosition);
                }
            }
            else
            {
                // TextRun is never null here as rendering is only accessable via a Glyph which
                // uses the cloned metrics instance.
                if ((this.TextRun!.TextDecorations & TextDecorations.Underline) == TextDecorations.Underline)
                {
                    DrawLine(this.FontMetrics.UnderlineThickness, this.FontMetrics.UnderlinePosition);
                }

                if ((this.TextRun!.TextDecorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
                {
                    DrawLine(this.FontMetrics.StrikeoutSize, this.FontMetrics.StrikeoutPosition);
                }

                if ((this.TextRun!.TextDecorations & TextDecorations.Overline) == TextDecorations.Overline)
                {
                    DrawLine(overlineThickness, overlinePosition);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldSkipGlyphRendering(CodePoint codePoint)
            => UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) && !ShouldRenderWhiteSpaceOnly(codePoint);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldRenderWhiteSpaceOnly(CodePoint codePoint)
        {
            if (CodePoint.IsWhiteSpace(codePoint))
            {
                return true;
            }

            // Note: While U+115F, U+1160, U+3164 and U+FFA0 are Default_Ignorable,
            // we do NOT want to hide them, as the way Uniscribe has implemented them
            // is with regular spacing glyphs, and that's the way fonts are made to work.
            // As such, we make exceptions for those four.
            // Also ignoring U+1BCA0..1BCA3. https://github.com/harfbuzz/harfbuzz/issues/503
            uint value = (uint)codePoint.Value;
            if (value is 0x115F or 0x1160 or 0x3164 or 0xFFA0)
            {
                return true;
            }

            if (UnicodeUtility.IsInRangeInclusive(value, 0x1BCA0, 0x1BCA3))
            {
                return true;
            }

            return false;
        }
    }
}
