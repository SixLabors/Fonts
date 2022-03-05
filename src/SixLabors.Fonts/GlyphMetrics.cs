// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.General.Glyphs;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a glyph metric from a particular font face.
    /// </summary>
    public class GlyphMetrics
    {
        private static readonly Vector2 MirrorScale = new(1, -1);
        private GlyphVector vector;
        private readonly Dictionary<float, GlyphVector> scaledVector = new();
        private Vector2 offset = Vector2.Zero;
        private TextRun? textRun;

        internal GlyphMetrics(
            StreamFontMetrics font,
            CodePoint codePoint,
            GlyphVector vector,
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
            this.vector = vector;

            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.GlyphId = glyphId;

            Bounds bounds = this.vector.GetBounds();
            this.Width = bounds.Max.X - bounds.Min.X;
            this.Height = bounds.Max.Y - bounds.Min.Y;
            this.GlyphType = glyphType;
            this.LeftSideBearing = leftSideBearing;
            this.TopSideBearing = topSideBearing;
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
        /// Gets the top side bearing for vertical layout, expressed in font units.
        /// </summary>
        public short TopSideBearing { get; }

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
        public Vector2 ScaleFactor { get; private set; }

        /// <summary>
        /// Gets the glyph Id.
        /// </summary>
        internal ushort GlyphId { get; }

        /// <summary>
        /// Performs a semi-deep clone (FontMetrics are not cloned) for rendering
        /// This allows caching the original in the font metrics.
        /// </summary>
        /// <param name="other">The original glyph metrics.</param>
        /// <param name="textRun">The text run this glyph is a member of.</param>
        /// <param name="codePoint">The codepoint for this glyph.</param>
        /// <returns>The new <see cref="GlyphMetrics"/>.</returns>
        internal static GlyphMetrics CloneForRendering(GlyphMetrics other, TextRun textRun, CodePoint codePoint)
        {
            StreamFontMetrics fontMetrics = other.FontMetrics;
            Vector2 offset = other.offset;
            Vector2 scaleFactor = other.ScaleFactor;
            if (textRun.TextAttributes.HasFlag(TextAttribute.Subscript))
            {
                float units = other.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SubscriptXSize / units, fontMetrics.SubscriptYSize / units);
                offset = new(other.FontMetrics.SubscriptXOffset, other.FontMetrics.SubscriptYOffset);
            }
            else if (textRun.TextAttributes.HasFlag(TextAttribute.Superscript))
            {
                float units = other.UnitsPerEm;
                scaleFactor /= new Vector2(fontMetrics.SuperscriptXSize / units, fontMetrics.SuperscriptYSize / units);
                offset = new(fontMetrics.SuperscriptXOffset, -fontMetrics.SuperscriptYOffset);
            }

            GlyphMetrics metrics = new(
                fontMetrics,
                codePoint,
                GlyphVector.DeepClone(other.vector),
                other.AdvanceWidth,
                other.AdvanceHeight,
                other.LeftSideBearing,
                other.TopSideBearing,
                other.UnitsPerEm,
                other.GlyphId,
                other.GlyphType,
                other.GlyphColor);

            metrics.offset = offset;
            metrics.ScaleFactor = scaleFactor;
            metrics.textRun = textRun;

            return metrics;
        }

        /// <summary>
        /// Gets the outline for the current glyph.
        /// </summary>
        /// <returns>The <see cref="GlyphOutline"/>.</returns>
        public GlyphOutline GetOutline() => this.vector.GetOutline();

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        internal Bounds GetBounds() => this.vector.GetBounds();

        /// <summary>
        /// Apply an offset to the glyph.
        /// </summary>
        /// <param name="x">The x-offset.</param>
        /// <param name="y">The y-offset.</param>
        internal void ApplyOffset(short x, short y)
            => this.vector = GlyphVector.Translate(this.vector, x, y);

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
            Bounds bounds = this.GetBounds();
            Vector2 size = bounds.Size() * scale;
            Vector2 loc = (new Vector2(bounds.Min.X, bounds.Max.Y) + this.offset) * scale * MirrorScale;
            loc = origin + loc;

            return new FontRectangle(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="options">The options used to influence the rendering of this glyph.</param>
        /// <exception cref="NotSupportedException">Too many control points</exception>
        internal void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, TextOptions options)
        {
            float dpi = options.Dpi;
            location *= dpi;
            float scaledPoint = dpi * pointSize;
            FontRectangle box = this.GetBoundingBox(location, scaledPoint);

            // TextRun is never null here as rendering is only accessable via a Glyph which
            // uses the cloned metrics instance.
            var parameters = new GlyphRendererParameters(this, this.textRun!, pointSize, dpi);

            if (surface.BeginGlyph(box, parameters))
            {
                if (!CodePoint.IsWhiteSpace(this.CodePoint))
                {
                    if (this.GlyphColor.HasValue && surface is IColorGlyphRenderer colorSurface)
                    {
                        colorSurface.SetColor(this.GlyphColor.Value);
                    }

                    if (!this.scaledVector.TryGetValue(scaledPoint, out GlyphVector scaledVector))
                    {
                        // Scale and translate the glyph
                        Vector2 scale = new Vector2(scaledPoint) / this.ScaleFactor;
                        var transform = Matrix3x2.CreateScale(scale);
                        transform.Translation = this.offset * scale * MirrorScale;
                        scaledVector = GlyphVector.Transform(this.vector, transform);
                        this.scaledVector[scaledPoint] = scaledVector;
                    }

                    if (options.ApplyHinting)
                    {
                        this.FontMetrics.ApplyHinting(scaledVector, pointSize * dpi / 72, this.GlyphId);
                    }

                    GlyphOutline outline = scaledVector.GetOutline();
                    ReadOnlySpan<Vector2> controlPoints = outline.ControlPoints.Span;
                    ReadOnlySpan<ushort> endPoints = outline.EndPoints.Span;
                    ReadOnlySpan<bool> onCurves = outline.OnCurves.Span;

                    int endOfContour = -1;
                    for (int i = 0; i < outline.EndPoints.Length; i++)
                    {
                        surface.BeginFigure();
                        int startOfContour = endOfContour + 1;
                        endOfContour = endPoints[i];

                        Vector2 prev;
                        Vector2 curr = (MirrorScale * controlPoints[endOfContour]) + location;
                        Vector2 next = (MirrorScale * controlPoints[startOfContour]) + location;

                        if (onCurves[endOfContour])
                        {
                            surface.MoveTo(curr);
                        }
                        else
                        {
                            if (onCurves[startOfContour])
                            {
                                surface.MoveTo(next);
                            }
                            else
                            {
                                // If both first and last points are off-curve, start at their middle.
                                Vector2 startPoint = (curr + next) * .5F;
                                surface.MoveTo(startPoint);
                            }
                        }

                        int length = endOfContour - startOfContour + 1;
                        for (int p = 0; p < length; p++)
                        {
                            prev = curr;
                            curr = next;
                            int currentIndex = startOfContour + p;
                            int nextIndex = startOfContour + ((p + 1) % length);
                            int prevIndex = startOfContour + ((length + p - 1) % length);
                            next = (MirrorScale * controlPoints[nextIndex]) + location;

                            if (onCurves[currentIndex])
                            {
                                // This is a straight line.
                                surface.LineTo(curr);
                            }
                            else
                            {
                                Vector2 prev2 = prev;
                                Vector2 next2 = next;

                                if (!onCurves[prevIndex])
                                {
                                    prev2 = (curr + prev) * .5F;
                                    surface.LineTo(prev2);
                                }

                                if (!onCurves[nextIndex])
                                {
                                    next2 = (curr + next) * .5F;
                                }

                                surface.LineTo(prev2);
                                surface.QuadraticBezierTo(curr, next2);
                            }
                        }

                        surface.EndFigure();
                    }
                }

                void DrawLine(float thickness, float position)
                {
                    surface.BeginFigure();

                    float height = thickness;
                    float top = position - (height * .5F);
                    float bottom = top + height;

                    Vector2 scale = new Vector2(scaledPoint) / this.ScaleFactor * MirrorScale;
                    Vector2 offset = location + (this.offset * scale * MirrorScale);

                    Vector2 tl = (new Vector2(-this.LeftSideBearing, top) * scale) + offset;
                    Vector2 tr = (new Vector2(this.AdvanceWidth + this.LeftSideBearing, top) * scale) + offset;
                    Vector2 br = (new Vector2(this.AdvanceWidth + this.LeftSideBearing, bottom) * scale) + offset;
                    Vector2 bl = (new Vector2(-this.LeftSideBearing, bottom) * scale) + offset;

                    tl.Y = MathF.Ceiling(tl.Y);
                    tr.Y = MathF.Ceiling(tr.Y);
                    br.Y = MathF.Ceiling(br.Y);
                    bl.Y = MathF.Ceiling(bl.Y);

                    surface.MoveTo(tl);
                    surface.LineTo(bl);
                    surface.LineTo(br);
                    surface.LineTo(tr);

                    surface.EndFigure();
                }

                // Add underline and stroke.
                // TextRun is never null here as rendering is only accessable via a Glyph which
                // uses the cloned metrics instance.
                if ((this.textRun!.TextAttributes & TextAttribute.Underline) == TextAttribute.Underline)
                {
                    DrawLine(this.FontMetrics.UnderlineThickness, this.FontMetrics.UnderlinePosition);
                }

                if ((this.textRun!.TextAttributes & TextAttribute.Strikethrough) == TextAttribute.Strikethrough)
                {
                    DrawLine(this.FontMetrics.StrikeoutSize, this.FontMetrics.StrikeoutPosition);
                }
            }

            surface.EndGlyph();
        }

        private static void AlignToGrid(ref Vector2 point)
        {
            var floorPoint = new Vector2(MathF.Floor(point.X), MathF.Floor(point.Y));
            Vector2 decimalPart = point - floorPoint;

            decimalPart.X = decimalPart.X < 0.5f ? 0 : 1f;
            decimalPart.Y = decimalPart.Y < 0.5f ? 0 : 1f;

            point = floorPoint + decimalPart;
        }

        private static ControlPointCollection DrawPoints(IGlyphRenderer surface, ControlPointCollection points, Vector2 point)
        {
            switch (points.Count)
            {
                case 0:
                    break;
                case 1:
                    surface.QuadraticBezierTo(
                        points.SecondControlPoint,
                        point);
                    break;
                case 2:
                    surface.CubicBezierTo(
                        points.SecondControlPoint,
                        points.ThirdControlPoint,
                        point);
                    break;
                default:
                    throw new NotSupportedException("Too many control points");
            }

            points.Clear();
            return points;
        }

        private struct ControlPointCollection
        {
            public Vector2 SecondControlPoint;
            public Vector2 ThirdControlPoint;
            public int Count;

            public void Add(Vector2 point)
            {
                switch (this.Count++)
                {
                    case 0:
                        this.SecondControlPoint = point;
                        break;
                    case 1:
                        this.ThirdControlPoint = point;
                        break;
                    default:
                        throw new NotSupportedException("Too many control points");
                }
            }

            public void ReplaceLast(Vector2 point)
            {
                this.Count--;
                this.Add(point);
            }

            public void Clear()
                => this.Count = 0;
        }
    }
}
