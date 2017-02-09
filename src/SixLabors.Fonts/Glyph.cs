using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class Glyph
    {
        private readonly Vector2[] controlPoints;
        private readonly bool[] onCurves;
        private readonly ushort[] endPoints;
        private ushort index;

        internal Glyph(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds, ushort advanceWidth, ushort index)
        {
            this.controlPoints = controlPoints;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.Bounds = bounds;
            this.AdvanceWidth = advanceWidth;
            this.Index = index;
        }

        public Bounds Bounds { get; }

        /// <summary>
        /// Gets the width of the advance.
        /// </summary>
        /// <value>
        /// The width of the advance.
        /// </value>
        public ushort AdvanceWidth { get; }

        internal ushort Index { get; }

        /// <summary>
        /// Renders the glyph to the render surface in font units relative to a bottom left origin at (0,0)
        /// </summary>
        /// <param name="surface">The surface.</param>
        public void RenderTo(IRenderSurface surface)
        {

            int npoints = controlPoints.Length;
            int startContour = 0;
            int cpoint_index = 0;

            surface.BeginGlyph();


            Vector2 lastMove = Vector2.Zero;

            int controlPointCount = 0;
            for (int i = 0; i < endPoints.Length; i++)
            {
                int nextContour = endPoints[startContour] + 1;
                bool isFirstPoint = true;
                Vector2 secondControlPoint = new Vector2();
                Vector2 thirdControlPoint = new Vector2();
                bool justFromCurveMode = false;

                for (; cpoint_index < nextContour; ++cpoint_index)
                {

                    var vpoint = controlPoints[cpoint_index];
                    if (onCurves[cpoint_index])
                    {
                        //on curve
                        if (justFromCurveMode)
                        {
                            switch (controlPointCount)
                            {
                                case 1:
                                    {
                                        surface.QuadraticBezierTo(
                                            secondControlPoint,
                                            vpoint);
                                    }
                                    break;
                                case 2:
                                    {
                                        surface.CubicBezierTo(
                                                secondControlPoint,
                                                thirdControlPoint,
                                                vpoint);
                                    }
                                    break;
                                default:
                                    {
                                        throw new NotSupportedException();
                                    }
                            }
                            controlPointCount = 0;
                            justFromCurveMode = false;
                        }
                        else
                        {
                            if (isFirstPoint)
                            {
                                isFirstPoint = false;
                                lastMove = vpoint;
                                surface.MoveTo(lastMove);
                            }
                            else
                            {
                                surface.LineTo(vpoint);
                            }
                        }
                    }
                    else
                    {
                        switch (controlPointCount)
                        {
                            case 0:
                                {
                                    secondControlPoint = vpoint;
                                }
                                break;
                            case 1:
                                {
                                    //we already have prev second control point
                                    //so auto calculate line to 
                                    //between 2 point
                                    Vector2 mid = (secondControlPoint + vpoint)/2;
                                    surface.QuadraticBezierTo(
                                        secondControlPoint,
                                        mid);
                                    controlPointCount--;
                                    secondControlPoint = vpoint;
                                }
                                break;
                            default:
                                {
                                    throw new NotSupportedException("Too many control points");
                                }
                        }

                        controlPointCount++;
                        justFromCurveMode = true;
                    }
                }
                //--------
                //close figure
                //if in curve mode
                if (justFromCurveMode)
                {
                    switch (controlPointCount)
                    {
                        case 0: break;
                        case 1:
                            {
                                surface.QuadraticBezierTo(
                                    secondControlPoint,
                                    lastMove);
                            }
                            break;
                        case 2:
                            {
                                surface.CubicBezierTo(
                                    secondControlPoint,
                                    thirdControlPoint,
                                    lastMove);
                            }
                            break;
                        default:
                            { throw new NotSupportedException("Too many control points"); }
                    }
                    justFromCurveMode = false;
                    controlPointCount = 0;
                }
                surface.EndFigure();
                //--------                   
                startContour++;
            }
            surface.EndGlyph();
        }
    }
}
