using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General.Name;
using SixLabors.Fonts.Utilities;
using SixLabors.Fonts.WellKnownIds;

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal class OS2Table : Table
    {
        private const string TableName = "OS/2";
        private ushort fsSelection;
        private ushort fsType;
        private byte[] panose;
        private short sCapHeight;
        private short sFamilyClass;
        private short sxHeight;
        private string tag;
        private ushort ulCodePageRange1;
        private ushort ulCodePageRange2;
        private uint ulUnicodeRange1;
        private uint ulUnicodeRange2;
        private uint ulUnicodeRange3;
        private uint ulUnicodeRange4;
        private ushort usBreakChar;
        private ushort usDefaultChar;
        private ushort usFirstCharIndex;
        private ushort usLastCharIndex;
        private ushort usLowerOpticalPointSize;
        private ushort usMaxContext;
        private ushort usUpperOpticalPointSize;
        private ushort usWeightClass;
        private ushort usWidthClass;
        private ushort usWinAscent;
        private ushort usWinDescent;
        private short xAvgCharWidth1;
        private short xAvgCharWidth2;
        private short yStrikeoutPosition;
        private short yStrikeoutSize;
        private short ySubscriptXOffset;
        private short ySubscriptXSize;
        private short ySubscriptYOffset;
        private short ySubscriptYSize;
        private short ySuperscriptXOffset;
        private short ySuperscriptXSize;
        private short ySuperscriptYOffset;
        private short ySuperscriptYSize;

        public int TypoAscender { get; }

        public short TypoDescender { get; }

        public short TypoLineGap { get; }

        public static OS2Table Load(FontReader reader)
        {
            using (var r = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(r);
            }
        }

        public static OS2Table Load(BinaryReader reader)
        {
            // Version 1.0
            // Type   | Name                   | Comments
            // -------|------------------------|-----------------------
            // uint16 |version                 | 0x0005
            // int16  |xAvgCharWidth           |
            // uint16 |usWeightClass           |
            // uint16 |usWidthClass            |
            // uint16 |fsType                  |
            // int16  |ySubscriptXSize         |
            // int16  |ySubscriptYSize         |
            // int16  |ySubscriptXOffset       |
            // int16  |ySubscriptYOffset       |
            // int16  |ySuperscriptXSize       |
            // int16  |ySuperscriptYSize       |
            // int16  |ySuperscriptXOffset     |
            // int16  |ySuperscriptYOffset     |
            // int16  |yStrikeoutSize          |
            // int16  |yStrikeoutPosition      |
            // int16  |sFamilyClass            |
            // uint8  |panose[10]              |
            // uint32 |ulUnicodeRange1         | Bits 0–31
            // uint32 |ulUnicodeRange2         | Bits 32–63
            // uint32 |ulUnicodeRange3         | Bits 64–95
            // uint32 |ulUnicodeRange4         | Bits 96–127
            // Tag    |achVendID               |
            // uint16 |fsSelection             |
            // uint16 |usFirstCharIndex        |
            // uint16 |usLastCharIndex         |
            // int16  |sTypoAscender           |
            // int16  |sTypoDescender          |
            // int16  |sTypoLineGap            |
            // uint16 |usWinAscent             |
            // uint16 |usWinDescent            |
            // uint32 |ulCodePageRange1        | Bits 0–31
            // uint32 |ulCodePageRange2        | Bits 32–63
            // int16  |sxHeight                |
            // int16  |sCapHeight              |
            // uint16 |usDefaultChar           |
            // uint16 |usBreakChar             |
            // uint16 |usMaxContext            |
            // uint16 |usLowerOpticalPointSize |
            // uint16 |usUpperOpticalPointSize |
            var version = reader.ReadUInt16(); // assert 0x0005
            var xAvgCharWidth = reader.ReadInt16();
            var usWeightClass = reader.ReadUInt16();
            var usWidthClass = reader.ReadUInt16();
            var fsType = reader.ReadUInt16();
            var ySubscriptXSize = reader.ReadInt16();
            var ySubscriptYSize = reader.ReadInt16();
            var ySubscriptXOffset = reader.ReadInt16();
            var ySubscriptYOffset = reader.ReadInt16();

            var ySuperscriptXSize = reader.ReadInt16();
            var ySuperscriptYSize = reader.ReadInt16();
            var ySuperscriptXOffset = reader.ReadInt16();
            var ySuperscriptYOffset = reader.ReadInt16();

            var yStrikeoutSize = reader.ReadInt16();
            var yStrikeoutPosition = reader.ReadInt16();
            var sFamilyClass = reader.ReadInt16();
            var panose = reader.ReadUInt8Array(10);
            var ulUnicodeRange1 = reader.ReadUInt32(); // Bits 0–31
            var ulUnicodeRange2 = reader.ReadUInt32(); // Bits 32–63
            var ulUnicodeRange3 = reader.ReadUInt32(); // Bits 64–95
            var ulUnicodeRange4 = reader.ReadUInt32(); // Bits 96–127
            var tag = reader.ReadTag();
            var fsSelection = reader.ReadUInt16();
            var usFirstCharIndex = reader.ReadUInt16();
            var usLastCharIndex = reader.ReadUInt16();
            var sTypoAscender = reader.ReadInt16();
            var sTypoDescender = reader.ReadInt16();
            var sTypoLineGap = reader.ReadInt16();
            var usWinAscent = reader.ReadUInt16();
            var usWinDescent = reader.ReadUInt16();

            if (version == 0)
            {
                return new OS2Table(
                    xAvgCharWidth,
                    xAvgCharWidth,
                usWeightClass,
                usWidthClass,
                fsType,
                ySubscriptXSize,
                ySubscriptYSize,
                ySubscriptXOffset,
                ySubscriptYOffset,
                ySuperscriptXSize,
                ySuperscriptYSize,
                ySuperscriptXOffset,
                ySuperscriptYOffset,
                yStrikeoutSize,
                yStrikeoutPosition,
                sFamilyClass,
                panose,
                ulUnicodeRange1,
                ulUnicodeRange2,
                ulUnicodeRange3,
                ulUnicodeRange4,
                tag,
                fsSelection,
                usFirstCharIndex,
                usLastCharIndex,
                sTypoAscender,
                sTypoDescender,
                sTypoLineGap,
                usWinAscent,
                usWinDescent);
            }

            ushort ulCodePageRange1 = 0;
            ushort ulCodePageRange2 = 0;
            short sxHeight = 0;
            short sCapHeight = 0;

            ushort usDefaultChar = 0;
            ushort usBreakChar = 0;
            ushort usMaxContext = 0;

            ulCodePageRange1 = reader.ReadUInt16(); // Bits 0–31
            ulCodePageRange2 = reader.ReadUInt16(); // Bits 32–63
            sxHeight = reader.ReadInt16();
            sCapHeight = reader.ReadInt16();

            usDefaultChar = reader.ReadUInt16();
            usBreakChar = reader.ReadUInt16();
            usMaxContext = reader.ReadUInt16();
            if (version < 5)
            {
                return new OS2Table(
                    xAvgCharWidth,
                 xAvgCharWidth,
             usWeightClass,
             usWidthClass,
             fsType,
             ySubscriptXSize,
             ySubscriptYSize,
             ySubscriptXOffset,
             ySubscriptYOffset,
             ySuperscriptXSize,
             ySuperscriptYSize,
             ySuperscriptXOffset,
             ySuperscriptYOffset,
             yStrikeoutSize,
             yStrikeoutPosition,
             sFamilyClass,
             panose,
             ulUnicodeRange1,
             ulUnicodeRange2,
             ulUnicodeRange3,
             ulUnicodeRange4,
             tag,
             fsSelection,
             usFirstCharIndex,
             usLastCharIndex,
             sTypoAscender,
             sTypoDescender,
             sTypoLineGap,
             usWinAscent,
             usWinDescent,
             ulCodePageRange1,
             ulCodePageRange2,
             sxHeight,
             sCapHeight,
             usDefaultChar,
             usBreakChar,
             usMaxContext);
            }

            ushort usLowerOpticalPointSize = reader.ReadUInt16();
            ushort usUpperOpticalPointSize = reader.ReadUInt16();

            return new OS2Table(
                xAvgCharWidth,
                 xAvgCharWidth,
             usWeightClass,
             usWidthClass,
             fsType,
             ySubscriptXSize,
             ySubscriptYSize,
             ySubscriptXOffset,
             ySubscriptYOffset,
             ySuperscriptXSize,
             ySuperscriptYSize,
             ySuperscriptXOffset,
             ySuperscriptYOffset,
             yStrikeoutSize,
             yStrikeoutPosition,
             sFamilyClass,
             panose,
             ulUnicodeRange1,
             ulUnicodeRange2,
             ulUnicodeRange3,
             ulUnicodeRange4,
             tag,
             fsSelection,
             usFirstCharIndex,
             usLastCharIndex,
             sTypoAscender,
             sTypoDescender,
             sTypoLineGap,
             usWinAscent,
             usWinDescent,
             ulCodePageRange1,
             ulCodePageRange2,
             sxHeight,
             sCapHeight,
             usDefaultChar,
             usBreakChar,
             usMaxContext,
             usLowerOpticalPointSize,
             usUpperOpticalPointSize);
        }

        public OS2Table(short xAvgCharWidth1, short xAvgCharWidth2, ushort usWeightClass, ushort usWidthClass, ushort fsType, short ySubscriptXSize, short ySubscriptYSize, short ySubscriptXOffset, short ySubscriptYOffset, short ySuperscriptXSize, short ySuperscriptYSize, short ySuperscriptXOffset, short ySuperscriptYOffset, short yStrikeoutSize, short yStrikeoutPosition, short sFamilyClass, byte[] panose, uint ulUnicodeRange1, uint ulUnicodeRange2, uint ulUnicodeRange3, uint ulUnicodeRange4, string tag, ushort fsSelection, ushort usFirstCharIndex, ushort usLastCharIndex, short sTypoAscender, short sTypoDescender, short sTypoLineGap, ushort usWinAscent, ushort usWinDescent)
        {
            this.xAvgCharWidth1 = xAvgCharWidth1;
            this.xAvgCharWidth2 = xAvgCharWidth2;
            this.usWeightClass = usWeightClass;
            this.usWidthClass = usWidthClass;
            this.fsType = fsType;
            this.ySubscriptXSize = ySubscriptXSize;
            this.ySubscriptYSize = ySubscriptYSize;
            this.ySubscriptXOffset = ySubscriptXOffset;
            this.ySubscriptYOffset = ySubscriptYOffset;
            this.ySuperscriptXSize = ySuperscriptXSize;
            this.ySuperscriptYSize = ySuperscriptYSize;
            this.ySuperscriptXOffset = ySuperscriptXOffset;
            this.ySuperscriptYOffset = ySuperscriptYOffset;
            this.yStrikeoutSize = yStrikeoutSize;
            this.yStrikeoutPosition = yStrikeoutPosition;
            this.sFamilyClass = sFamilyClass;
            this.panose = panose;
            this.ulUnicodeRange1 = ulUnicodeRange1;
            this.ulUnicodeRange2 = ulUnicodeRange2;
            this.ulUnicodeRange3 = ulUnicodeRange3;
            this.ulUnicodeRange4 = ulUnicodeRange4;
            this.tag = tag;
            this.fsSelection = fsSelection;
            this.usFirstCharIndex = usFirstCharIndex;
            this.usLastCharIndex = usLastCharIndex;
            this.TypoAscender = sTypoAscender;
            this.TypoDescender = sTypoDescender;
            this.TypoLineGap = sTypoLineGap;
            this.usWinAscent = usWinAscent;
            this.usWinDescent = usWinDescent;
        }

        public OS2Table(short xAvgCharWidth1, short xAvgCharWidth2, ushort usWeightClass, ushort usWidthClass, ushort fsType, short ySubscriptXSize, short ySubscriptYSize, short ySubscriptXOffset, short ySubscriptYOffset, short ySuperscriptXSize, short ySuperscriptYSize, short ySuperscriptXOffset, short ySuperscriptYOffset, short yStrikeoutSize, short yStrikeoutPosition, short sFamilyClass, byte[] panose, uint ulUnicodeRange1, uint ulUnicodeRange2, uint ulUnicodeRange3, uint ulUnicodeRange4, string tag, ushort fsSelection, ushort usFirstCharIndex, ushort usLastCharIndex, short sTypoAscender, short sTypoDescender, short sTypoLineGap, ushort usWinAscent, ushort usWinDescent, ushort ulCodePageRange1, ushort ulCodePageRange2, short sxHeight, short sCapHeight, ushort usDefaultChar, ushort usBreakChar, ushort usMaxContext)
            : this(xAvgCharWidth1, xAvgCharWidth2, usWeightClass, usWidthClass, fsType, ySubscriptXSize, ySubscriptYSize, ySubscriptXOffset, ySubscriptYOffset, ySuperscriptXSize, ySuperscriptYSize, ySuperscriptXOffset, ySuperscriptYOffset, yStrikeoutSize, yStrikeoutPosition, sFamilyClass, panose, ulUnicodeRange1, ulUnicodeRange2, ulUnicodeRange3, ulUnicodeRange4, tag, fsSelection, usFirstCharIndex, usLastCharIndex, sTypoAscender, sTypoDescender, sTypoLineGap, usWinAscent, usWinDescent)
        {
            this.ulCodePageRange1 = ulCodePageRange1;
            this.ulCodePageRange2 = ulCodePageRange2;
            this.sxHeight = sxHeight;
            this.sCapHeight = sCapHeight;
            this.usDefaultChar = usDefaultChar;
            this.usBreakChar = usBreakChar;
            this.usMaxContext = usMaxContext;
        }

        public OS2Table(short xAvgCharWidth1, short xAvgCharWidth2, ushort usWeightClass, ushort usWidthClass, ushort fsType, short ySubscriptXSize, short ySubscriptYSize, short ySubscriptXOffset, short ySubscriptYOffset, short ySuperscriptXSize, short ySuperscriptYSize, short ySuperscriptXOffset, short ySuperscriptYOffset, short yStrikeoutSize, short yStrikeoutPosition, short sFamilyClass, byte[] panose, uint ulUnicodeRange1, uint ulUnicodeRange2, uint ulUnicodeRange3, uint ulUnicodeRange4, string tag, ushort fsSelection, ushort usFirstCharIndex, ushort usLastCharIndex, short sTypoAscender, short sTypoDescender, short sTypoLineGap, ushort usWinAscent, ushort usWinDescent, ushort ulCodePageRange1, ushort ulCodePageRange2, short sxHeight, short sCapHeight, ushort usDefaultChar, ushort usBreakChar, ushort usMaxContext, ushort usLowerOpticalPointSize, ushort usUpperOpticalPointSize)
            : this(xAvgCharWidth1, xAvgCharWidth2, usWeightClass, usWidthClass, fsType, ySubscriptXSize, ySubscriptYSize, ySubscriptXOffset, ySubscriptYOffset, ySuperscriptXSize, ySuperscriptYSize, ySuperscriptXOffset, ySuperscriptYOffset, yStrikeoutSize, yStrikeoutPosition, sFamilyClass, panose, ulUnicodeRange1, ulUnicodeRange2, ulUnicodeRange3, ulUnicodeRange4, tag, fsSelection, usFirstCharIndex, usLastCharIndex, sTypoAscender, sTypoDescender, sTypoLineGap, usWinAscent, usWinDescent, ulCodePageRange1, ulCodePageRange2, sxHeight, sCapHeight, usDefaultChar, usBreakChar, usMaxContext)
        {
            this.usLowerOpticalPointSize = usLowerOpticalPointSize;
            this.usUpperOpticalPointSize = usUpperOpticalPointSize;
        }
    }
}
