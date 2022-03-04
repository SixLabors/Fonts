// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Hinting;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// Represents the raw glyph outlines for a given glyph comprised of a collection of glyph table entries.
    /// The type is mutable by design to reduce copying during transformation.
    /// </summary>
    internal struct GlyphVector
    {
        private readonly List<GlyphTableEntry> entries;
        private readonly Bounds compositeBounds;

        internal GlyphVector(
            Vector2[] controlPoints,
            bool[] onCurves,
            ushort[] endPoints,
            Bounds bounds,
            ReadOnlyMemory<byte> instructions)
        {
            this.entries = new List<GlyphTableEntry>
            {
                new GlyphTableEntry(controlPoints, onCurves, endPoints, bounds, instructions)
            };

            this.compositeBounds = default;
        }

        private GlyphVector(List<GlyphTableEntry> entries, Bounds compositeBounds = default)
        {
            this.entries = entries;
            this.compositeBounds = compositeBounds;
        }

        public static GlyphVector Empty(Bounds bounds = default)
            => new(Array.Empty<Vector2>(), Array.Empty<bool>(), Array.Empty<ushort>(), bounds, Array.Empty<byte>());

        /// <summary>
        /// Transforms a glyph vector by a specified 3x2 matrix.
        /// </summary>
        /// <param name="src">The glyph vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Transform(GlyphVector src, Matrix3x2 matrix)
        {
            List<GlyphTableEntry> entries = new(src.entries.Count);
            for (int i = 0; i < src.entries.Count; i++)
            {
                entries.Add(GlyphTableEntry.Transform(src.entries[i], matrix));
            }

            if (src.compositeBounds == default)
            {
                return new(entries, src.compositeBounds);
            }

            return new(entries, Bounds.Transform(src.compositeBounds, matrix));
        }

        /// <summary>
        /// Scales a glyph vector uniformly by a specified scale.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Scale(GlyphVector src, float scale)
            => Transform(src, Matrix3x2.CreateScale(scale));

        /// <summary>
        /// Scales a glyph vector uniformly by a specified scale.
        /// </summary>
        /// <param name="src">The glyph vector to translate.</param>
        /// <param name="scales">The vector scale to use.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Scale(GlyphVector src, Vector2 scales)
            => Transform(src, Matrix3x2.CreateScale(scales));

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
        /// <param name="compositeBounds">The bounds for the composite glyph.</param>
        /// <returns>The new <see cref="GlyphVector"/>.</returns>
        public static GlyphVector Append(GlyphVector first, GlyphVector second, Bounds compositeBounds)
        {
            if (!first.HasValue())
            {
                return second;
            }

            List<GlyphTableEntry> entries = new(first.entries.Count + second.entries.Count);
            for (int i = 0; i < first.entries.Count; i++)
            {
                entries.Add(first.entries[i]);
            }

            for (int i = 0; i < second.entries.Count; i++)
            {
                entries.Add(second.entries[i]);
            }

            return new(entries, compositeBounds);
        }

        /// <summary>
        /// Applies True Type hinting to the specified glyph vector.
        /// </summary>
        /// <param name="glyph">The glyph vector to hint.</param>
        /// <param name="interpreter">The True Type interpreter.</param>
        /// <param name="pp1">The first phantom point.</param>
        /// <param name="pp2">The second phantom point.</param>
        /// <param name="pp3">The third phantom point.</param>
        /// <param name="pp4">The fourth phantom point.</param>
        public static void Hint(ref GlyphVector glyph, Interpreter interpreter, Vector2 pp1, Vector2 pp2, Vector2 pp3, Vector2 pp4)
        {
            for (int i = 0; i < glyph.entries.Count; i++)
            {
                GlyphTableEntry entry = glyph.entries[i];
                var controlPoints = new Vector2[entry.ControlPoints.Length + 4];
                controlPoints[controlPoints.Length - 4] = pp1;
                controlPoints[controlPoints.Length - 3] = pp2;
                controlPoints[controlPoints.Length - 2] = pp3;
                controlPoints[controlPoints.Length - 1] = pp4;
                entry.ControlPoints.AsSpan().CopyTo(controlPoints.AsSpan());

                var withPhantomPoints = new GlyphTableEntry(controlPoints, entry.OnCurves, entry.EndPoints, entry.Bounds, entry.Instructions);
                interpreter.HintGlyph(withPhantomPoints);

                controlPoints.AsSpan(0, entry.ControlPoints.Length).CopyTo(entry.ControlPoints.AsSpan());
                glyph.entries[i] = entry;
            }
        }

        /// <summary>
        /// Creates a new glyph vector that is a deep copy of the specified instance.
        /// </summary>
        /// <param name="src">The source glyph vector to copy.</param>
        /// <returns>The cloned <see cref="GlyphVector"/>.</returns>
        public static GlyphVector DeepClone(GlyphVector src)
        {
            List<GlyphTableEntry> entries = new(src.entries.Count);
            for (int i = 0; i < src.entries.Count; i++)
            {
                entries.Add(GlyphTableEntry.DeepClone(src.entries[i]));
            }

            return new(entries, src.compositeBounds);
        }

        /// <summary>
        /// Returns a value indicating whether the current instance is empty.
        /// </summary>
        /// <returns>The <see cref="bool"/> indicating the result.</returns>
        public bool HasValue() => this.entries?[0].ControlPoints.Length > 0;

        /// <summary>
        /// Returns the bounds for the current instance.
        /// </summary>
        /// <returns>The <see cref="GetBounds"/>.</returns>
        public Bounds GetBounds() => this.compositeBounds != default ? this.compositeBounds : this.entries[0].Bounds;

        /// <summary>
        /// Returns the result of combining each glyph within this instance as a single outline.
        /// </summary>
        /// <returns>The <see cref="GlyphOutline"/>.</returns>
        public GlyphOutline GetOutline()
        {
            List<Vector2> controlPoints = new();
            List<bool> onCurves = new();
            List<ushort> endPoints = new();

            for (int resultIndex = 0; resultIndex < this.entries.Count; resultIndex++)
            {
                GlyphTableEntry glyph = this.entries[resultIndex];
                int pointCount = glyph.PointCount;
                ushort endPointOffset = (ushort)controlPoints.Count;
                for (int i = 0; i < pointCount; i++)
                {
                    controlPoints.Add(glyph.ControlPoints[i]);
                    onCurves.Add(glyph.OnCurves[i]);
                }

                foreach (ushort p in glyph.EndPoints)
                {
                    endPoints.Add((ushort)(p + endPointOffset));
                }
            }

            return new GlyphOutline(controlPoints.ToArray(), endPoints.ToArray(), onCurves.ToArray());
        }

        /// <summary>
        /// Returns a new instance with the composite bounds set to the specified value
        /// </summary>
        /// <param name="src">The src glyph vector.</param>
        /// <param name="bounds">The composite bounds.</param>
        /// <returns>The <see cref="GlyphVector"/>.</returns>
        public static GlyphVector WithCompositeBounds(GlyphVector src, Bounds bounds)
            => new(src.entries, bounds);
    }
}
