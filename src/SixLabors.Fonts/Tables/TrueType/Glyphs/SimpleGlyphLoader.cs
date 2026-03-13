// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Numerics;

namespace SixLabors.Fonts.Tables.TrueType.Glyphs;

/// <summary>
/// Implements loading Simple Glyph Description which is part of the `glyph`table.
/// </summary>
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/otspec160/glyf#simple-glyph-description"/>
internal class SimpleGlyphLoader : GlyphLoader
{
    private readonly ControlPoint[] controlPoints;
    private readonly ushort[] endPoints;
    private readonly Bounds bounds;
    private readonly byte[] instructions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleGlyphLoader"/> class.
    /// </summary>
    /// <param name="controlPoints">The glyph's control points.</param>
    /// <param name="endPoints">The indices of the last point of each contour.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <param name="instructions">The hinting instructions for this glyph.</param>
    public SimpleGlyphLoader(ControlPoint[] controlPoints, ushort[] endPoints, Bounds bounds, byte[] instructions)
    {
        this.controlPoints = controlPoints;
        this.endPoints = endPoints;
        this.bounds = bounds;
        this.instructions = instructions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleGlyphLoader"/> class
    /// for a glyph with zero contours (bounds only).
    /// </summary>
    /// <param name="bounds">The glyph bounding box.</param>
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
        /// <summary>
        /// The point is is off the curve.
        /// </summary>
        ControlPoint = 0,

        /// <summary>
        /// The point is on the curve.
        /// </summary>
        OnCurve = 1,

        /// <summary>
        /// If set, the corresponding x-coordinate is 1 byte long. If not set, 2 bytes.
        /// </summary>
        XByte = 2,

        /// <summary>
        /// If set, the corresponding y-coordinate is 1 byte long. If not set, 2 bytes.
        /// </summary>
        YByte = 4,

        /// <summary>
        /// f set, the next byte specifies the number of additional times this set of flags is to be repeated.
        /// In this way, the number of flags listed can be smaller than the number of points in a character.
        /// </summary>
        Repeat = 8,

        /// <summary>
        /// This flag has two meanings, depending on how the x-Short Vector flag is set.
        /// If x-Short Vector is set, this bit describes the sign of the value, with 1 equalling positive and 0 negative.
        /// If the x-Short Vector bit is not set and this bit is set, then the current x-coordinate is the same as the previous x-coordinate.
        /// If the x-Short Vector bit is not set and this bit is also not set, the current x-coordinate is a signed 16-bit delta vector.
        /// </summary>
        XSignOrSame = 16,

        /// <summary>
        /// This flag has two meanings, depending on how the y-Short Vector flag is set.
        /// If y-Short Vector is set, this bit describes the sign of the value, with 1 equalling positive and 0 negative.
        /// If the y-Short Vector bit is not set and this bit is set, then the current y-coordinate is the same as the previous y-coordinate.
        /// If the y-Short Vector bit is not set and this bit is also not set, the current y-coordinate is a signed 16-bit delta vector.
        /// </summary>
        YSignOrSame = 32
    }

    /// <inheritdoc/>
    public override GlyphVector CreateGlyph(GlyphTable table)
        => new(this.controlPoints, this.endPoints, this.bounds, this.instructions, false);

    /// <summary>
    /// Reads a simple glyph description from the binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned after the glyph header.</param>
    /// <param name="count">The number of contours in the glyph.</param>
    /// <param name="bounds">The glyph bounding box.</param>
    /// <returns>A <see cref="GlyphLoader"/> containing the simple glyph data.</returns>
    public static GlyphLoader LoadSimpleGlyph(BigEndianBinaryReader reader, short count, in Bounds bounds)
    {
        if (count == 0)
        {
            return new SimpleGlyphLoader(bounds);
        }

        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | Type            | Name                                   | Description                                                        |
        // +=================+========================================+====================================================================+
        // | uint16          | endPtsOfContours[n]                    | Array of last points of each contour; n is the number of contours. |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | uint16          | instructionLength                      | Total number of bytes for instructions.                            |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | uint8           | instructions[n]                        | Array of instructions for each glyph;                              |
        // |                 |                                        | n is the number of instructions.                                   |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | uint8           | flags[n]                               | Array of flags for each coordinate in outline;                     |
        // |                 |                                        | n is the number of flags.                                          |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | uint8 or int16  | xCoordinates[]                         | First coordinates relative to(0, 0);                               |
        // |                 |                                        | others are relative to previous point.                             |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
        // | uint8 or int16  | yCoordinates[]                         | First coordinates relative to (0, 0);                              |
        // |                 |                                        | others are relative to previous point.                             |
        // +-----------------+----------------------------------------+--------------------------------------------------------------------+
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

        var controlPoints = new ControlPoint[xs.Length];
        for (int i = 0; i < flags.Length; i++)
        {
            controlPoints[i] = new(new Vector2(xs[i], ys[i]), (flags[i] & Flags.OnCurve) == Flags.OnCurve);
        }

        return new SimpleGlyphLoader(controlPoints, endPoints, bounds, instructions);
    }

    /// <summary>
    /// Reads the packed flag array for all points in a simple glyph.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="flagCount">The number of flags (points) to read.</param>
    /// <returns>An array of flags, one per control point.</returns>
    private static Flags[] ReadFlags(BigEndianBinaryReader reader, int flagCount)
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
                if ((flag & Flags.Repeat) == Flags.Repeat)
                {
                    repeatCount = reader.ReadByte();
                }
            }

            result[c++] = flag;
        }

        return result;
    }

    /// <summary>
    /// Reads a coordinate array (x or y) for all points in a simple glyph, applying delta decoding.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="pointCount">The number of points to read.</param>
    /// <param name="flags">The per-point flags array.</param>
    /// <param name="isByte">The flag indicating the coordinate is stored as a single byte.</param>
    /// <param name="signOrSame">The flag indicating sign (if byte) or same-as-previous (if word).</param>
    /// <returns>An array of absolute coordinate values.</returns>
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
