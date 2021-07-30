// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General.Glyphs;

namespace SixLabors.Fonts.Tables
{
    // Source code is based on https://github.com/LayoutFarm/Typography
    // see https://github.com/LayoutFarm/Typography/blob/master/Typography.OpenFont/WebFont/Woff2Reader.cs
    internal class Woff2Utils
    {
        private static readonly string[] KnownTableTags =
        {
            TableNames.Cmap, TableNames.Head, TableNames.Hhea, TableNames.Hmtx, TableNames.Maxp, TableNames.Name, TableNames.Os2,
            TableNames.Post, TableNames.Cvt, TableNames.Fpgm, TableNames.Glyph, TableNames.Loca, TableNames.Prep, TableNames.Cff,
            TableNames.Vorg, TableNames.Ebdt, TableNames.Eblc, TableNames.Gasp, TableNames.Hdmx, TableNames.Kern, TableNames.Ltsh,
            TableNames.Pclt, TableNames.Vdmx, TableNames.Vhea, TableNames.Vmtx, TableNames.Base, TableNames.Gdef, TableNames.Gpos,
            TableNames.Gsub, TableNames.Ebsc, TableNames.Jstf, TableNames.Math, TableNames.Cbdt, TableNames.Cblc, TableNames.Colr,
            TableNames.Cpal, TableNames.Svg, TableNames.Sbix, TableNames.Acnt, TableNames.Avar, TableNames.Bdat, TableNames.Bloc,
            TableNames.Blsn, TableNames.Cvar, TableNames.Fdsc, TableNames.Feat, TableNames.Fmtx, TableNames.Fvar, TableNames.Gvar,
            TableNames.Hsty, TableNames.Just, TableNames.Lcar, TableNames.Mort, TableNames.Morx, TableNames.Opbd, TableNames.Prop,
            TableNames.Trak, TableNames.Zapf, TableNames.Silf, TableNames.Glat, TableNames.Gloc,  TableNames.FEAT, TableNames.Sill,
            "...." // Arbitrary tag follows.
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
            byte flags = reader.ReadByte();
            int knowTable = flags & 0x1F;
            string tableName = (knowTable < 63) ? KnownTableTags[knowTable] : reader.ReadTag();
            byte preprocessingTransformation = (byte)((flags >> 5) & 0x3);

            if (!ReadUIntBase128(reader, out uint tableOrigLength))
            {
                throw new FontException("Error parsing woff2 table header");
            }

            var woffTableHeader = new Woff2TableHeader(tableName, 0, expectedTableStartAt, tableOrigLength);
            uint tableTransformLength = 0;
            nextExpectedTableStartAt = expectedTableStartAt;
            switch (preprocessingTransformation)
            {
                default:
                    break;
                case 0:
                    if (tableName == TableNames.Glyph)
                    {
                        if (!ReadUIntBase128(reader, out tableTransformLength))
                        {
                            throw new FontException("Error parsing woff2 table header");
                        }

                        nextExpectedTableStartAt += tableTransformLength;
                    }
                    else if (tableName == TableNames.Loca)
                    {
                        if (!ReadUIntBase128(reader, out tableTransformLength))
                        {
                            throw new FontException("Error parsing woff2 table header");
                        }

                        nextExpectedTableStartAt += tableTransformLength;
                    }
                    else
                    {
                        nextExpectedTableStartAt += tableOrigLength;
                    }

                    break;

                case 1:
                    nextExpectedTableStartAt += tableOrigLength;
                    break;

                case 2:
                    nextExpectedTableStartAt += tableOrigLength;
                    break;

                case 3:
                    nextExpectedTableStartAt += tableOrigLength;
                    break;
            }

            if (tableName == TableNames.Glyph && preprocessingTransformation == 0)
            {
                woffTableHeader = new Woff2TableHeader(tableName, 0, expectedTableStartAt, tableTransformLength);
            }

            if (tableName == TableNames.Loca && preprocessingTransformation == 0)
            {
                woffTableHeader = new Woff2TableHeader(tableName, 0, expectedTableStartAt, tableTransformLength);
            }

            return woffTableHeader;
        }

