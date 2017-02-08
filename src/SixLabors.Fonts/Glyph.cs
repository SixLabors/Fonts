using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    internal class Glyph
    {
        public Glyph(short[] xs, short[] ys, bool[] onCurves, ushort[] endPoints, Bounds bounds)
            : this(Convert(xs, ys), onCurves, endPoints, bounds)
        {
        }

        public Glyph(Vector2[] controlPoints, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.ControlPoints = controlPoints;
            this.OnCurves = onCurves;
            this.EndPoints = endPoints;
            this.Bounds = bounds;
        }

        private static Vector2[] Convert(short[] xs, short[] ys)
        {
            Vector2[] vectors = new Vector2[xs.Length];
            Vector2 current = Vector2.Zero;
            for (var i = 0; i < xs.Length; i++)
            {
                current += new Vector2(xs[i], ys[i]);
                vectors[i] = current;
            }

            return vectors;
        }

        public int PointCount => this.ControlPoints.Length;

        public Vector2[] ControlPoints { get; private set; }

        public ushort[] EndPoints { get; private set; }

        public bool[] OnCurves { get; private set; }

        public Bounds Bounds { get; private set; }
    }
}
