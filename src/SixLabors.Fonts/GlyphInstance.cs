using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// A glyph from a particular font face.
    /// </summary>
    internal class GlyphInstance
    {
        private readonly ushort sizeOfEm;
        private readonly Vector2[] controlPoints;
        private readonly bool[] onCurves;
        private readonly ushort[] endPoints;

        internal GlyphInstance(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds, ushort advanceWidth, ushort sizeOfEm, ushort index)
        {
            this.sizeOfEm = sizeOfEm;
            this.controlPoints = controlPoints;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.Bounds = bounds;
            this.AdvanceWidth = advanceWidth;
            this.Index = index;
            this.Height = sizeOfEm - this.Bounds.Min.Y;
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

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="dpi">The dpi.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRenderer surface, float pointSize, Vector2 location, Vector2 dpi)
        {
            location = location * dpi;

            int pointIndex = 0;
            var scaleFactor = (float)(this.sizeOfEm * 72f);

            surface.BeginGlyph();

            Vector2 firstPoint = Vector2.Zero;

            for (int i = 0; i < this.endPoints.Length; i++)
            {
                int nextContour = this.endPoints[i] + 1;
                bool isFirstPoint = true;
                ControlPointCollection points = new ControlPointCollection();
                bool justFromCurveMode = false;

                for (; pointIndex < nextContour; ++pointIndex)
                {
                    var point = location + ((this.controlPoints[pointIndex] * pointSize * dpi) / scaleFactor); // scale each point as we go, w will now have the correct relative point size

                    if (this.onCurves[pointIndex])
                    {
                        // on curve
                        if (justFromCurveMode)
                        {
                            points = DrawPoints(surface, points, point);
                        }
                        else
                        {
                            if (isFirstPoint)
                            {
                                isFirstPoint = false;
                                firstPoint = point;
                                surface.MoveTo(firstPoint);
                            }
                            else
                            {
                                surface.LineTo(point);
                            }
                        }
                    }
                    else
                    {
                        switch (points.Count)
                        {
                            case 0:
                                points.Add(point);
                                break;
                            case 1:
                                // we already have prev second control point
                                // so auto calculate line to
                                // between 2 point
                                Vector2 mid = (points.SecondControlPoint + point) / 2;
                                surface.QuadraticBezierTo(
                                    points.SecondControlPoint,
                                    mid);
                                points.SecondControlPoint = point; //replace 2nd
                                break;
                            default:
                                throw new NotSupportedException("Too many control points");
                        }
                    }
                    justFromCurveMode = !this.onCurves[pointIndex];
                }

                // close figure
                // if in curve mode
                if (justFromCurveMode)
                {
                    DrawPoints(surface, points, firstPoint);
                }

                surface.EndFigure();
            }

            surface.EndGlyph();
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
                switch (Count++)
                {
                    case 0:
                        SecondControlPoint = point;
                        break;
                    case 1:
                        ThirdControlPoint = point;
                        break;
                    default:
                        throw new NotSupportedException("Too many control points");
                }
            }
            public void ReplaceLast(Vector2 point)
            {
                Count--;
                Add(point);
            }

            public void Clear()
            {
                Count = 0;
            }
        }
    }
}