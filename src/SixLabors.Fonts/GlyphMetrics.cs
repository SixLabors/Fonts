// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
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

        internal GlyphMetrics(
            FontMetrics font,
            CodePoint codePoint,
            GlyphVector vector,
            ushort advanceWidth,
            ushort advanceHeight,
            short leftSideBearing,
            short topSideBearing,
            ushort glyphId,
            GlyphType glyphType = GlyphType.Standard,
            GlyphColor? glyphColor = null)
        {
            this.FontMetrics = font;
            this.CodePoint = codePoint;
            this.vector = vector;

            this.AdvanceWidth = advanceWidth;
            this.AdvanceHeight = advanceHeight;
            this.GlyphId = glyphId;

            this.Width = this.Bounds.Max.X - this.Bounds.Min.X;
            this.Height = this.Bounds.Max.Y - this.Bounds.Min.Y;
            this.GlyphType = glyphType;
            this.LeftSideBearing = leftSideBearing;
            this.TopSideBearing = topSideBearing;
            this.ScaleFactor = this.UnitsPerEm * 72F;
            this.GlyphColor = glyphColor;
        }

        internal GlyphMetrics(GlyphMetrics other, CodePoint codePoint)
        {
            this.FontMetrics = other.FontMetrics;
            this.CodePoint = codePoint;
            this.vector = (GlyphVector)other.vector.DeepClone();

            this.AdvanceWidth = other.AdvanceWidth;
            this.AdvanceHeight = other.AdvanceHeight;
            this.GlyphId = other.GlyphId;

            this.Width = other.Width;
            this.Height = other.Height;
            this.GlyphType = other.GlyphType;
            this.LeftSideBearing = other.LeftSideBearing;
            this.TopSideBearing = other.TopSideBearing;
            this.ScaleFactor = other.ScaleFactor;
            this.GlyphColor = other.GlyphColor;
        }

        /// <summary>
        /// Gets the font metrics.
        /// </summary>
        internal FontMetrics FontMetrics { get; }

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

        /// <inheritdoc cref="IFontMetrics.UnitsPerEm"/>
        public ushort UnitsPerEm => this.FontMetrics.UnitsPerEm;

        /// <inheritdoc cref="IFontMetrics.ScaleFactor"/>
        public float ScaleFactor { get; }

        /// <summary>
        /// Gets the points defining the shape of this glyph
        /// </summary>
        public Vector2[] ControlPoints => this.vector.ControlPoints;

        /// <summary>
        /// Gets at value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve.
        /// </summary>
        public bool[] OnCurves => this.vector.OnCurves;

        /// <summary>
        /// Gets the end points
        /// </summary>
        public ushort[] EndPoints => this.vector.EndPoints;

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        internal Bounds Bounds => this.vector.Bounds;

        /// <summary>
        /// Gets the glyph Id.
        /// </summary>
        internal ushort GlyphId { get; }

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

        internal FontRectangle BoundingBox(Vector2 origin, float scaledPointSize)
        {
            Vector2 size = this.Bounds.Size() * scaledPointSize / this.ScaleFactor;
            Vector2 loc = new Vector2(this.Bounds.Min.X, this.Bounds.Max.Y) * scaledPointSize / this.ScaleFactor * MirrorScale;

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
        public void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, RendererOptions options)
        {
            float dpi = options.Dpi;
            location *= dpi;

            float scaledPoint = dpi * pointSize;

            FontRectangle box = this.BoundingBox(location, scaledPoint);

            var parameters = new GlyphRendererParameters(this, pointSize, dpi);

            if (surface.BeginGlyph(box, parameters))
            {
                if (this.GlyphColor.HasValue && surface is IColorGlyphRenderer colorSurface)
                {
                    colorSurface.SetColor(this.GlyphColor.Value);
                }

                if (!this.scaledVector.TryGetValue(scaledPoint, out GlyphVector scaledVector))
                {
                    scaledVector = GlyphVector.Scale(this.vector, scaledPoint / this.ScaleFactor);
                    this.scaledVector[scaledPoint] = scaledVector;
                }

                if (options.ApplyHinting)
                {
                    this.FontMetrics.ApplyHinting(scaledVector, pointSize * dpi / 72, this.GlyphId);
                }

                int endOfContour = -1;
                for (int i = 0; i < this.vector.EndPoints.Length; i++)
                {
                    surface.BeginFigure();
                    int startOfContour = endOfContour + 1;
                    endOfContour = this.vector.EndPoints[i];

                    Vector2 prev;
                    Vector2 curr = this.GetPoint(ref scaledVector, endOfContour) + location;
                    Vector2 next = this.GetPoint(ref scaledVector, startOfContour) + location;

                    if (this.vector.OnCurves[endOfContour])
                    {
                        surface.MoveTo(curr);
                    }
                    else
                    {
                        if (this.vector.OnCurves[startOfContour])
                        {
                            surface.MoveTo(next);
                        }
                        else
                        {
                            // If both first and last points are off-curve, start at their middle.
                            Vector2 startPoint = (curr + next) / 2;
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
                        next = this.GetPoint(ref scaledVector, nextIndex) + location;

                        if (this.vector.OnCurves[currentIndex])
                        {
                            // This is a straight line.
                            surface.LineTo(curr);
                        }
                        else
                        {
                            Vector2 prev2 = prev;
                            Vector2 next2 = next;

                            if (!this.vector.OnCurves[prevIndex])
                            {
                                prev2 = (curr + prev) / 2;
                                surface.LineTo(prev2);
                            }

                            if (!this.vector.OnCurves[nextIndex])
                            {
                                next2 = (curr + next) / 2;
                            }

                            surface.LineTo(prev2);
                            surface.QuadraticBezierTo(curr, next2);
                        }
                    }

                    surface.EndFigure();
                }
            }

            surface.EndGlyph();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 GetPoint(ref GlyphVector glyphVector, int pointIndex)

            // Scale each point as we go, we will now have the correct relative point size
            => MirrorScale * glyphVector.ControlPoints[pointIndex];

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
