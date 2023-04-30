// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a glyph metric from a particular font face.
    /// </summary>
    public abstract class GlyphMetrics
    {
        private static readonly Vector2 YInverter = new(1, -1);

        internal GlyphMetrics(
            StreamFontMetrics font,
            ushort glyphId,
            CodePoint codePoint,
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
        {
            this.FontMetrics = font;
            this.GlyphId = glyphId;
            this.CodePoint = codePoint;
            this.Bounds = bounds;
            this.Width = bounds.Max.X - bounds.Min.X;
            this.Height = bounds.Max.Y - bounds.Min.Y;
            this.UnitsPerEm = unitsPerEM;
            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.LeftSideBearing = leftSideBearing;
            this.RightSideBearing = (short)(this.AdvanceWidth - this.LeftSideBearing - this.Width);
            this.TopSideBearing = topSideBearing;
            this.BottomSideBearing = (short)(this.AdvanceHeight - this.TopSideBearing - this.Height);
            this.TextAttributes = textAttributes;
            this.TextDecorations = textDecorations;
            this.GlyphType = glyphType;
            this.GlyphColor = glyphColor;

            Vector2 offset = Vector2.Zero;
            Vector2 scaleFactor = new(unitsPerEM * 72F);
            if (textAttributes.HasFlag(TextAttributes.Subscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(font.SubscriptXSize / units, font.SubscriptYSize / units);
                offset = new(font.SubscriptXOffset, font.SubscriptYOffset < 0 ? font.SubscriptYOffset : -font.SubscriptYOffset);
            }
            else if (textAttributes.HasFlag(TextAttributes.Superscript))
            {
                float units = this.UnitsPerEm;
                scaleFactor /= new Vector2(font.SuperscriptXSize / units, font.SuperscriptYSize / units);
                offset = new(font.SuperscriptXOffset, font.SuperscriptYOffset < 0 ? -font.SuperscriptYOffset : font.SuperscriptYOffset);
            }

            this.ScaleFactor = scaleFactor;
            this.Offset = offset;
        }

        internal GlyphMetrics(
            StreamFontMetrics font,
            ushort glyphId,
            CodePoint codePoint,
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
        {
            // This is used during cloning. Ensure anything that could be changed is copied.
            this.FontMetrics = font;
            this.GlyphId = glyphId;
            this.CodePoint = codePoint;
            this.Bounds = new Bounds(bounds.Min, bounds.Max);
            this.Width = bounds.Max.X - bounds.Min.X;
            this.Height = bounds.Max.Y - bounds.Min.Y;
            this.UnitsPerEm = unitsPerEM;
            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.LeftSideBearing = leftSideBearing;
            this.RightSideBearing = (short)(this.AdvanceWidth - this.LeftSideBearing - this.Width);
            this.TopSideBearing = topSideBearing;
            this.BottomSideBearing = (short)(this.AdvanceHeight - this.TopSideBearing - this.Height);
            this.TextAttributes = textRun.TextAttributes;
            this.TextDecorations = textRun.TextDecorations;
            this.GlyphType = glyphType;
            this.GlyphColor = glyphColor;
            this.ScaleFactor = new Vector2(scaleFactor.X, scaleFactor.Y);
            this.Offset = new Vector2(offset.X, offset.Y);
            this.TextRun = textRun;
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

        /// <summary>
        /// Gets the id of the glyph within the font tables.
        /// </summary>
        public ushort GlyphId { get; }

        /// <summary>
        /// Gets the scale factor that is applied to all glyphs in this face.
        /// Normally calculated as 72 * <see cref="UnitsPerEm"/> so that 1pt = 1px
        /// unless the glyph has <see cref="TextAttributes"/> that apply scaling adjustment.
        /// </summary>
        public Vector2 ScaleFactor { get; }

        /// <summary>
        /// Gets or sets the offset in font design units.
        /// </summary>
        internal Vector2 Offset { get; set; }

        /// <summary>
        /// Gets the text run that the glyph belongs to.
        /// </summary>
        internal TextRun TextRun { get; } = null!;

        /// <summary>
        /// Gets the text attributes applied to the glyph.
        /// </summary>
        public TextAttributes TextAttributes { get; }

        /// <summary>
        /// Gets the text decorations applied to the glyph.
        /// </summary>
        public TextDecorations TextDecorations { get; }

        /// <summary>
        /// Performs a semi-deep clone (FontMetrics are not cloned) for rendering
        /// This allows caching the original in the font metrics.
        /// </summary>
        /// <param name="textRun">The current text run this glyph belongs to.</param>
        /// <returns>The new <see cref="GlyphMetrics"/>.</returns>
        internal abstract GlyphMetrics CloneForRendering(TextRun textRun);

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
            Vector2 loc = (new Vector2(bounds.Min.X, bounds.Max.Y) + this.Offset) * scale * YInverter;
            loc += origin;

            return new FontRectangle(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="renderer">The surface renderer.</param>
        /// <param name="location">The location representing offset of the glyph outer bounds relative to the origin.</param>
        /// <param name="offset">The offset of the glyph vector relative to the top-left position of the glyph advance.</param>
        /// <param name="options">The options used to influence the rendering of this glyph.</param>
        internal abstract void RenderTo(IGlyphRenderer renderer, Vector2 location, Vector2 offset, TextOptions options);

        internal void RenderDecorationsTo(IGlyphRenderer renderer, Vector2 location, LayoutMode layoutMode, bool rotated, Matrix3x2 transform, float scaledPPEM)
        {
            bool isVerticalLayout = layoutMode.IsVertical() || layoutMode.IsVerticalMixed();
            bool ishorizontalGlyph = layoutMode.IsHorizontal() || rotated;
            (Vector2 Start, Vector2 End, float Thickness) GetEnds(TextDecorations decorations, float thickness, float decoratorPosition)
            {
                // For vertical layout we need to draw a vertical line.
                if (isVerticalLayout)
                {
                    float length = this.AdvanceHeight;
                    if (length == 0)
                    {
                        return (Vector2.Zero, Vector2.Zero, 0);
                    }

                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;

                    // Undo the vertical offset applied when laying out the text.
                    Vector2 scaledOffset = (this.Offset + new Vector2(decoratorPosition, 0)) * scale;

                    length *= scale.Y;
                    thickness *= scale.X;

                    Vector2 tl = new(scaledOffset.X, scaledOffset.Y);
                    Vector2 tr = new(scaledOffset.X + thickness, scaledOffset.Y);
                    Vector2 bl = new(scaledOffset.X, scaledOffset.Y + length);

                    thickness = tr.X - tl.X;

                    // Horizontally offset the line to the correct horizontal position
                    // based upon which side drawing occurs of the line.
                    float m = decorations switch
                    {
                        TextDecorations.Strikeout => .5F,
                        TextDecorations.Overline => 3,
                        _ => 1,
                    };

                    // Account for any future pixel clamping.
                    scaledOffset = new Vector2(thickness * m, 0) + location;
                    tl += scaledOffset;
                    bl += scaledOffset;

                    return (tl, bl, thickness);
                }
                else
                {
                    float length = this.AdvanceWidth;
                    if (length == 0)
                    {
                        return (Vector2.Zero, Vector2.Zero, 0);
                    }

                    Vector2 scale = new Vector2(scaledPPEM) / this.ScaleFactor;
                    Vector2 scaledOffset = (this.Offset + new Vector2(0, decoratorPosition)) * scale;

                    length *= scale.X;
                    thickness *= scale.Y;

                    Vector2 tl = new(scaledOffset.X, scaledOffset.Y);
                    Vector2 tr = new(scaledOffset.X + length, scaledOffset.Y);
                    Vector2 bl = new(scaledOffset.X, scaledOffset.Y + thickness);

                    thickness = bl.Y - tl.Y;
                    tl = (Vector2.Transform(tl, transform) * YInverter) + location;
                    tr = (Vector2.Transform(tr, transform) * YInverter) + location;

                    return (tl, tr, thickness);
                }
            }

            void SetDecoration(TextDecorations decorations, float thickness, float position)
            {
                (Vector2 start, Vector2 end, float calcThickness) = GetEnds(decorations, thickness, position);
                if (calcThickness != 0)
                {
                    renderer.SetDecoration(decorations, start, end, calcThickness);
                }
            }

            // Allow the renderer to override the decorations to attach.
            // Use CSS values to match vertical text and missing metrics.
            TextDecorations decorations = renderer.EnabledDecorations();
            if ((decorations & TextDecorations.Underline) == TextDecorations.Underline)
            {
                SetDecoration(TextDecorations.Underline, this.FontMetrics.UnderlineThickness, ishorizontalGlyph ? this.FontMetrics.UnderlinePosition : 0);
            }

            if ((decorations & TextDecorations.Strikeout) == TextDecorations.Strikeout)
            {
                SetDecoration(TextDecorations.Strikeout, this.FontMetrics.StrikeoutSize, ishorizontalGlyph ? this.FontMetrics.StrikeoutPosition : this.FontMetrics.UnitsPerEm * .5F);
            }

            if ((decorations & TextDecorations.Overline) == TextDecorations.Overline)
            {
                // There's no built in metrics for overline thickness so use underline.
                SetDecoration(TextDecorations.Overline, this.FontMetrics.UnderlineThickness, ishorizontalGlyph ? this.FontMetrics.HorizontalMetrics.Ascender : this.FontMetrics.UnitsPerEm);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the specified code point should be skipped when rendering.
        /// </summary>
        /// <param name="codePoint">The code point.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool ShouldSkipGlyphRendering(CodePoint codePoint)
            => UnicodeUtility.IsDefaultIgnorableCodePoint((uint)codePoint.Value) && !ShouldRenderWhiteSpaceOnly(codePoint);

        /// <summary>
        /// Gets a value indicating whether the specified code point should be rendered as a white space only.
        /// </summary>
        /// <param name="codePoint">The code point.</param>
        /// <returns>The <see cref="bool"/>.</returns>
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

        /// <summary>
        /// Returns the size to render/measure the glyph based on the given size and resolution in px units.
        /// </summary>
        /// <param name="pointSize">The font size in pt units.</param>
        /// <param name="dpi">The DPI (Dots Per Inch) to render/measure the glyph at</param>
        /// <returns>The <see cref="float"/>.</returns>
        protected float GetScaledSize(float pointSize, float dpi)
        {
            float scaledPPEM = dpi * pointSize;
            bool forcePPEMToInt = (this.FontMetrics.HeadFlags & HeadTable.HeadFlags.ForcePPEMToInt) != 0;

            if (forcePPEMToInt)
            {
                scaledPPEM = MathF.Round(scaledPPEM);
            }

            return scaledPPEM;
        }

        /// <summary>
        /// Gets the rotation matrix for the glyph based on the layout mode.
        /// </summary>
        /// <param name="layoutMode">The layout mode.</param>
        /// <param name="matrix">The rotation matrix.</param>
        /// <returns>The<see cref="bool"/>.</returns>
        internal bool TryGetRotationMatrix(LayoutMode layoutMode, out Matrix3x2 matrix)
        {
            if (layoutMode.IsVerticalMixed() && CodePoint.GetVerticalOrientationType(this.CodePoint) is VerticalOrientationType.Rotate or VerticalOrientationType.TransformRotate)
            {
                // Rotate 90 degrees clockwise.
                matrix = Matrix3x2.CreateRotation(-MathF.PI / 2F);
                return true;
            }

            matrix = Matrix3x2.Identity;
            return false;
        }

        /// <summary>
        /// Gets the rotation matrix for the glyph based on the layout mode.
        /// </summary>
        /// <param name="layoutMode">The layout mode.</param>
        /// <param name="matrix">The rotation matrix.</param>
        /// <returns>The<see cref="bool"/>.</returns>
        internal bool TryGetDecorationRotationMatrix(LayoutMode layoutMode, out Matrix3x2 matrix)
        {
            if (layoutMode.IsVerticalMixed() || layoutMode.IsVertical())
            {
                // Rotate 90 degrees clockwise.
                // We use negative as the values are mirrored.
                matrix = Matrix3x2.CreateRotation(-MathF.PI / 2F);
                return true;
            }

            matrix = Matrix3x2.Identity;
            return false;
        }
    }
}
