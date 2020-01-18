// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal class SimpleGlyphLoader : GlyphLoader
    {
        private readonly short[] xs;
        private readonly short[] ys;
        private readonly bool[] onCurves;
        private readonly ushort[] endPoints;
        private readonly Bounds bounds;

        public SimpleGlyphLoader(short[] xs, short[] ys, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.xs = xs;
            this.ys = ys;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.bounds = bounds;
        }

        public SimpleGlyphLoader(Bounds bounds)
        {
            this.ys = this.xs = new short[0];
            this.onCurves = new bool[0];
            this.endPoints = new ushort[0];
            this.bounds = bounds;
        }

        [Flags]
        private enum Flags : byte
        {
            ControlPoint = 0,
            OnCurve = 1,
            XByte = 2,
            YByte = 4,
            Repeat = 8,
            XSignOrSame = 16,
            YSignOrSame = 32
        }

        public override GlyphVector CreateGlyph(GlyphTable table)
        {
            // lets build some shapes ??? here from
            return new GlyphVector(Convert(this.xs, this.ys), this.onCurves, this.endPoints, this.bounds);
        }

        private static Vector2[] Convert(short[] xs, short[] ys)
        {
            var vectors = new Vector2[xs.Length];
            Vector2 current = Vector2.Zero;
            for (int i = 0; i < xs.Length; i++)
            {
                vectors[i] = new Vector2(xs[i], ys[i]);
            }

            return vectors;
        }

        public static GlyphLoader LoadSimpleGlyph(BinaryReader reader, short count, in Bounds bounds)
        {
            if (count == 0)
            {
                return new SimpleGlyphLoader(bounds);
            }

            // uint16         | endPtsOfContours[n] | Array of last points of each contour; n is the number of contours.
            // uint16         | instructionLength   | Total number of bytes for instructions.
            // uint8          | instructions[n]     | Array of instructions for each glyph; n is the number of instructions.
            // uint8          | flags[n]            | Array of flags for each coordinate in outline; n is the number of flags.
            // uint8 or int16 | xCoordinates[ ]     | First coordinates relative to(0, 0); others are relative to previous point.
            // uint8 or int16 | yCoordinates[]      | First coordinates relative to (0, 0); others are relative to previous point.
            ushort[] endPoints = reader.ReadUInt16Array(count);

            ushort instructionSize = reader.ReadUInt16();
            byte[] instructions = reader.ReadUInt8Array(instructionSize);

            // TODO: should this take the max points rather?
            int pointCount = 0;
            if (count > 0)
            {
                pointCount = endPoints[count - 1] + 1;
            }

            Flags[] flags = ReadFlags(reader, pointCount);
            short[] xs = ReadCoordinates(reader, pointCount, flags, Flags.XByte, Flags.XSignOrSame);
            short[] ys = ReadCoordinates(reader, pointCount, flags, Flags.YByte, Flags.YSignOrSame);

            bool[] onCurves = new bool[flags.Length];
            for (int i = flags.Length - 1; i >= 0; --i)
            {
                onCurves[i] = flags[i].HasFlag(Flags.OnCurve);
            }

            return new SimpleGlyphLoader(xs, ys, onCurves, endPoints, bounds);
        }

        private static Flags[] ReadFlags(BinaryReader reader, int flagCount)
        {
            var result = new Flags[flagCount];
            int c = 0;
            int repeatCount = 0;
            Flags flag = default;
            while (c < flagCount)
            {
                if (repeatCount > 0)
                {
                    repeatCount--;
                }
                else
                {
                    flag = (Flags)reader.ReadUInt8();
                    if (flag.HasFlag(Flags.Repeat))
                    {
                        repeatCount = reader.ReadByte();
                    }
                }

                result[c++] = flag;
            }

            return result;
        }

        private static short[] ReadCoordinates(BinaryReader reader, int pointCount, Flags[] flags, Flags isByte, Flags signOrSame)
        {
            short[] xs = new short[pointCount];
            int x = 0;
            for (int i = 0; i < pointCount; i++)
            {
                int dx;
                if (flags[i].HasFlag(isByte))
                {
                    byte b = reader.ReadByte();
                    dx = flags[i].HasFlag(signOrSame) ? b : -b;
                }
                else
                {
                    if (flags[i].HasFlag(signOrSame))
                    {
                        dx = 0;
                    }
                    else
                    {
                        dx = reader.ReadInt16();
                    }
                }

                x += dx;
                xs[i] = (short)x; // TODO: overflow?
            }

            return xs;
        }
    }
}
