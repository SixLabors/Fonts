// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal readonly struct GlyphTableEntry
    {
        public GlyphTableEntry(
            Vector2[] controlPoints,
            bool[] onCurves,
            ushort[] endPoints,
            Bounds bounds,
            ReadOnlyMemory<byte> instructions)
        {
            this.ControlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;

            if (bounds != default)
            {
                this.Bounds = bounds;
            }
            else
            {
                this.Bounds = CalculateBounds(this.ControlPoints);
            }

            this.Instructions = instructions;
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints { get; }

        public ushort[] EndPoints { get; }

        public bool[] OnCurves { get; }

        public Bounds Bounds { get; }

        public ReadOnlyMemory<byte> Instructions { get; }

        /// <summary>
        /// Transforms a glyph vector by a specified 3x2 matrix.
        /// </summary>
        /// <param name="src">The glyph vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphTableEntry Transform(in GlyphTableEntry src, Matrix3x2 matrix)
        {
            var controlPoints = new Vector2[src.PointCount];
            bool[] onCurves = new bool[src.OnCurves.Length];
            ushort[] endPoints = new ushort[src.EndPoints.Length];

            for (int i = 0; i < controlPoints.Length; i++)
            {
                controlPoints[i] = Vector2.Transform(src.ControlPoints[i], matrix);
            }

            src.OnCurves.AsSpan().CopyTo(onCurves);
            src.EndPoints.AsSpan().CopyTo(endPoints);

            return new GlyphTableEntry(controlPoints, onCurves, endPoints, Bounds.Transform(src.Bounds, matrix), src.Instructions);
        }

        /// <summary>
        /// Scales a glyph vector uniformly by a specified scale.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The new <see cref="GlyphTableEntry"/>.</returns>
        public static GlyphTableEntry Scale(in GlyphTableEntry src, float scale)
            => Transform(in src, Matrix3x2.CreateScale(scale));

        /// <summary>
        /// Translates a glyph vector by a specified x and y coordinates.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="dx">The x-offset.</param>
        /// <param name="dy">The y-offset.</param>
        /// <returns>The new <see cref="GlyphTableEntry"/>.</returns>
        public static GlyphTableEntry Translate(in GlyphTableEntry src, float dx, float dy)
            => Transform(in src, Matrix3x2.CreateTranslation(dx, dy));

        public static GlyphTableEntry DeepClone(in GlyphTableEntry other)
        {
            // Deep clone the arrays
            var controlPoints = new Vector2[other.ControlPoints.Length];
            other.ControlPoints.CopyTo(controlPoints.AsSpan());

            bool[] onCurves = new bool[other.OnCurves.Length];
            other.OnCurves.CopyTo(onCurves.AsSpan());

            ushort[] endPoints = new ushort[other.EndPoints.Length];
            other.EndPoints.CopyTo(endPoints.AsSpan());

            // Clone bounds
            Bounds sourceBounds = other.Bounds;
            var newBounds = new Bounds(sourceBounds.Min.X, sourceBounds.Min.Y, sourceBounds.Max.X, sourceBounds.Max.Y);

            // Instructions remain untouched.
            return new GlyphTableEntry(controlPoints, onCurves, endPoints, newBounds, other.Instructions);
        }

        private static Bounds CalculateBounds(Vector2[] controlPoints)
        {
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            for (int i = 0; i < controlPoints.Length; ++i)
            {
                Vector2 p = controlPoints[i];
                if (p.X < xMin)
                {
                    xMin = p.X;
                }

                if (p.X > xMax)
                {
                    xMax = p.X;
                }

                if (p.Y < yMin)
                {
                    yMin = p.Y;
                }

                if (p.Y > yMax)
                {
                    yMax = p.Y;
                }
            }

            return new Bounds(MathF.Round(xMin), MathF.Round(yMin), MathF.Round(xMax), MathF.Round(yMax));
        }
    }
}
