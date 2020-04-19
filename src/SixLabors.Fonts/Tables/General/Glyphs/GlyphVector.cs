// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal readonly struct GlyphVector
    {
        internal GlyphVector(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.ControlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;
            this.Bounds = bounds;
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints { get; }

        public ushort[] EndPoints { get; }

        public bool[] OnCurves { get; }

        public Bounds Bounds { get; }
    }

    internal readonly struct GlyphVectorWithColor
    {
        internal GlyphVectorWithColor(GlyphVector vector, GlyphColor color)
        {
            this.Vector = vector;
            this.Color = color;
        }

        public GlyphVector Vector { get; }

        public GlyphColor Color { get; }
    }
}
