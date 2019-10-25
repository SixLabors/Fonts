// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Numerics;

namespace SixLabors.Fonts
{
    internal readonly struct Bounds
    {
        public Bounds(Vector2 min, Vector2 max)
        {
            this.Min = Vector2.Min(min, max);
            this.Max = Vector2.Max(min, max);
        }

        public Bounds(float minX, float minY, float maxX, float maxY)
            : this(new Vector2(minX, minY), new Vector2(maxX, maxY))
        {
        }

        public Vector2 Min { get; }

        public Vector2 Max { get; }

        public Vector2 Size()
        {
            return new Vector2(this.Max.X - this.Min.X, this.Max.Y - this.Min.Y);
        }

        internal static Bounds Load(BinaryReader reader)
        {
            short minX = reader.ReadInt16();
            short minY = reader.ReadInt16();
            short maxX = reader.ReadInt16();
            short maxY = reader.ReadInt16();

            return new Bounds(minX, minY, maxX, maxY);
        }
    }
}
