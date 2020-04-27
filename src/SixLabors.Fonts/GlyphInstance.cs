// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    public partial class GlyphInstance
    {
        private static readonly Vector2 Scale = new Vector2(1, -1);

        private readonly ushort sizeOfEm;
        private readonly GlyphVector vector;
        private readonly short leftSideBearing;
        private readonly float scaleFactor;

        internal GlyphInstance(FontInstance font, GlyphVector vector, ushort advanceWidth, short leftSideBearing, ushort sizeOfEm, ushort index, GlyphType glyphType = GlyphType.Standard, GlyphColor? glyphColor = null)
        {
            this.Font = font;
            this.sizeOfEm = sizeOfEm;
            this.vector = vector;

            this.AdvanceWidth = advanceWidth;
            this.Index = index;
            this.Height = sizeOfEm - this.Bounds.Min.Y;
            this.GlyphType = glyphType;
            this.leftSideBearing = leftSideBearing;
            this.scaleFactor = (float)(this.sizeOfEm * 72f);
            this.GlyphColor = glyphColor;
        }

        /// <summary>
        /// Gets the Font.
        /// </summary>
        /// <value>
        /// The Font.
        /// </value>
        internal FontInstance Font { get; }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        internal Bounds Bounds => this.vector.Bounds;

        /// <summary>
        /// Gets the width of the advance.
        /// </summary>
        /// <value>
        /// The width of the advance.
        /// </value>
        public ushort AdvanceWidth { get; }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>
        /// The height.
        /// </value>
        public float Height { get; }

        /// <summary>
        /// Gets a value indicating the type of glyph instance this is.
        /// </summary>
        /// <value>
        /// The type of this glyph
        /// </value>
        public GlyphType GlyphType { get; }

        /// <summary>
        /// Gets the color of this glyph
        /// </summary>
        /// <value>
        /// The color of the glyph when the <see cref="GlyphType"/> is <see cref="GlyphType.ColrLayer"/>
        /// </value>
        public GlyphColor? GlyphColor { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        internal ushort Index { get; }

        /// <summary>
        /// Gets the size of the EM
        /// </summary>
        public ushort SizeOfEm => this.sizeOfEm;

        /// <summary>
        /// Gets the points defining the shape of this glyph
        /// </summary>
        public Vector2[] ControlPoints => this.vector.ControlPoints;

        /// <summary>
        /// Gets wether or not the corresponding control point is on a curve
        /// </summary>
        public bool[] OnCurves => this.vector.OnCurves;

        /// <summary>
        /// Gets the end points
        /// </summary>
        public ushort[] EndPoints => this.vector.EndPoints;

        /// <summary>
        /// Gets the distance from the bounding box start
        /// </summary>
        public short LeftSideBearing => this.leftSideBearing;

        /// <summary>
        /// Gets the scale factor that is applied to the glyph
        /// </summary>
        public float ScaleFactor => this.scaleFactor;

        internal FontRectangle BoundingBox(Vector2 origin, Vector2 scaledPointSize)
        {
            Vector2 size = (this.Bounds.Size() * scaledPointSize) / this.scaleFactor;
            Vector2 loc = ((new Vector2(this.Bounds.Min.X, this.Bounds.Max.Y) * scaledPointSize) / this.scaleFactor) * Scale;

            loc = origin + loc;

            return new FontRectangle(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="lineHeight">The lineHeight the current glyph was draw agains to offset topLeft while calling out to IGlyphRenderer.</param>
        /// <exception cref="NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, Vector2 dpi, float lineHeight)
        {
            location = location * dpi;

            Vector2 firstPoint = Vector2.Zero;

            Vector2 scaledPoint = dpi * pointSize;

            FontRectangle box = this.BoundingBox(location, scaledPoint);

            var paramaters = new GlyphRendererParameters(this, pointSize, dpi);

            if (surface.BeginGlyph(box, paramaters))
            {
                if (this.GlyphColor.HasValue && surface is IColorGlyphRenderer colorSurface)
                {
                    colorSurface.SetColor(this.GlyphColor.Value);
                }

                int endOfContor = -1;
                for (int i = 0; i < this.vector.EndPoints.Length; i++)
                {
                    surface.BeginFigure();
                    int startOfContor = endOfContor + 1;
                    endOfContor = this.vector.EndPoints[i];

                    Vector2 prev = Vector2.Zero;
                    Vector2 curr = this.GetPoint(ref scaledPoint, endOfContor) + location;
                    Vector2 next = this.GetPoint(ref scaledPoint, startOfContor) + location;

                    if (this.vector.OnCurves[endOfContor])
                    {
                        surface.MoveTo(curr);
                    }
                    else
                    {
                        if (this.vector.OnCurves[startOfContor])
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

                    int length = (endOfContor - startOfContor) + 1;
                    for (int p = 0; p < length; p++)
                    {
                        prev = curr;
                        curr = next;
                        int currentIndex = startOfContor + p;
                        int nextIndex = startOfContor + ((p + 1) % length);
                        int prevIndex = startOfContor + (((length + p) - 1) % length);
                        next = this.GetPoint(ref scaledPoint, nextIndex) + location;

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
        private Vector2 GetPoint(ref Vector2 scaledPoint, int pointIndex)
        {
            Vector2 point = Scale * ((this.vector.ControlPoints[pointIndex] * scaledPoint) / this.scaleFactor); // scale each point as we go, w will now have the correct relative point size

            return point;
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
                case 0: break;
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
            {
                this.Count = 0;
            }
        }
    }
}
