// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements reading the CVT Variations table <c>cvar</c>.
/// The cvar table provides variation data for the Control Value Table (CVT)
/// used by TrueType hinting instructions. It uses the same Tuple Variation Store
/// format as gvar, but with a single dimension of deltas (CVT values rather than X/Y coordinates).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/cvar"/>
/// </summary>
internal class CVarTable : Table
{
    /// <summary>
    /// The table name identifier for the cvar table.
    /// </summary>
    internal const string TableName = "cvar";

    /// <summary>
    /// Initializes a new instance of the <see cref="CVarTable"/> class.
    /// </summary>
    /// <param name="tupleVariations">The array of tuple variations containing CVT deltas.</param>
    public CVarTable(CVarTupleVariation[] tupleVariations)
        => this.TupleVariations = tupleVariations;

    /// <summary>
    /// Gets the tuple variations containing CVT deltas.
    /// </summary>
    public CVarTupleVariation[] TupleVariations { get; }

    /// <summary>
    /// Loads the cvar table from the font reader.
    /// The axis count must be known from the fvar table before loading cvar.
    /// </summary>
    /// <param name="reader">The font reader.</param>
    /// <param name="axisCount">The number of variation axes from fvar.</param>
    /// <returns>The loaded cvar table, or null if not present.</returns>
    public static CVarTable? Load(FontReader reader, int axisCount)
    {
        if (!reader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader, axisCount);
        }
    }

    /// <summary>
    /// Loads the cvar table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the cvar table.</param>
    /// <param name="axisCount">The number of variation axes from fvar.</param>
    /// <returns>The <see cref="CVarTable"/>.</returns>
    public static CVarTable Load(BigEndianBinaryReader reader, int axisCount)
    {
        // cvar — CVT Variations Table
        // The cvar table uses the Tuple Variation Store format.
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        // | Type                     | Name                                      | Description                                                  |
        // +==========================+===========================================+==============================================================+
        // | uint16                   | majorVersion                              | Major version — set to 1.                                    |
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        // | uint16                   | minorVersion                              | Minor version — set to 0.                                    |
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        // | uint16                   | tupleVariationCount                       | Packed field: high 4 bits are flags,                         |
        // |                          |                                           | low 12 bits are the number of tuple variation tables.        |
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        // | Offset16                 | dataOffset                                | Offset from the start of the cvar table to the               |
        // |                          |                                           | serialized data.                                             |
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        // | TupleVariation           | tupleVariationHeaders[tupleVariationCount]| Array of tuple variation headers.                            |
        // +--------------------------+-------------------------------------------+--------------------------------------------------------------+
        ushort majorVersion = reader.ReadUInt16();
        ushort minorVersion = reader.ReadUInt16();

        if (majorVersion != 1)
        {
            throw new NotSupportedException("Only version 1 of cvar table is supported");
        }

        ushort tupleVariationCount = reader.ReadUInt16();
        bool hasSharedPointNumbers = (tupleVariationCount & GlyphVariationData.SharedPointNumbersMask) != 0;
        int tupleCount = tupleVariationCount & GlyphVariationData.CountMask;
        ushort dataOffset = reader.ReadOffset16();

        // Read all tuple variation headers.
        TupleVariation[] tupleVariations = new TupleVariation[tupleCount];
        for (int i = 0; i < tupleCount; i++)
        {
            tupleVariations[i] = TupleVariation.Load(reader, axisCount);
        }

        // Seek to the serialized data.
        reader.Seek(dataOffset, SeekOrigin.Begin);

        // If shared point numbers flag is set, decode them from the start of the serialized data.
        ushort[]? sharedPointNumbers = null;
        if (hasSharedPointNumbers)
        {
            sharedPointNumbers = GlyphVariationData.DecodePackedPoints(reader);
        }

        // Decode each tuple's serialized data.
        // Unlike gvar, cvar has only one set of deltas per tuple (CVT value adjustments).
        CVarTupleVariation[] cvarTuples = new CVarTupleVariation[tupleCount];
        for (int i = 0; i < tupleCount; i++)
        {
            TupleVariation header = tupleVariations[i];
            long tupleDataStart = reader.BaseStream.Position;

            // Determine which CVT indices this tuple applies to.
            ushort[]? pointNumbers;
            if (header.HasPrivatePointNumbers)
            {
                pointNumbers = GlyphVariationData.DecodePackedPoints(reader);
            }
            else
            {
                pointNumbers = sharedPointNumbers;
            }

            int nPoints = pointNumbers is { Length: > 0 } ? pointNumbers.Length : 0;

            short[]? deltas = null;
            if (nPoints > 0)
            {
                // cvar has only one set of deltas (not X/Y pairs like gvar).
                deltas = GlyphVariationData.DecodePackedDeltas(reader, nPoints);
            }
            else
            {
                // All CVT entries are referenced. Store raw bytes for deferred decoding.
                long bytesConsumed = reader.BaseStream.Position - tupleDataStart;
                int remaining = header.VariationDataSize - (int)bytesConsumed;
                if (remaining > 0)
                {
                    cvarTuples[i] = new CVarTupleVariation(header, pointNumbers, null, reader.ReadBytes(remaining));
                    continue;
                }
            }

            // Skip any remaining bytes for this tuple.
            long consumed = reader.BaseStream.Position - tupleDataStart;
            int skip = header.VariationDataSize - (int)consumed;
            if (skip > 0)
            {
                reader.BaseStream.Position += skip;
            }

            cvarTuples[i] = new CVarTupleVariation(header, pointNumbers, deltas, null);
        }

        return new CVarTable(cvarTuples);
    }
}

/// <summary>
/// Represents a single tuple variation for the cvar table with its CVT index references and deltas.
/// Unlike gvar's <see cref="TupleVariationHeader"/> which has X/Y delta pairs,
/// cvar tuples have a single set of deltas for CVT values.
/// </summary>
internal class CVarTupleVariation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CVarTupleVariation"/> class.
    /// </summary>
    /// <param name="tupleVariation">The tuple variation header containing peak coordinates and flags.</param>
    /// <param name="pointNumbers">The CVT indices this tuple applies to, or null/empty for all CVT entries.</param>
    /// <param name="deltas">The CVT deltas, or null if deferred.</param>
    /// <param name="rawDeltaData">The raw serialized delta data for deferred decoding, or null if already decoded.</param>
    public CVarTupleVariation(
        TupleVariation tupleVariation,
        ushort[]? pointNumbers,
        short[]? deltas,
        byte[]? rawDeltaData)
    {
        this.TupleVariation = tupleVariation;
        this.PointNumbers = pointNumbers;
        this.Deltas = deltas;
        this.RawDeltaData = rawDeltaData;
    }

    /// <summary>
    /// Gets the tuple variation header containing peak coordinates and flags.
    /// </summary>
    public TupleVariation TupleVariation { get; }

    /// <summary>
    /// Gets the CVT indices this tuple applies to.
    /// An empty array means all CVT entries are referenced.
    /// </summary>
    public ushort[]? PointNumbers { get; }

    /// <summary>
    /// Gets the CVT deltas for the referenced entries.
    /// Null when deltas apply to all CVT entries and were deferred (see <see cref="RawDeltaData"/>).
    /// </summary>
    public short[]? Deltas { get; }

    /// <summary>
    /// Gets the raw serialized delta data for deferred decoding.
    /// Used when point numbers indicate "all CVT entries" and the actual count
    /// is not known until the CVT table size is available.
    /// </summary>
    public byte[]? RawDeltaData { get; }
}
