// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Primitives;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    internal partial class GlyphInstance
    {
        private readonly ushort sizeOfEm;
        private readonly Vector2[] controlPoints;
        private readonly bool[] onCurves;
        private readonly ushort[] endPoints;
        private readonly short leftSideBearing;
        private readonly float scaleFactor;

        internal GlyphInstance(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds, ushort advanceWidth, short leftSideBearing, ushort sizeOfEm, ushort index)
        {
            this.sizeOfEm = sizeOfEm;
            this.controlPoints = controlPoints;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.Bounds = bounds;
            this.AdvanceWidth = advanceWidth;
            this.Index = index;
            this.Height = sizeOfEm - this.Bounds.Min.Y;

            this.leftSideBearing = leftSideBearing;
            this.scaleFactor = (float)(this.sizeOfEm * 72f);
        }

        /// <summary>
        /// Gets the bounds.
        /// </summary>
        /// <value>
        /// The bounds.
        /// </value>
        internal Bounds Bounds { get; }

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
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        internal ushort Index { get; }

        private static readonly Vector2 scale = new Vector2(1, -1);

        internal RectangleF BoundingBox(Vector2 origin, Vector2 scaledPointSize)
        {
            var size = (this.Bounds.Size() * scaledPointSize) / this.scaleFactor;
            var loc = ((new Vector2(this.Bounds.Min.X, this.Bounds.Max.Y) * scaledPointSize) / this.scaleFactor) * scale;

            loc = origin + loc;

            return new RectangleF(loc.X, loc.Y, size.X, size.Y);
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="location">The location.</param>
        /// <param name="dpi">The dpi.</param>
        /// <param name="lineHeight">The lineHeight the current glyph was draw agains to offset topLeft while calling out to IGlyphRenderer.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, Vector2 dpi, float lineHeight)
        {
            location = location * dpi;

            Vector2 firstPoint = Vector2.Zero;

            var scaledPoint = dpi * pointSize;

            var box = this.BoundingBox(location, scaledPoint);

            var hash = HashHelpers.Combine(this.GetHashCode(), pointSize.GetHashCode());
            hash = HashHelpers.Combine(hash, dpi.GetHashCode());

            // (lineHeight * dpi.Y)
            if (surface.BeginGlyph(box, hash))
            {
                int startOfContor = 0;
                int endOfContor = -1;
                for (int i = 0; i < this.endPoints.Length; i++)
                {
                    surface.BeginFigure();
                    startOfContor = endOfContor + 1;
                    endOfContor = this.endPoints[i];

                    Vector2 prev = Vector2.Zero;
                    Vector2 curr = this.GetPoint(ref scaledPoint, endOfContor) + location;
                    Vector2 next = this.GetPoint(ref scaledPoint, startOfContor) + location;

                    if (this.onCurves[endOfContor])
                    {
                        surface.MoveTo(curr);
                    }
                    else
                    {
                        if (this.onCurves[startOfContor])
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

                        if (this.onCurves[currentIndex])
                        {
                            // This is a straight line.
                            surface.LineTo(curr);
                        }
                        else
                        {
                            Vector2 prev2 = prev;
                            Vector2 next2 = next;

                            if (!this.onCurves[prevIndex])
                            {
                                prev2 = (curr + prev) / 2;
                                surface.LineTo(prev2);
                            }

                            if (!this.onCurves[nextIndex])
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
            Vector2 point = scale * ((this.controlPoints[pointIndex] * scaledPoint) / this.scaleFactor); // scale each point as we go, w will now have the correct relative point size

            return point;
        }

        private static void AlignToGrid(ref Vector2 point)
        {
            Vector2 floorPoint = new Vector2(MathF.Floor(point.X), MathF.Floor(point.Y));
            Vector2 decimalPart = point - floorPoint;

            if (decimalPart.X < 0.5)
            {
                decimalPart.X = 0;
            }
            else
            {
                decimalPart.X = 1;
            }

            if (decimalPart.Y < 0.5)
            {
                decimalPart.Y = 0;
            }
            else
            {
                decimalPart.Y = 1f;
            }

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