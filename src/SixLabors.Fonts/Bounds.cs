using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    public class Bounds
    {
        public Bounds(Vector2 min, Vector2 max)
        {
            this.Min = min;
            this.Max = max;
        }

        public Bounds(float minX, float minY, float maxX, float maxY)
            : this(new Vector2(minX, minY), new Vector2(maxX, maxY))
        {
        }

        public Vector2 Min { get; }

        public Vector2 Max { get; }

        internal static Bounds Load(BinaryReader reader)
        {
            var minX = reader.ReadInt16();
            var minY = reader.ReadInt16();
            var maxX = reader.ReadInt16();
            var maxY = reader.ReadInt16();

            return new Bounds(minX, minY, maxX, maxY);
        }
    }
}