        public static GlyphLoader[] LoadAllGlyphs(BigEndianBinaryReader reader, EmptyGlyphLoader emptyGlyphLoader)
        {
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
                    // -1 = compound
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
                (short)Math.Round(xMin),
                (short)Math.Round(yMin),
                (short)Math.Round(xMax),
                (short)Math.Round(yMax));
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
            var bounds = default(Bounds);
            var glyphVector = new GlyphVector(glyphPoints, onCurves, endContours, bounds);
            return glyphVector;
        }

        private static bool CompositeHasInstructions(BigEndianBinaryReader reader)
        {
            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();
                short arg1 = 0;
                short arg2 = 0;
                ushort arg1and2 = 0;
                if (flags.HasFlag(CompositeGlyphFlags.ArgsAreWords))
                {
                    arg1 = reader.ReadInt16();
                    arg2 = reader.ReadInt16();
                }
                else
                {
                    arg1and2 = reader.ReadUInt16();
                }

                float xscale = 1;
                float scale01 = 0;
                float scale10 = 0;
                float yscale = 1;

                // bool useMatrix = false;
                // bool hasScale = false;
                if (flags.HasFlag(CompositeGlyphFlags.WeHaveAScale))
                {
                    // If the bit WE_HAVE_A_SCALE is set, the scale value is read in 2.14 format-the value can be between -2 to almost +2.
                    // The glyph will be scaled by this value before grid-fitting.
                    xscale = yscale = reader.ReadF2dot14();

                    // hasScale = true;
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveXAndYScale))
                {
                    xscale = reader.ReadF2dot14();
                    yscale = reader.ReadF2dot14();

                    // hasScale = true;
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    // The bit WE_HAVE_A_TWO_BY_TWO allows for linear transformation of the X and Y coordinates by specifying a 2 × 2 matrix.
                    // This could be used for scaling and 90-degree*** rotations of the glyph components, for example.

                    // 2x2 matrix

                    // The purpose of USE_MY_METRICS is to force the lsb and rsb to take on a desired value.
                    // For example, an i-circumflex (U+00EF) is often composed of the circumflex and a dotless-i.
                    // In order to force the composite to have the same metrics as the dotless-i,
                    // set USE_MY_METRICS for the dotless-i component of the composite.
                    // Without this bit, the rsb and lsb would be calculated from the hmtx entry for the composite
                    // (or would need to be explicitly set with TrueType instructions).

                    // Note that the behavior of the USE_MY_METRICS operation is undefined for rotated composite components.
                    // useMatrix = true;
                    // hasScale = true;
                    xscale = reader.ReadF2dot14();
                    scale01 = reader.ReadF2dot14();
                    scale10 = reader.ReadF2dot14();
                    yscale = reader.ReadF2dot14();
                }
            }
            while (flags.HasFlag(CompositeGlyphFlags.MoreComponents));

            return flags.HasFlag(CompositeGlyphFlags.WeHaveInstructions);
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
            var finalGlyph = default(GlyphVector);
            CompositeGlyphFlags flags;
            do
            {
                flags = (CompositeGlyphFlags)reader.ReadUInt16();
                ushort glyphIndex = reader.ReadUInt16();
                if (createdGlyphs[glyphIndex].Equals(default(GlyphVector)))
                {
                    // This glyph is not read yet, resolve it first!
                    long storedOffset = reader.BaseStream.Position;
                    GlyphVector missingGlyph = ReadCompositeGlyph(createdGlyphs, reader);
                    createdGlyphs[glyphIndex] = missingGlyph;
                    reader.BaseStream.Position = storedOffset;
                }

                var glyphClone = (GlyphVector)createdGlyphs[glyphIndex].DeepClone();
                var newGlyph = (GlyphVector)createdGlyphs[glyphIndex].DeepClone();

                short arg1 = 0;
                short arg2 = 0;
                ushort arg1and2 = 0;

                if (flags.HasFlag(CompositeGlyphFlags.ArgsAreWords))
                {
                    arg1 = reader.ReadInt16();
                    arg2 = reader.ReadInt16();
                }
                else
                {
                    arg1and2 = reader.ReadUInt16();
                }

                //-----------------------------------------
                float xscale = 1;
                float scale01 = 0;
                float scale10 = 0;
                float yscale = 1;

                bool useMatrix = false;
                bool hasScale = false;
                if (flags.HasFlag(CompositeGlyphFlags.WeHaveAScale))
                {
                    // If the bit WE_HAVE_A_SCALE is set,
                    // the scale value is read in 2.14 format-the value can be between -2 to almost +2.
                    // The glyph will be scaled by this value before grid-fitting.
                    xscale = yscale = reader.ReadF2dot14();
                    hasScale = true;
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveXAndYScale))
                {
                    xscale = reader.ReadF2dot14();
                    yscale = reader.ReadF2dot14();
                    hasScale = true;
                }
                else if (flags.HasFlag(CompositeGlyphFlags.WeHaveATwoByTwo))
                {
                    // The bit WE_HAVE_A_TWO_BY_TWO allows for linear transformation of the X and Y coordinates by specifying a 2 × 2 matrix.
                    // This could be used for scaling and 90-degree*** rotations of the glyph components, for example.

                    // 2x2 matrix

                    // The purpose of USE_MY_METRICS is to force the lsb and rsb to take on a desired value.
                    // For example, an i-circumflex (U+00EF) is often composed of the circumflex and a dotless-i.
                    // In order to force the composite to have the same metrics as the dotless-i,
                    // set USE_MY_METRICS for the dotless-i component of the composite.
                    // Without this bit, the rsb and lsb would be calculated from the hmtx entry for the composite
                    // (or would need to be explicitly set with TrueType instructions).

                    // Note that the behavior of the USE_MY_METRICS operation is undefined for rotated composite components.
                    useMatrix = true;
                    hasScale = true;
                    xscale = reader.ReadF2dot14();
                    scale01 = reader.ReadF2dot14();
                    scale10 = reader.ReadF2dot14();
                    yscale = reader.ReadF2dot14();
                }

                //--------------------------------------------------------------------
                if (flags.HasFlag(CompositeGlyphFlags.ArgsAreXYValues))
                {
                    // Argument1 and argument2 can be either x and y offsets to be added to the glyph or two point numbers.
                    // x and y offsets to be added to the glyph
                    // When arguments 1 and 2 are an x and a y offset instead of points and the bit ROUND_XY_TO_GRID is set to 1,
                    // the values are rounded to those of the closest grid lines before they are added to the glyph.
                    // X and Y offsets are described in FUnits.
                    if (useMatrix)
                    {
                        newGlyph.TtfTransformWithMatrix(xscale, scale01, scale10, yscale);
                        newGlyph.TtfOffsetXy(arg1, arg2);
                    }
                    else
                    {
                        if (hasScale)
                        {
                            if (!(xscale == 1.0 && yscale == 1.0))
                            {
                                newGlyph.TtfTransformWithMatrix(xscale, 0, 0, yscale);
                            }

                            newGlyph.TtfOffsetXy(arg1, arg2);
                        }
                        else
                        {
                            if (flags.HasFlag(CompositeGlyphFlags.RoundXYToGrid))
                            {
                                // TODO: implement round xy to grid
                            }

                            // just offset.
                            newGlyph.TtfOffsetXy(arg1, arg2);
                        }
                    }
                }
                else
                {
                    // two point numbers.
                    // the first point number indicates the point that is to be matched to the new glyph.
                    // The second number indicates the new glyph's “matched” point.
                    // Once a glyph is added,its point numbers begin directly after the last glyphs (endpoint of first glyph + 1)
                }

                if (finalGlyph.Equals(default(GlyphVector)))
                {
                    finalGlyph = newGlyph;
                }
                else
                {
                    finalGlyph.TtfAppendGlyph(newGlyph);
                }
            }
            while (flags.HasFlag(CompositeGlyphFlags.MoreComponents));

            if (flags.HasFlag(CompositeGlyphFlags.WeHaveInstructions))
            {
                // Read this later.
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
