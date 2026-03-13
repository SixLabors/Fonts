// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General;

/// <summary>
/// Represents the maximum profile table, which establishes memory requirements for the font.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/maxp"/>
/// </summary>
internal sealed class MaximumProfileTable : Table
{
    /// <summary>
    /// The table name identifier.
    /// </summary>
    internal const string TableName = "maxp";

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumProfileTable"/> class
    /// for version 0.5 (CFF fonts specifying only the glyph count).
    /// </summary>
    /// <param name="numGlyphs">The number of glyphs in the font.</param>
    public MaximumProfileTable(ushort numGlyphs)
        => this.GlyphCount = numGlyphs;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumProfileTable"/> class
    /// for version 1.0 (TrueType fonts with all fields).
    /// </summary>
    /// <param name="numGlyphs">The number of glyphs in the font.</param>
    /// <param name="maxPoints">The maximum points in a non-composite glyph.</param>
    /// <param name="maxContours">The maximum contours in a non-composite glyph.</param>
    /// <param name="maxCompositePoints">The maximum points in a composite glyph.</param>
    /// <param name="maxCompositeContours">The maximum contours in a composite glyph.</param>
    /// <param name="maxZones">The maximum zones (1 if no twilight zone, 2 otherwise).</param>
    /// <param name="maxTwilightPoints">The maximum points used in the twilight zone (Z0).</param>
    /// <param name="maxStorage">The number of storage area locations.</param>
    /// <param name="maxFunctionDefs">The number of FDEFs.</param>
    /// <param name="maxInstructionDefs">The number of IDEFs.</param>
    /// <param name="maxStackElements">The maximum stack depth.</param>
    /// <param name="maxSizeOfInstructions">The maximum byte count for glyph instructions.</param>
    /// <param name="maxComponentElements">The maximum number of components at top level for any composite glyph.</param>
    /// <param name="maxComponentDepth">The maximum levels of recursion.</param>
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

    /// <summary>
    /// Gets the maximum points in a non-composite glyph.
    /// </summary>
    public ushort MaxPoints { get; }

    /// <summary>
    /// Gets the maximum contours in a non-composite glyph.
    /// </summary>
    public ushort MaxContours { get; }

    /// <summary>
    /// Gets the maximum points in a composite glyph.
    /// </summary>
    public ushort MaxCompositePoints { get; }

    /// <summary>
    /// Gets the maximum contours in a composite glyph.
    /// </summary>
    public ushort MaxCompositeContours { get; }

    /// <summary>
    /// Gets the maximum zones (1 if instructions do not use the twilight zone, otherwise 2).
    /// </summary>
    public ushort MaxZones { get; }

    /// <summary>
    /// Gets the maximum points used in the twilight zone (Z0).
    /// </summary>
    public ushort MaxTwilightPoints { get; }

    /// <summary>
    /// Gets the number of storage area locations.
    /// </summary>
    public ushort MaxStorage { get; }

    /// <summary>
    /// Gets the number of FDEFs (equals to the highest function number + 1).
    /// </summary>
    public ushort MaxFunctionDefs { get; }

    /// <summary>
    /// Gets the number of IDEFs.
    /// </summary>
    public ushort MaxInstructionDefs { get; }

    /// <summary>
    /// Gets the maximum stack depth.
    /// </summary>
    public ushort MaxStackElements { get; }

    /// <summary>
    /// Gets the maximum byte count for glyph instructions.
    /// </summary>
    public ushort MaxSizeOfInstructions { get; }

    /// <summary>
    /// Gets the maximum number of components referenced at top level for any composite glyph.
    /// </summary>
    public ushort MaxComponentElements { get; }

    /// <summary>
    /// Gets the maximum levels of recursion (1 for simple components).
    /// </summary>
    public ushort MaxComponentDepth { get; }

    /// <summary>
    /// Gets the number of glyphs in the font.
    /// </summary>
    public ushort GlyphCount { get; }

    /// <summary>
    /// Loads the <see cref="MaximumProfileTable"/> from the specified font reader.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <returns>The <see cref="MaximumProfileTable"/>.</returns>
    public static MaximumProfileTable Load(FontReader reader)
    {
        using (BigEndianBinaryReader r = reader.GetReaderAtTablePosition(TableName))
        {
            return Load(r);
        }
    }

    /// <summary>
    /// Loads the <see cref="MaximumProfileTable"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <returns>The <see cref="MaximumProfileTable"/>.</returns>
    public static MaximumProfileTable Load(BigEndianBinaryReader reader)
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
        // uint16 | maxComponentElements  | Maximum number of components referenced at "top level" for any composite glyph.
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
