using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal struct GlyphVector
    {
        internal GlyphVector(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.ControlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;
            this.Bounds = bounds;
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints { get; private set; }

        public ushort[] EndPoints { get; private set; }

        public bool[] OnCurves { get; private set; }

        public Bounds Bounds { get; private set; }
    }
}
