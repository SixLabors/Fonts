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
    public class Glyph
    {
        private readonly ushort emSize;
        private readonly Vector2[] controlPoints;
        private readonly bool[] onCurves;
        private readonly ushort[] endPoints;

        internal Glyph(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds, ushort advanceWidth, ushort emSize, ushort index)
        {
            this.emSize = emSize;
            this.controlPoints = controlPoints;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.Bounds = bounds;
            this.AdvanceWidth = advanceWidth;
            this.Index = index;
            this.Height = emSize - this.Bounds.Min.Y;
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
        /// Renders to.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="dpi">The dpi.</param>
        public void RenderTo(IGlyphRender surface, float pointSize, float dpi)
        {
            this.RenderTo(surface, pointSize, new Vector2(dpi));
        }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="pointSize">Size of the point.</param>
        /// <param name="dpi">The dpi.</param>
        /// <exception cref="System.NotSupportedException">Too many control points</exception>
        public void RenderTo(IGlyphRender surface, float pointSize, Vector2 dpi)
        {
            int startContour = 0;
            int pointIndex = 0;
            var scaleFactor = (float)(this.emSize * 72f);

            surface.BeginGlyph();

            Vector2 lastMove = Vector2.Zero;

            int controlPointCount = 0;
            for (int i = 0; i < this.endPoints.Length; i++)
            {
                int nextContour = this.endPoints[startContour] + 1;
                bool isFirstPoint = true;
                Vector2 secondControlPoint = default(Vector2);
                Vector2 thirdControlPoint = default(Vector2);
                bool justFromCurveMode = false;

                for (; pointIndex < nextContour; ++pointIndex)
                {
                    var point = (this.controlPoints[pointIndex] * pointSize * dpi) / scaleFactor; // scale each point as we go, w will now have the correct relative point size

                    if (this.onCurves[pointIndex])
                    {
                        // on curve
                        if (justFromCurveMode)
                        {
                            switch (controlPointCount)
                            {
                                case 1:
                                    surface.QuadraticBezierTo(
                                        secondControlPoint,
                                        point);
                                    break;
                                case 2:
                                    surface.CubicBezierTo(
                                            secondControlPoint,
                                            thirdControlPoint,
                                            point);
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            controlPointCount = 0;
                            justFromCurveMode = false;
                        }
                        else
                        {
                            if (isFirstPoint)
                            {
                                isFirstPoint = false;
                                lastMove = point;
                                surface.MoveTo(lastMove);
                            }
                            else
                            {
                                surface.LineTo(point);
                            }
                        }
                    }
                    else
                    {
                        switch (controlPointCount)
                        {
                            case 0:
                                secondControlPoint = point;
                                break;
                            case 1:
                                // we already have prev second control point
                                // so auto calculate line to
                                // between 2 point
                                Vector2 mid = (secondControlPoint + point) / 2;
                                surface.QuadraticBezierTo(
                                    secondControlPoint,
                                    mid);
                                controlPointCount--;
                                secondControlPoint = point;
                                break;
                            default:
                                throw new NotSupportedException("Too many control points");
                        }

                        controlPointCount++;
                        justFromCurveMode = true;
                    }
                }

                // close figure
                // if in curve mode
                if (justFromCurveMode)
                {
                    switch (controlPointCount)
                    {
                        case 0: break;
                        case 1:
                            surface.QuadraticBezierTo(
                                secondControlPoint,
                                lastMove);
                            break;
                        case 2:
                            surface.CubicBezierTo(
                                secondControlPoint,
                                thirdControlPoint,
                                lastMove);
                            break;
                        default:
                            throw new NotSupportedException("Too many control points");
                    }

                    justFromCurveMode = false;
                    controlPointCount = 0;
                }

                surface.EndFigure();
                startContour++;
            }

            surface.EndGlyph();
        }
    }
}