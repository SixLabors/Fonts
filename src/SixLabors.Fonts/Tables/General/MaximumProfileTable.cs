// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.General
{
    [TableName(TableName)]
    internal sealed class MaximumProfileTable : Table
    {
        private const string TableName = "maxp";

        public MaximumProfileTable(ushort numGlyphs)
        {
            this.GlyphCount = numGlyphs;
        }

        public MaximumProfileTable(ushort numGlyphs, ushort maxPoints, ushort maxContours, ushort maxCompositePoints, ushort maxCompositeContours, ushort maxZones, ushort maxTwilightPoints, ushort maxStorage, ushort maxFunctionDefs, ushort maxInstructionDefs, ushort maxStackElements, ushort maxSizeOfInstructions, ushort maxComponentElements, ushort maxComponentDepth)
                : this(numGlyphs)
        {
            this.MaxPoints = maxPoints;
            this.MaxContours = maxContours;
            this.MaxCompositePoints = maxCompositePoints;
            this.MaxCompositeContours = maxCompositeContours;
            this.MaxZones = maxZones;
            this.MaxTwilightPoints = maxTwilightPoints;
            this.MaxStorage = maxStorage;
            this.MaxFunctionDefs = maxFunctionDefs;
            this.MaxInstructionDefs = maxInstructionDefs;
            this.MaxStackElements = maxStackElements;
            this.MaxSizeOfInstructions = maxSizeOfInstructions;
            this.MaxComponentElements = maxComponentElements;
            this.MaxComponentDepth = maxComponentDepth;
        }

        public ushort MaxPoints { get; }

        public ushort MaxContours { get; }

        public ushort MaxCompositePoints { get; }

        public ushort MaxCompositeContours { get; }

        public ushort MaxZones { get; }

        public ushort MaxTwilightPoints { get; }

        public ushort MaxStorage { get; }

        public ushort MaxFunctionDefs { get; }

        public ushort MaxInstructionDefs { get; }

        public ushort MaxStackElements { get; }

        public ushort MaxSizeOfInstructions { get; }

        public ushort MaxComponentElements { get; }

        public ushort MaxComponentDepth { get; }

        public ushort GlyphCount { get; }

        public static MaximumProfileTable Load(FontReader reader)
        {
            using (BinaryReader r = reader.GetReaderAtTablePosition(TableName))
            {
                return Load(r);
            }
        }

        public static MaximumProfileTable Load(BinaryReader reader)
        {
            // This table establishes the memory requirements for this font.Fonts with CFF data must use Version 0.5 of this table, specifying only the numGlyphs field.Fonts with TrueType outlines must use Version 1.0 of this table, where all data is required.
            // Version 0.5
            // Type   | Name                 | Description
            // -------|----------------------|---------------------------------------
            // Fixed  | Table version number | 0x00005000 for version 0.5 (Note the difference in the representation of a non - zero fractional part, in Fixed numbers.)
            // uint16 | numGlyphs            | The number of glyphs in the font.
            float version = reader.ReadFixed();
            ushort numGlyphs = reader.ReadUInt16();
            if (version == 0.5)
            {
                return new MaximumProfileTable(numGlyphs);
            }

            // Version 1.0
            // Type   | Name                  | Description
            // -------|-----------------------|---------------------------------------
            // *Fixed | Table version number  | 0x00010000 for version 1.0.
            // *uint16| numGlyphs             | The number of glyphs in the font.
            // uint16 | maxPoints             | Maximum points in a non - composite glyph.
            // uint16 | maxContours           | Maximum contours in a non - composite glyph.
            // uint16 | maxCompositePoints    | Maximum points in a composite glyph.
            // uint16 | maxCompositeContours  | Maximum contours in a composite glyph.
            // uint16 | maxZones              | 1 if instructions do not use the twilight zone (Z0), or 2 if instructions do use Z0; should be set to 2 in most cases.
            // uint16 | maxTwilightPoints     | Maximum points used in Z0.
            // uint16 | maxStorage            | Number of Storage Area locations.
            // uint16 | maxFunctionDefs       | Number of FDEFs, equals to the highest function number +1.
            // uint16 | maxInstructionDefs    | Number of IDEFs.
            // uint16 | maxStackElements      | Maximum stack depth2.
            // uint16 | maxSizeOfInstructions | Maximum byte count for glyph instructions.
            // uint16 | maxComponentElements  | Maximum number of components referenced at “top level” for any composite glyph.
            // uint16 | maxComponentDepth     | Maximum levels of recursion; 1 for simple components.
            ushort maxPoints = reader.ReadUInt16();
            ushort maxContours = reader.ReadUInt16();
            ushort maxCompositePoints = reader.ReadUInt16();
            ushort maxCompositeContours = reader.ReadUInt16();

            ushort maxZones = reader.ReadUInt16();
            ushort maxTwilightPoints = reader.ReadUInt16();
            ushort maxStorage = reader.ReadUInt16();
            ushort maxFunctionDefs = reader.ReadUInt16();
            ushort maxInstructionDefs = reader.ReadUInt16();
            ushort maxStackElements = reader.ReadUInt16();
            ushort maxSizeOfInstructions = reader.ReadUInt16();
            ushort maxComponentElements = reader.ReadUInt16();
            ushort maxComponentDepth = reader.ReadUInt16();

            return new MaximumProfileTable(
                numGlyphs,
                maxPoints,
                maxContours,
                maxCompositePoints,
                maxCompositeContours,
                maxZones,
                maxTwilightPoints,
                maxStorage,
                maxFunctionDefs,
                maxInstructionDefs,
                maxStackElements,
                maxSizeOfInstructions,
                maxComponentElements,
                maxComponentDepth);
        }
    }
}