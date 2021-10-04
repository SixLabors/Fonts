// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.IO;

namespace SixLabors.Fonts.Tables.General
{
    [DebuggerDisplay("Format: {Format}, StartGlyph: {StartGlyph}")]
    internal sealed class ClassDefinitionTable
    {
        public ushort Format { get; internal set; }

        public ushort StartGlyph { get; internal set; }

        public ushort[]? ClassValueArray { get; internal set; }

        public ClassRangeRecord[]? ClassRangeRecords { get; internal set; }

        public static ClassDefinitionTable Load(BigEndianBinaryReader reader, long offset)
        {
            // Class Definition Table, Format 1
            // Type      | Name                        | Description
            // ----------|-----------------------------|--------------------------------------------------------------------------------------------------------
            // uint16    | classFormat                 | Format identifier — format = 1.
            // ----------|-----------------------------|--------------------------------------------------------------------------------------------------------
            // uint16    | startGlyphID                | First glyph ID of the classValueArray.
            // ----------|-----------------------------|--------------------------------------------------------------------------------------------------------
            // uint16    | glyphCount                  | Size of the classValueArray.
            // ----------|-----------------------------|--------------------------------------------------------------------------------------------------------
            // uint16    | classValueArray[glyphCount] | Array of Class Values — one per glyph ID.
            // ----------|-----------------------------|--------------------------------------------------------------------------------------------------------

            // Class Definition Table, Format 2
            // Type             | Name                             | Description
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16           | classFormat                      | Format identifier — format = 2.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16           | classRangeCount                  | Number of ClassRangeRecords.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // ClassRangeRecord classRangeRecords[classRangeCount] | Array of ClassRangeRecords — ordered by startGlyphID.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------

            // Class Range Record
            // Type             | Name                             | Description
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16           | startGlyphID                     | First glyph ID in the range.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16           | endGlyphID                       | Last glyph ID in the range.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            // uint16           | class                            | Applied to all glyphs in the range.
            // -----------------|----------------------------------|--------------------------------------------------------------------------------------------------------
            reader.Seek(offset, SeekOrigin.Begin);
            ushort format = reader.ReadUInt16();

            var classDefinitionTable = new ClassDefinitionTable
            {
                Format = format
            };
            switch (format)
            {
                case 1:
                    classDefinitionTable.StartGlyph = reader.ReadUInt16();
                    ushort glyphCount = reader.ReadUInt16();
                    classDefinitionTable.ClassValueArray = reader.ReadUInt16Array(glyphCount);
                    break;
                case 2:
                    ushort classRangeCount = reader.ReadUInt16();
                    var records = new ClassRangeRecord[classRangeCount];
                    for (int i = 0; i < classRangeCount; ++i)
                    {
                        records[i] = new ClassRangeRecord(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16());
                    }

                    classDefinitionTable.ClassRangeRecords = records;
                    break;
                default:
                    throw new InvalidFontFileException($"Invalid value for class definition format {format}. Should be '1' or '2'.");
            }

            return classDefinitionTable;
        }
    }
}
