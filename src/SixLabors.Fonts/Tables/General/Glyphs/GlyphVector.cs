// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal struct GlyphVector : IDeepCloneable
    {
        private Vector2[] controlPoints;

        internal GlyphVector(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.controlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;
            this.Bounds = bounds;
        }

        private GlyphVector(GlyphVector other)
        {
            this.controlPoints = new Vector2[other.ControlPoints.Length];
            other.ControlPoints.CopyTo(this.controlPoints.AsSpan());
            this.OnCurves = new bool[other.OnCurves.Length];
            other.OnCurves.CopyTo(this.OnCurves.AsSpan());
            this.EndPoints = new ushort[other.EndPoints.Length];
            other.EndPoints.CopyTo(this.EndPoints.AsSpan());
            Bounds origBounds = other.Bounds;
            this.Bounds = new Bounds(origBounds.Min.X, origBounds.Min.Y, origBounds.Max.X, origBounds.Max.Y);
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints => this.controlPoints;

        public ushort[] EndPoints { get; }

        public bool[] OnCurves { get; }

        public Bounds Bounds { get; internal set; }

        /// <inheritdoc/>
        public IDeepCloneable DeepClone() => new GlyphVector(this);

        /// <summary>
        /// TrueType outline, offset glyph points.
        /// Change the bounding box on the current glyph.
        /// </summary>
        /// <param name="dx">The delta x.</param>
        /// <param name="dy">The delta y.</param>
        public void TtfOffsetXy(short dx, short dy)
        {
            Vector2[] glyphPoints = this.ControlPoints;
            for (int i = glyphPoints.Length - 1; i >= 0; --i)
            {
                glyphPoints[i] = Offset(glyphPoints[i], dx, dy);
            }

            Bounds orgBounds = this.Bounds;
            this.Bounds = new Bounds(
                (short)(orgBounds.Min.X + dx),
                (short)(orgBounds.Min.Y + dy),
                (short)(orgBounds.Max.X + dx),
                (short)(orgBounds.Max.Y + dy));
        }

        /// <summary>
        /// Append glyph control points to the current instance.
        /// </summary>
        /// <param name="src">The source glyph which points will be appended.</param>
        public void TtfAppendGlyph(GlyphVector src)
        {
            int destPointsCount = this.controlPoints.Length;
            Vector2[] srcPoints = src.ControlPoints;
            Array.Resize(ref this.controlPoints, this.controlPoints.Length + srcPoints.Length);
            srcPoints.CopyTo(this.controlPoints.AsSpan(destPointsCount));
        }

        public void TtfTransformWith2x2Matrix(float m00, float m01, float m10, float m11)
        {
            // Change data on current glyph.
            // http://stackoverflow.com/questions/13188156/whats-the-different-between-vector2-transform-and-vector2-transformnormal-i
            // http://www.technologicalutopia.com/sourcecode/xnageometry/vector2.cs.htm
            float newXmin = 0;
            float newYmin = 0;
            float newXmax = 0;
            float newYmax = 0;

            Vector2[] glyphPoints = this.ControlPoints;
            for (int i = 0; i < glyphPoints.Length; ++i)
            {
                Vector2 p = glyphPoints[i];
                float x = p.X;
                float y = p.Y;

                float newX = (float)Math.Round((x * m00) + (y * m10));
                float newY = (float)Math.Round((x * m01) + (y * m11));
                glyphPoints[i] = new Vector2(newX, newY);

                if (newX < newXmin)
                {
                    newXmin = newX;
                }

                if (newX > newXmax)
                {
                    newXmax = newX;
                }

                if (newY < newYmin)
                {
                    newYmin = newY;
                }

                if (newY > newYmax)
                {
                    newYmax = newY;
                }
            }

            this.Bounds = new Bounds((short)newXmin, (short)newYmin, (short)newXmax, (short)newYmax);
        }

        private static Vector2 Offset(Vector2 p, short dx, short dy) => new Vector2(p.X + dx, p.Y + dy);
    }
}
