// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal struct GlyphVector : IDeepCloneable
    {
        internal static GlyphVector Empty(Bounds bounds = default)
            => new GlyphVector(Array.Empty<Vector2>(), Array.Empty<bool>(), Array.Empty<ushort>(), bounds, Array.Empty<byte>());

        internal GlyphVector(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds, ReadOnlyMemory<byte> instructions)
        {
            this.ControlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;
            this.Bounds = bounds;
            this.Instructions = instructions;
        }

        private GlyphVector(GlyphVector other)
        {
            this.ControlPoints = new Vector2[other.ControlPoints.Length];
            other.ControlPoints.CopyTo(this.ControlPoints.AsSpan());
            this.OnCurves = new bool[other.OnCurves.Length];
            other.OnCurves.CopyTo(this.OnCurves.AsSpan());
            this.EndPoints = new ushort[other.EndPoints.Length];
            other.EndPoints.CopyTo(this.EndPoints.AsSpan());
            Bounds origBounds = other.Bounds;
            this.Bounds = new Bounds(origBounds.Min.X, origBounds.Min.Y, origBounds.Max.X, origBounds.Max.Y);

            this.Instructions = other.Instructions;
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints { get; }

        public ushort[] EndPoints { get; }

        public bool[] OnCurves { get; }

        public ReadOnlyMemory<byte> Instructions { get; }

        public Bounds Bounds { get; internal set; }

        /// <inheritdoc/>
        public IDeepCloneable DeepClone() => new GlyphVector(this);

        /// <summary>
        /// Transforms a glyph vector by a specified 3x2 matrix.
        /// </summary>
        /// <param name="src">The glyph vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Transform(GlyphVector src, Matrix3x2 matrix)
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

            var bounds = new Bounds(
                Vector2.Transform(src.Bounds.Min, matrix),
                Vector2.Transform(src.Bounds.Max, matrix));

            return new GlyphVector(controlPoints, onCurves, endPoints, bounds, src.Instructions);
        }

        /// <summary>
        /// Translates a glyph vector by a specified x and y coordinates.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="dx">The x-offset.</param>
        /// <param name="dy">The y-offset.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Translate(GlyphVector src, float dx, float dy)
            => Transform(src, Matrix3x2.CreateTranslation(dx, dy));

        /// <summary>
        /// Appends the second glyph vector's control points to the first.
        /// </summary>
        /// <param name="first">The first glyph vector.</param>
        /// <param name="second">The second glyph vector.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Append(GlyphVector first, GlyphVector second)
        {
            // should we be doing this at this level or should we be doing this at hinting stage ???

            // No IEquality<GlyphVector> implementation.
            if (first.ControlPoints is null)
            {
                return new GlyphVector(second);
            }

            var controlPoints = new Vector2[first.PointCount + second.PointCount];
            first.ControlPoints.AsSpan().CopyTo(controlPoints);
            second.ControlPoints.AsSpan().CopyTo(controlPoints.AsSpan(first.PointCount));

            bool[] onCurves = new bool[first.OnCurves.Length + second.OnCurves.Length];
            first.OnCurves.AsSpan().CopyTo(onCurves);
            second.OnCurves.AsSpan().CopyTo(onCurves.AsSpan(first.OnCurves.Length));

            ushort[] endPoints = new ushort[first.EndPoints.Length + second.EndPoints.Length];
            first.EndPoints.AsSpan().CopyTo(endPoints);
            int offset = first.EndPoints.Length;
            ushort endPointOffset = (ushort)first.PointCount;
            for (int i = 0; i < second.EndPoints.Length; i++)
            {
                endPoints[i + offset] = (ushort)(second.EndPoints[i] + endPointOffset);
            }

            var bounds = new Bounds(first.Bounds.Min, first.Bounds.Max);
            return new GlyphVector(controlPoints, onCurves, endPoints, bounds, first.Instructions);
        }
    }
}
