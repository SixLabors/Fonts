// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

internal class SimpleGlyphLoader : GlyphLoader
{
    private readonly ControlPoint[] controlPoints;
    private readonly ushort[] endPoints;
    private readonly Bounds bounds;
    private readonly byte[] instructions;

    public SimpleGlyphLoader(ControlPoint[] controlPoints, ushort[] endPoints, Bounds bounds, byte[] instructions)
    {
        this.controlPoints = controlPoints;
        this.endPoints = endPoints;
        this.bounds = bounds;
        this.instructions = instructions;
    }

    public SimpleGlyphLoader(Bounds bounds)
    {
        this.controlPoints = Array.Empty<ControlPoint>();
        this.endPoints = Array.Empty<ushort>();
        this.instructions = Array.Empty<byte>();
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
        => new(this.controlPoints, this.endPoints, this.bounds, this.instructions, false);

    public static GlyphLoader LoadSimpleGlyph(BigEndianBinaryReader reader, short count, in Bounds bounds)
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

        ControlPoint[] controlPoints = new ControlPoint[xs.Length];
        for (int i = 0; i < flags.Length; i++)
        {
            controlPoints[i] = new ControlPoint(new Vector2(xs[i], ys[i]), (flags[i] & Flags.OnCurve) == Flags.OnCurve);
        }

        return new SimpleGlyphLoader(controlPoints, endPoints, bounds, instructions);
    }

    private static Flags[] ReadFlags(BigEndianBinaryReader reader, int flagCount)
    {
        Flags[] result = new Flags[flagCount];
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
                if ((flag & Flags.Repeat) == Flags.Repeat)
                {
                    repeatCount = reader.ReadByte();
                }
            }

            result[c++] = flag;
        }

        return result;
    }

    private static short[] ReadCoordinates(BigEndianBinaryReader reader, int pointCount, Flags[] flags, Flags isByte, Flags signOrSame)
    {
        short[] xs = new short[pointCount];
        short x = 0;
        for (int i = 0; i < pointCount; i++)
        {
            short dx;
            Flags currentFlag = flags[i];
            if ((currentFlag & isByte) == isByte)
            {
                byte b = reader.ReadByte();
                dx = (short)((currentFlag & signOrSame) == signOrSame ? b : -b);
            }
            else if ((currentFlag & signOrSame) == signOrSame)
            {
                dx = 0;
            }
            else
            {
                dx = reader.ReadInt16();
            }

            x += dx;
            xs[i] = x;
        }

        return xs;
    }
}
