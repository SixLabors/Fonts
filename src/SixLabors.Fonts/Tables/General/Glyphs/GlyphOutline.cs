// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// Represents the outline of a glyph.
    /// </summary>
    public readonly struct GlyphOutline
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlyphOutline"/> struct.
        /// </summary>
        /// <param name="controlPoints">The vectorial points defining the shape of this glyph.</param>
        /// <param name="endPoints">The point indices for the last point of each contour, in increasing numeric order.</param>
        /// <param name="onCurves">Value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve</param>
        public GlyphOutline(
            Vector2[] controlPoints,
            ushort[] endPoints,
            bool[] onCurves)
        {
            this.ControlPoints = controlPoints;
            this.EndPoints = endPoints;
            this.OnCurves = onCurves;
        }

        /// <summary>
        /// Gets the vectorial points defining the shape of this glyph.
        /// </summary>
        public ReadOnlyMemory<Vector2> ControlPoints { get; }

        /// <summary>
        /// Gets the point indices for the last point of each contour, in increasing numeric order.
        /// </summary>
        public ReadOnlyMemory<ushort> EndPoints { get; }

        /// <summary>
        /// Gets at value indicating whether the corresponding <see cref="ControlPoints"/> item is on a curve.
        /// </summary>
        public ReadOnlyMemory<bool> OnCurves { get; }
    }
}
