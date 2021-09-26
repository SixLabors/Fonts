// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables
{
    // Source code is based on https://github.com/LayoutFarm/Typography
    // see https://github.com/LayoutFarm/Typography/blob/master/Typography.OpenFont/WebFont/Woff2Reader.cs
    internal static class Woff2Utils
    {
        // We don't reuse the const tag headers from our table types for clarity.
        private static readonly string[] KnownTableTags =
        {
            "cmap", "head", "hhea", "hmtx", "maxp", "name", "OS/2", "post", "cvt ",
            "fpgm", "glyf", "loca", "prep", "CFF ", "VORG", "EBDT", "EBLC", "gasp",
            "hdmx", "kern", "LTSH", "PCLT", "VDMX", "vhea", "vmtx", "BASE", "GDEF",
            "GPOS", "GSUB", "EBSC", "JSTF", "MATH", "CBDT", "CBLC", "COLR", "CPAL",
            "SVG ", "sbix", "acnt", "avar", "bdat", "bloc", "bsln", "cvar", "fdsc",
            "feat", "fmtx", "fvar", "gvar", "hsty", "just", "lcar", "mort", "morx",
            "opbd", "prop", "trak", "Zapf", "Silf", "Glat", "Gloc", "Feat", "Sill"
        };

        private const byte OneMoreByteCode1 = 255;
        private const byte OneMoreByteCode2 = 254;
        private const byte WordCode = 253;
        private const byte LowestUCode = 253;

        public static ReadOnlyDictionary<string, TableHeader> ReadWoff2Headers(BigEndianBinaryReader reader, int tableCount)
        {
            uint expectedTableStartAt = 0;
            var headers = new Dictionary<string, TableHeader>(tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                Woff2TableHeader woffTableHeader = Read(reader, expectedTableStartAt, out uint nextExpectedTableStartAt);
                expectedTableStartAt = nextExpectedTableStartAt;
                headers.Add(woffTableHeader.Tag, woffTableHeader);
            }

            return new ReadOnlyDictionary<string, TableHeader>(headers);
        }

        public static Woff2TableHeader Read(BigEndianBinaryReader reader, uint expectedTableStartAt, out uint nextExpectedTableStartAt)
        {
            // Leave the first byte open to store flagByte
            const uint woff2FlagsTransform = 1 << 8;
            byte flagsByte = reader.ReadByte();
            int knownTable = flagsByte & 0x3F;
            string tableName = knownTable == 0x3F ? reader.ReadTag() : KnownTableTags[knownTable];

            uint flags = 0;
            byte xformVersion = (byte)((flagsByte >> 6) & 0x03);

            // 0 means xform for glyph/loca, non-0 for others
            if (tableName is "glyf" or "loca")
            {
                if (xformVersion == 0)
                {
                    flags |= woff2FlagsTransform;
                }
            }
            else if (xformVersion != 0)
            {
                flags |= woff2FlagsTransform;
            }

            flags |= xformVersion;

            if (!ReadUIntBase128(reader, out uint tableOrigLength))
            {
                throw new FontException("Error parsing woff2 table header");
            }

            uint tableTransformLength = tableOrigLength;
            if ((flags & woff2FlagsTransform) != 0)
            {
                if (!ReadUIntBase128(reader, out tableTransformLength))
                {
                    throw new FontException("Error parsing woff2 table header");
                }

                if (tableName == "loca" && tableTransformLength > 0)
                {
                    throw new FontException("Error parsing woff2 table header");
                }
            }

            nextExpectedTableStartAt = expectedTableStartAt + tableTransformLength;
            if (nextExpectedTableStartAt < expectedTableStartAt)
            {
                throw new FontException("Error parsing woff2 table header");
            }

            return new Woff2TableHeader(tableName, 0, expectedTableStartAt, tableTransformLength);
        }

        public static GlyphLoader[] LoadAllGlyphs(BigEndianBinaryReader reader, EmptyGlyphLoader emptyGlyphLoader)
        {
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | Data Type | Semantic              | Description and value type (if applicable)                                                            |
            // +===========+=======================+=======================================================================================================+
            // | Fixed     | version               | = 0x00000000                                                                                          |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt16    | numGlyphs             | Number of glyphs                                                                                      |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt16    | indexFormat           | Offset format for loca table, should be consistent with indexToLocFormat                              |
            // |           |                       | of the original head table (see specification)                                                        |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | nContourStreamSize    | Size of nContour stream in bytes                                                                      |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | nPointsStreamSize     | Size of nPoints stream in bytes                                                                       |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | flagStreamSize        | Size of flag stream in bytes                                                                          |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | glyphStreamSize       | Size of glyph stream in bytes (a stream of variable-length encoded values, see description below)     |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | compositeStreamSize   | Size of composite stream in bytes (a stream of variable-length encoded values, see description below) |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | bboxStreamSize        | Size of bbox data in bytes representing combined length of bboxBitmap (a packed bit array)            |
            // |           |                       | and bboxStream (a stream of Int16 values)                                                             |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt32    | instructionStreamSize | Size of instruction stream (a stream of UInt8 values)                                                 |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | Int16     | nContourStream[]      | Stream of Int16 values representing number of contours for each glyph record                          |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | 255UInt16 | nPointsStream[]       | Stream of values representing number of outline points for each contour in glyph records              |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt8     | flagStream[]          | Stream of UInt8 values representing flag values for each outline point.                               |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | Vary      | glyphStream[]         | Stream of bytes representing point coordinate values using variable length                            |
            // |           |                       | encoding format (defined in subclause 5.2)                                                            |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | Vary      | compositeStream[]     | Stream of bytes representing component flag values and associated composite glyph data                |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt8     | bboxBitmap[]          | Bitmap (a numGlyphs-long bit array) indicating explicit bounding boxes                                |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | Int16     | bboxStream[]          | Stream of Int16 values representing glyph bounding box data                                           |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            // | UInt8     | instructionStream[]   | Stream of UInt8 values representing a set of instructions for each corresponding glyph                |
            // +-----------+-----------------------+-------------------------------------------------------------------------------------------------------+
            uint version = reader.ReadUInt32();
            ushort numGlyphs = reader.ReadUInt16();
            ushort indexFormatOffset = reader.ReadUInt16();

            uint nContourStreamSize = reader.ReadUInt32();
            uint nPointsStreamSize = reader.ReadUInt32();
            uint flagStreamSize = reader.ReadUInt32();
            uint glyphStreamSize = reader.ReadUInt32();
            uint compositeStreamSize = reader.ReadUInt32();
            uint bboxStreamSize = reader.ReadUInt32();
            uint instructionStreamSize = reader.ReadUInt32();

            long expectedNCountStartAt = reader.BaseStream.Position;
            long expectedNPointStartAt = expectedNCountStartAt + nContourStreamSize;
            long expectedFlagStreamStartAt = expectedNPointStartAt + nPointsStreamSize;
            long expectedGlyphStreamStartAt = expectedFlagStreamStartAt + flagStreamSize;
            long expectedCompositeStreamStartAt = expectedGlyphStreamStartAt + glyphStreamSize;

            long expectedBboxStreamStartAt = expectedCompositeStreamStartAt + compositeStreamSize;
            long expectedInstructionStreamStartAt = expectedBboxStreamStartAt + bboxStreamSize;
            long expectedEndAt = expectedInstructionStreamStartAt + instructionStreamSize;

            var glyphs = new GlyphVector[numGlyphs];
            var allGlyphs = new TempGlyph[numGlyphs];
            var glyphLoaders = new GlyphLoader[numGlyphs];
            var compositeGlyphs = new List<ushort>();
            int contourCount = 0;
            for (ushort i = 0; i < numGlyphs; ++i)
            {
                short numContour = reader.ReadInt16();
                allGlyphs[i] = new TempGlyph(i, numContour);
                if (numContour > 0)
                {
                    contourCount += numContour;

                    // >0 => simple glyph
                    // -1 = composite
                    // 0 = empty glyph
                }
                else if (numContour < 0)
                {
                    // Composite glyph, resolve later.
                    compositeGlyphs.Add(i);
                }
            }

            ushort[] pntPerContours = new ushort[contourCount];
            for (int i = 0; i < contourCount; ++i)
            {
                // Each of these is the number of points of that contour.
                pntPerContours[i] = Read255UInt16(reader);
            }

            // 2) flagStream, flags value for each point.
            // Each byte in flags stream represents one point.
            byte[] flagStream = reader.ReadBytes((int)flagStreamSize);

            // Some composite glyphs have instructions=> so we must check all composite glyphs before read the glyph stream.
            using (var compositeMemoryStream = new MemoryStream())
            {
                reader.BaseStream.Position = expectedCompositeStreamStartAt;
                compositeMemoryStream.Write(reader.ReadBytes((int)compositeStreamSize), 0, (int)compositeStreamSize);
                compositeMemoryStream.Position = 0;

                using (var compositeReader = new BigEndianBinaryReader(compositeMemoryStream, false))
                {
                    for (ushort i = 0; i < compositeGlyphs.Count; ++i)
                    {
                        ushort compositeGlyphIndex = compositeGlyphs[i];
                        allGlyphs[compositeGlyphIndex].CompositeHasInstructions = CompositeHasInstructions(compositeReader);
                    }
                }

                reader.BaseStream.Position = expectedGlyphStreamStartAt;
            }

            int curFlagsIndex = 0;
            int pntContourIndex = 0;
            for (int i = 0; i < allGlyphs.Length; ++i)
            {
                var emptyGlyph = default(GlyphVector);
                glyphs[i] = BuildSimpleGlyphStructure(reader, ref allGlyphs[i], emptyGlyph, pntPerContours, ref pntContourIndex, flagStream, ref curFlagsIndex);
            }

            // Now we read the composite stream again and create composite glyphs.
            for (ushort i = 0; i < compositeGlyphs.Count; ++i)
            {
                int compositeGlyphIndex = compositeGlyphs[i];
                glyphs[compositeGlyphIndex] = ReadCompositeGlyph(glyphs, reader);
            }

            int bitmapCount = (numGlyphs + 7) / 8;
            byte[] bboxBitmap = ExpandBitmap(reader.ReadBytes(bitmapCount));
            for (ushort i = 0; i < numGlyphs; ++i)
            {
                TempGlyph tempGlyph = allGlyphs[i];

                byte hasBbox = bboxBitmap[i];
                if (hasBbox == 1)
                {
                    // Read bbox from the bboxstream.
                    short minX = reader.ReadInt16();
                    short minY = reader.ReadInt16();
                    short maxX = reader.ReadInt16();
                    short maxY = reader.ReadInt16();

                    glyphs[i].Bounds = new Bounds(minX, minY, maxX, maxY);
                }
                else
                {
                    if (tempGlyph.NumContour < 0)
                    {
                        throw new NotSupportedException("composite glyph must have a bounding box");
                    }
                    else if (tempGlyph.NumContour > 0)
                    {
                        // For simple glyphs, if the corresponding bit in the bounding box bit vector is not set,
                        // then derive the bounding box by computing the minimum and maximum x and y coordinates in the outline, and storing that.
                        glyphs[i].Bounds = FindSimpleGlyphBounds(glyphs[i]);
                    }
                }
            }

            reader.BaseStream.Position = expectedInstructionStreamStartAt;

            for (ushort i = 0; i < numGlyphs; ++i)
            {
                TempGlyph tempGlyph = allGlyphs[i];
                if (tempGlyph.InstructionLen > 0)
                {
                    byte[] glyphInstructions = reader.ReadBytes(tempGlyph.InstructionLen);

                    // TODO: use GlyphInstructions
                    // glyphs[i].GlyphInstructions = glyphInstructions;
                }
            }

            for (ushort i = 0; i < numGlyphs; ++i)
            {
                if (glyphs[i].Equals(default(GlyphVector)))
                {
                    glyphLoaders[i] = emptyGlyphLoader;
                    continue;
                }

                glyphLoaders[i] = new TransformedGlyphLoader(glyphs[i]);
            }

            return glyphLoaders;
        }

        private static Bounds FindSimpleGlyphBounds(GlyphVector glyph)
        {
            Vector2[] glyphPoints = glyph.ControlPoints;

            int j = glyphPoints.Length;
            float xMin = float.MaxValue;
            float yMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMax = float.MinValue;

            for (int i = 0; i < j; ++i)
            {
                Vector2 p = glyphPoints[i];
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

            return new Bounds(
                (float)Math.Round(xMin),
                (float)Math.Round(yMin),
                (float)Math.Round(xMax),
                (float)Math.Round(yMax));
        }

        private static GlyphVector BuildSimpleGlyphStructure(
            BigEndianBinaryReader glyphStreamReader,
            ref TempGlyph tmpGlyph,
            GlyphVector emptyGlyph,
            ushort[] pntPerContours,
            ref int pntContourIndex,
            byte[] flagStream,
            ref int flagStreamIndex)
        {
            if (tmpGlyph.NumContour == 0)
            {
                return emptyGlyph;
            }

            if (tmpGlyph.NumContour < 0)
            {
                // Composite glyph, check if this has instruction or not.
                if (tmpGlyph.CompositeHasInstructions)
                {
                    tmpGlyph.InstructionLen = Read255UInt16(glyphStreamReader);
                }

                return default; // Skip composite glyph (resolve later).
            }

            int curX = 0;
            int curY = 0;

            int numContour = tmpGlyph.NumContour;

            ushort[] endContours = new ushort[numContour];
            ushort pointCount = 0;

            for (ushort i = 0; i < numContour; ++i)
            {
                ushort numPoint = pntPerContours[pntContourIndex++];
                pointCount += numPoint;
                endContours[i] = (ushort)(pointCount - 1);
            }

            var glyphPoints = new Vector2[pointCount];
            bool[] onCurves = new bool[pointCount];
            int n = 0;
            for (int i = 0; i < numContour; ++i)
            {
                int endContour = endContours[i];
                for (; n <= endContour; ++n)
                {
                    byte f = flagStream[flagStreamIndex++];

                    // int f1 = (f >> 7); // Most significant 1 bit -> on/off curve.
                    int xyFormat = f & 0x7F; // Remaining 7 bits x, y format.

                    TripleEncodingRecord enc = TripleEncodingTable.EncTable[xyFormat]; // 0-128

                    byte[] packedXY = glyphStreamReader.ReadBytes(enc.ByteCount - 1); // byte count include 1 byte flags, so actual read=> byteCount-1

                    int x;
                    int y;
                    switch (enc.XBits)
                    {
                        default:
                            throw new NotSupportedException();
                        case 0: // 0,8,
                            x = 0;
                            y = enc.Ty(packedXY[0]);
                            break;
                        case 4: // 4,4
                            x = enc.Tx(packedXY[0] >> 4);
                            y = enc.Ty(packedXY[0] & 0xF);
                            break;
                        case 8: // 8,0 or 8,8
                            x = enc.Tx(packedXY[0]);
                            y = (enc.YBits == 8) ?
                                enc.Ty(packedXY[1]) :
                                0;
                            break;
                        case 12: // 12,12
                            x = enc.Tx((packedXY[0] << 4) | (packedXY[1] >> 4));
                            y = enc.Ty(((packedXY[1] & 0xF) << 8) | packedXY[2]);
                            break;
                        case 16: // 16,16
                            x = enc.Tx((packedXY[0] << 8) | packedXY[1]);
                            y = enc.Ty((packedXY[2] << 8) | packedXY[3]);
                            break;
                    }

                    // Most significant 1 bit -> on/off curve.
                    onCurves[n] = (f >> 7) == 0;
                    glyphPoints[n] = new Vector2(curX += x, curY += y);
                }
            }

            // TODO: store instructions.
            Read255UInt16(glyphStreamReader);

            // Calculate bounds later.
            Bounds bounds = default;
            return new GlyphVector(glyphPoints, onCurves, endContours, bounds);
        }

        private static bool CompositeHasInstructions(BigEndianBinaryReader reader)
        {
            bool weHaveInstructions = false;
            CompositeGlyphFlags flags = CompositeGlyphFlags.MoreComponents;
            while ((flags & CompositeGlyphFlags.MoreComponents) != 0)
            {
                flags = reader.ReadUInt16<CompositeGlyphFlags>();
                weHaveInstructions |= (flags & CompositeGlyphFlags.WeHaveInstructions) != 0;
                int argSize = 2; // glyph index
                if ((flags & CompositeGlyphFlags.Args1And2AreWords) != 0)
                {
                    argSize += 4;
                }
                else
                {
                    argSize += 2;
                }

                if ((flags & CompositeGlyphFlags.WeHaveAScale) != 0)
                {
                    argSize += 2;
                }
                else if ((flags & CompositeGlyphFlags.WeHaveXAndYScale) != 0)
                {
                    argSize += 4;
                }
                else if ((flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0)
                {
                    argSize += 8;
                }

                reader.BaseStream.Seek(argSize, SeekOrigin.Current);
            }

            return weHaveInstructions;
        }

        private static GlyphVector ReadCompositeGlyph(GlyphVector[] createdGlyphs, BigEndianBinaryReader reader)
        {
            // Decoding of Composite Glyphs
            // For a composite glyph(nContour == -1), the following steps take the place of (Building Simple Glyph, steps 1 - 5 above):

            // 1a.Read a UInt16 from compositeStream.
            //  This is interpreted as a component flag word as in the TrueType spec.
            //  Based on the flag values, there are between 4 and 14 additional argument bytes,
            //  interpreted as glyph index, arg1, arg2, and optional scale or affine matrix.

            // 2a.Read the number of argument bytes as determined in step 2a from the composite stream,
            // and store these in the reconstructed glyph.
            // If the flag word read in step 2a has the FLAG_MORE_COMPONENTS bit(bit 5) set, go back to step 2a.

            // 3a.If any of the flag words had the FLAG_WE_HAVE_INSTRUCTIONS bit(bit 8) set,
            // then read the instructions from the glyph and store them in the reconstructed glyph,
            // using the same process as described in steps 4 and 5 above (see Building Simple Glyph).
            GlyphVector finalGlyph = default;
            CompositeGlyphFlags flags;
            do
            {
                flags = reader.ReadUInt16<CompositeGlyphFlags>();
                ushort glyphIndex = reader.ReadUInt16();

                // No IEquality<GlyphVector> implementation
                if (createdGlyphs[glyphIndex].ControlPoints is null)
                {
                    // This glyph is not read yet, resolve it first!
                    long storedOffset = reader.BaseStream.Position;
                    createdGlyphs[glyphIndex] = ReadCompositeGlyph(createdGlyphs, reader);
                    reader.BaseStream.Position = storedOffset;
                }

                CompositeGlyphLoader.LoadArguments(reader, flags, out int dx, out int dy);

                Matrix3x2 transform = Matrix3x2.Identity;
                transform.Translation = new Vector2(dx, dy);

                if ((flags & CompositeGlyphFlags.WeHaveAScale) != 0)
                {
                    float scale = reader.ReadF2dot14(); // Format 2.14
                    transform.M11 = scale;
                    transform.M21 = scale;
                }
                else if ((flags & CompositeGlyphFlags.WeHaveXAndYScale) != 0)
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }
                else if ((flags & CompositeGlyphFlags.WeHaveATwoByTwo) != 0)
                {
                    transform.M11 = reader.ReadF2dot14();
                    transform.M12 = reader.ReadF2dot14();
                    transform.M21 = reader.ReadF2dot14();
                    transform.M22 = reader.ReadF2dot14();
                }

                finalGlyph = GlyphVector.Append(finalGlyph, GlyphVector.Transform(createdGlyphs[glyphIndex], transform));
            }
            while ((flags & CompositeGlyphFlags.MoreComponents) != 0);

            if ((flags & CompositeGlyphFlags.WeHaveInstructions) != 0)
            {
                // TODO: Read this later.
                // ushort numInstr = reader.ReadUInt16();
                // byte[] insts = reader.ReadBytes(numInstr);
                // finalGlyph.GlyphInstructions = insts;
            }

            return finalGlyph;
        }

        private static byte[] ExpandBitmap(byte[] orgBBoxBitmap)
        {
            byte[] expandArr = new byte[orgBBoxBitmap.Length * 8];

            int index = 0;
            for (int i = 0; i < orgBBoxBitmap.Length; ++i)
            {
                byte b = orgBBoxBitmap[i];
                expandArr[index++] = (byte)((b >> 7) & 0x1);
                expandArr[index++] = (byte)((b >> 6) & 0x1);
                expandArr[index++] = (byte)((b >> 5) & 0x1);
                expandArr[index++] = (byte)((b >> 4) & 0x1);
                expandArr[index++] = (byte)((b >> 3) & 0x1);
                expandArr[index++] = (byte)((b >> 2) & 0x1);
                expandArr[index++] = (byte)((b >> 1) & 0x1);
                expandArr[index++] = (byte)((b >> 0) & 0x1);
            }

            return expandArr;
        }

        /// <summary>
        /// Reads the UIntBase128 Data Type.
        /// </summary>
        /// <param name="reader">The binary reader using big endian encoding.</param>
        /// <param name="result">The result as uint.</param>
        /// <returns>true, if succeeded.</returns>
        private static bool ReadUIntBase128(BigEndianBinaryReader reader, out uint result)
        {
            // UIntBase128 is a different variable length encoding of unsigned integers,
            // suitable for values up to 2^(32) - 1.
            // A UIntBase128 encoded number is a sequence of bytes for which the most significant bit
            // is set for all but the last byte,
            // and clear for the last byte.
            //
            // The number itself is base 128 encoded in the lower 7 bits of each byte.
            // Thus, a decoding procedure for a UIntBase128 is:
            // start with value = 0.
            // Consume a byte, setting value = old value times 128 + (byte bitwise - and 127).
            // Repeat last step until the most significant bit of byte is false.
            //
            // UIntBase128 encoding format allows a possibility of sub-optimal encoding,
            // where e.g.the same numerical value can be represented with variable number of bytes(utilizing leading 'zeros').
            // For example, the value 63 could be encoded as either one byte 0x3F or two(or more) bytes: [0x80, 0x3f].
            // An encoder must not allow this to happen and must produce shortest possible encoding.
            // A decoder MUST reject the font file if it encounters a UintBase128 - encoded value with leading zeros(a value that starts with the byte 0x80),
            // if UintBase128 - encoded sequence is longer than 5 bytes,
            // or if a UintBase128 - encoded value exceeds 232 - 1.
            uint accum = 0;
            result = 0;
            for (int i = 0; i < 5; ++i)
            {
                byte data_byte = reader.ReadByte();

                // No leading 0's
                if (i == 0 && data_byte == 0x80)
                {
                    return false;
                }

                // If any of top 7 bits are set then << 7 would overflow.
                if ((accum & 0xFE000000) != 0)
                {
                    return false;
                }

                accum = (accum << 7) | (uint)(data_byte & 0x7F);

                // Spin until most significant bit of data byte is false.
                if ((data_byte & 0x80) == 0)
                {
                    result = accum;
                    return true;
                }
            }

            // UIntBase128 sequence exceeds 5 bytes.
            return false;
        }

        /// <summary>
        /// Reads the UIntBase255 Data Type.
        /// </summary>
        /// <param name="reader">The binary reader using big endian encoding.</param>
        /// <returns>The UIntBase255 result.</returns>
        private static ushort Read255UInt16(BigEndianBinaryReader reader)
        {
            // 255UInt16 Variable-length encoding of a 16-bit unsigned integer for optimized intermediate font data storage.
            // 255UInt16 is a variable-length encoding of an unsigned integer
            // in the range 0 to 65535 inclusive.
            // This data type is intended to be used as intermediate representation of various font values,
            // which are typically expressed as UInt16 but represent relatively small values.
            // Depending on the encoded value, the length of the data field may be one to three bytes,
            // where the value of the first byte either represents the small value itself or is treated as a code that defines the format of the additional byte(s).
            byte code = reader.ReadByte();
            if (code == WordCode)
            {
                int value = reader.ReadByte();
                value <<= 8;
                value &= 0xff00;
                int value2 = reader.ReadByte();
                value |= value2 & 0x00ff;

                return (ushort)value;
            }
            else if (code == OneMoreByteCode1)
            {
                return (ushort)(reader.ReadByte() + LowestUCode);
            }
            else if (code == OneMoreByteCode2)
            {
                return (ushort)(reader.ReadByte() + (LowestUCode * 2));
            }
            else
            {
                return code;
            }
        }
    }
}
