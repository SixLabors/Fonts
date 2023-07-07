// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts
{
    internal readonly struct Bounds : IEquatable<Bounds>
    {
        public static Bounds Empty = default;

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

        public static bool operator ==(Bounds left, Bounds right) => left.Equals(right);

        public static bool operator !=(Bounds left, Bounds right) => !(left == right);

        public Vector2 Size() => this.Max - this.Min;

        public static Bounds Load(BigEndianBinaryReader reader)
        {
            short minX = reader.ReadInt16();
            short minY = reader.ReadInt16();
            short maxX = reader.ReadInt16();
            short maxY = reader.ReadInt16();

            return new Bounds(minX, minY, maxX, maxY);
        }

        public static Bounds Load(IList<ControlPoint> controlPoints)
        {
            if (controlPoints is null || controlPoints.Count == 0)
            {
                return Empty;
            }

            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            for (int i = 0; i < controlPoints.Count; i++)
            {
                Vector2 p = controlPoints[i].Point;
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

            return new Bounds(xMin, yMin, xMax, yMax);
        }

        public static Bounds Transform(in Bounds bounds, Matrix3x2 matrix)
            => new(Vector2.Transform(bounds.Min, matrix), Vector2.Transform(bounds.Max, matrix));

        public override bool Equals(object? obj) => obj is Bounds bounds && this.Equals(bounds);

        public bool Equals(Bounds other) => this.Min.Equals(other.Min) && this.Max.Equals(other.Max);

        public override int GetHashCode() => HashCode.Combine(this.Min, this.Max);
    }
}
