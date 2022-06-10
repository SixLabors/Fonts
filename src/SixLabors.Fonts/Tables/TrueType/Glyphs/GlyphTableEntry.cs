// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs
{
    /// <summary>
    /// Represents a single simple glyph table entry.
    /// The type is mutable by design to reduce copying during transformation.
    /// </summary>
    internal struct GlyphTableEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphTableEntry"/> struct.
        /// </summary>
        /// <param name="controlPoints">The vectorial points defining the shape of this glyph.</param>
        /// <param name="onCurves">A value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve.</param>
        /// <param name="endPoints">The point indices for the last point of each contour, in increasing numeric order.</param>
        /// <param name="bounds">The glyph bounds.</param>
        /// <param name="instructions">The glyph hinting instructions.</param>
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

        /// <summary>
        /// Gets the number of control points.
        /// </summary>
        public int PointCount => this.ControlPoints.Length;

        /// <summary>
        /// Gets or sets the vectorial points defining the shape of this glyph.
        /// </summary>
        public Vector2[] ControlPoints { get; set; }

        /// <summary>
        /// Gets or sets the point indices for the last point of each contour, in increasing numeric order.
        /// </summary>
        public ushort[] EndPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve.
        /// </summary>
        public bool[] OnCurves { get; set; }

        /// <summary>
        /// Gets or sets the glyph bounds.
        /// </summary>
        public Bounds Bounds { get; set; }

        /// <summary>
        /// Gets or sets the hinting instructions.
        /// </summary>
        public ReadOnlyMemory<byte> Instructions { get; set; }

        /// <summary>
        /// Transforms a glyph vector by a specified 3x2 matrix.
        /// </summary>
        /// <param name="src">The glyph vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphTableEntry Transform(GlyphTableEntry src, Matrix3x2 matrix)
        {
            Vector2[] controlPoints = src.ControlPoints;
            for (int i = 0; i < controlPoints.Length; i++)
            {
                controlPoints[i] = Vector2.Transform(src.ControlPoints[i], matrix);
            }

            return new GlyphTableEntry(controlPoints, src.OnCurves, src.EndPoints, Bounds.Transform(src.Bounds, matrix), src.Instructions);
        }

        /// <summary>
        /// Scales a glyph vector uniformly by a specified scale.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The new <see cref="GlyphTableEntry"/>.</returns>
        public static GlyphTableEntry Scale(GlyphTableEntry src, float scale)
            => Transform(src, Matrix3x2.CreateScale(scale));

        /// <summary>
        /// Translates a glyph vector by a specified x and y coordinates.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="dx">The x-offset.</param>
        /// <param name="dy">The y-offset.</param>
        /// <returns>The new <see cref="GlyphTableEntry"/>.</returns>
        public static GlyphTableEntry Translate(GlyphTableEntry src, float dx, float dy)
            => Transform(src, Matrix3x2.CreateTranslation(dx, dy));

        /// <summary>
        /// Creates a new glyph table entry that is a deep copy of the specified instance.
        /// </summary>
        /// <param name="src">The source glyph table entry to copy.</param>
        /// <returns>The cloned <see cref="GlyphTableEntry"/>.</returns>
        public static GlyphTableEntry DeepClone(GlyphTableEntry src)
        {
            // Deep clone the arrays
            var controlPoints = new Vector2[src.ControlPoints.Length];
            src.ControlPoints.CopyTo(controlPoints.AsSpan());

            bool[] onCurves = new bool[src.OnCurves.Length];
            src.OnCurves.CopyTo(onCurves.AsSpan());

            ushort[] endPoints = new ushort[src.EndPoints.Length];
            src.EndPoints.CopyTo(endPoints.AsSpan());

            // Clone bounds
            Bounds sourceBounds = src.Bounds;
            var newBounds = new Bounds(sourceBounds.Min.X, sourceBounds.Min.Y, sourceBounds.Max.X, sourceBounds.Max.Y);

            // Instructions remain untouched.
            return new GlyphTableEntry(controlPoints, onCurves, endPoints, newBounds, src.Instructions);
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
