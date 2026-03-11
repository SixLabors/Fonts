// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Implements loading glyph variation data structure.
/// </summary>
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#tuple-variation-store-header"/>
internal class GlyphVariationData
{
    /// <summary>
    /// Mask for the low bits to give the number of tuple variation tables.
    /// </summary>
    internal const int CountMask = 0x0FFF;

    /// <summary>
    /// Flag indicating that some or all tuple variation tables reference a shared set of "point" numbers.
    /// These shared numbers are represented as packed point number data at the start of the serialized data.
    /// </summary>
    internal const int SharedPointNumbersMask = 0x8000;

    /// <summary>
    /// Flag indicating that packed deltas are zero and omitted. Lower 6 bits give run count - 1.
    /// </summary>
    private const int DeltasAreZero = 0x80;

    /// <summary>
    /// Flag indicating that packed deltas are 16-bit (int16). Lower 6 bits give run count - 1.
    /// If neither <see cref="DeltasAreZero"/> nor <see cref="DeltasAreWords"/> is set, deltas are 8-bit (int8).
    /// </summary>
    private const int DeltasAreWords = 0x40;

    /// <summary>
    /// Mask for the lower 6 bits of a delta run header, giving run count - 1.
    /// </summary>
    private const int DeltaRunCountMask = 0x3F;

    /// <summary>
    /// Flag in the first byte of packed point numbers indicating that point numbers are 16-bit.
    /// </summary>
    private const int PointsAreWords = 0x80;

    /// <summary>
    /// Mask for the lower 7 bits of a point run header, giving run count - 1.
    /// </summary>
    private const int PointRunCountMask = 0x7F;

    public GlyphVariationData(TupleVariationHeader[] tupleHeaders)
        => this.TupleHeaders = tupleHeaders;

    /// <summary>
    /// Gets the tuple variation headers with their decoded point indices and deltas.
    /// </summary>
    public TupleVariationHeader[] TupleHeaders { get; }

    /// <summary>
    /// Gets a value indicating whether this glyph has any variation data.
    /// </summary>
    public bool HasData => this.TupleHeaders.Length > 0;

    public static GlyphVariationData Load(BigEndianBinaryReader reader, long offset, int axisCount)
    {
        // GlyphVariationData
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Type                 | Name                                      | Description                                                                  |
        // +======================+===========================================+==============================================================================+
        // | uint16               | tupleVariationCount                       | A packed field. The high 4 bits are flags,                                   |
        // |                      |                                           | and the low 12 bits are the number of tuple variation tables for this glyph. |
        // |                      |                                           | The count can be any number between 1 and 4095.                              |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Offset16             | dataOffset                                | Offset from the start of the GlyphVariationData table to the serialized data.|
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | TupleVariation       | tupleVariationHeaders[tupleVariationCount]| Array of tuple variation headers.                                            |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // NOTE: 'offset' is relative to the start of the gvar table.
        reader.Seek(offset, SeekOrigin.Begin);
        ushort tupleVariationCount = reader.ReadUInt16();
        bool hasSharedPointNumbers = (tupleVariationCount & SharedPointNumbersMask) != 0;
        int tupleCount = tupleVariationCount & CountMask;

        // Spec: dataOffset is Offset16 (always 16-bit), independent of the gvar offset array format.
        // This offset is relative to the start of this GlyphVariationData table.
        ushort serializedDataOffset = reader.ReadOffset16();

        // Read all tuple variation headers first (they come before the serialized data).
        TupleVariation[] tupleVariations = new TupleVariation[tupleCount];
        for (int i = 0; i < tupleCount; i++)
        {
            tupleVariations[i] = TupleVariation.Load(reader, axisCount);
        }

        // Now read the serialized data that follows the headers.
        long serializedDataPos = offset + serializedDataOffset;
        reader.Seek(serializedDataPos, SeekOrigin.Begin);

        // If shared point numbers flag is set, decode them from the start of the serialized data.
        ushort[]? sharedPointNumbers = null;
        if (hasSharedPointNumbers)
        {
            sharedPointNumbers = DecodePackedPoints(reader);
        }

        // Decode each tuple's serialized data (point numbers and deltas).
        TupleVariationHeader[] tupleHeaders = new TupleVariationHeader[tupleCount];
        for (int i = 0; i < tupleCount; i++)
        {
            TupleVariation header = tupleVariations[i];
            long tupleDataStart = reader.BaseStream.Position;

            // Determine which point numbers this tuple uses.
            ushort[]? pointNumbers;
            if (header.HasPrivatePointNumbers)
            {
                pointNumbers = DecodePackedPoints(reader);
            }
            else
            {
                pointNumbers = sharedPointNumbers;
            }

            // The number of deltas to decode depends on whether specific points are referenced.
            // If pointNumbers is empty (length 0), deltas apply to all points and the count
            // is determined by the caller (TransformPoints). We use VariationDataSize to bound reading.
            int nPoints = pointNumbers is { Length: > 0 } ? pointNumbers.Length : 0;

            short[]? deltasX = null;
            short[]? deltasY = null;
            if (nPoints > 0)
            {
                deltasX = DecodePackedDeltas(reader, nPoints);
                deltasY = DecodePackedDeltas(reader, nPoints);
            }
            else
            {
                // When no explicit points are specified, we need to read all remaining data
                // for this tuple. The deltas apply to all glyph points + 4 phantom points.
                // We cannot know the point count here, so we store the raw bytes and decode later.
                // However, the simpler approach used by fontkit is to decode based on the remaining
                // bytes in this tuple's data block. We'll defer full decoding to TransformPoints
                // by storing the raw data range.
                long bytesConsumed = reader.BaseStream.Position - tupleDataStart;
                int remaining = header.VariationDataSize - (int)bytesConsumed;
                if (remaining > 0)
                {
                    // Store raw bytes for deferred decoding when we know the point count.
                    tupleHeaders[i] = new TupleVariationHeader(header, pointNumbers, null, null, reader.ReadBytes(remaining));
                    continue;
                }
            }

            // Skip any remaining bytes for this tuple that we haven't consumed.
            long consumed = reader.BaseStream.Position - tupleDataStart;
            int skip = header.VariationDataSize - (int)consumed;
            if (skip > 0)
            {
                reader.BaseStream.Position += skip;
            }

            tupleHeaders[i] = new TupleVariationHeader(header, pointNumbers, deltasX, deltasY, null);
        }

        return new GlyphVariationData(tupleHeaders);
    }

    /// <summary>
    /// Decodes packed point numbers from the serialized data.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the packed point data.</param>
    /// <returns>
    /// An array of absolute point indices, or an empty array if all points are referenced.
    /// </returns>
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#packed-point-numbers"/>
    internal static ushort[] DecodePackedPoints(BigEndianBinaryReader reader)
    {
        // First byte determines the count of points.
        byte firstByte = reader.ReadByte();
        int count;
        if ((firstByte & PointsAreWords) != 0)
        {
            // High bit set: count is ((firstByte & 0x7F) << 8) | nextByte.
            count = ((firstByte & PointRunCountMask) << 8) | reader.ReadByte();
        }
        else
        {
            count = firstByte;
        }

        // A count of 0 means "all points" — return empty array as sentinel.
        if (count == 0)
        {
            return [];
        }

        // Read run-length encoded point number deltas.
        ushort[] points = new ushort[count];
        int i = 0;
        while (i < count)
        {
            byte runHeader = reader.ReadByte();
            bool runPointsAreWords = (runHeader & PointsAreWords) != 0;
            int runCount = (runHeader & PointRunCountMask) + 1;

            ushort accumulator = i > 0 ? points[i - 1] : (ushort)0;
            for (int j = 0; j < runCount && i < count; j++, i++)
            {
                ushort delta = runPointsAreWords ? reader.ReadUInt16() : reader.ReadByte();
                accumulator += delta;
                points[i] = accumulator;
            }
        }

        return points;
    }

    /// <summary>
    /// Decodes packed delta values from the serialized data.
    /// </summary>
    /// <param name="reader">The binary reader positioned at the packed delta data.</param>
    /// <param name="count">The number of delta values to decode.</param>
    /// <returns>An array of decoded delta values.</returns>
    /// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats#packed-deltas"/>
    internal static short[] DecodePackedDeltas(BigEndianBinaryReader reader, int count)
    {
        short[] deltas = new short[count];
        int i = 0;
        while (i < count)
        {
            byte runHeader = reader.ReadByte();
            bool areZero = (runHeader & DeltasAreZero) != 0;
            bool areWords = (runHeader & DeltasAreWords) != 0;
            int runCount = (runHeader & DeltaRunCountMask) + 1;

            for (int j = 0; j < runCount && i < count; j++, i++)
            {
                if (areZero)
                {
                    deltas[i] = 0;
                }
                else if (areWords)
                {
                    deltas[i] = reader.ReadInt16();
                }
                else
                {
                    deltas[i] = (short)(sbyte)reader.ReadByte();
                }
            }
        }

        return deltas;
    }
}

/// <summary>
/// Represents a fully decoded tuple variation header with its associated point indices and delta values.
/// </summary>
internal class TupleVariationHeader
{
    public TupleVariationHeader(
        TupleVariation tupleVariation,
        ushort[]? pointNumbers,
        short[]? deltasX,
        short[]? deltasY,
        byte[]? rawDeltaData)
    {
        this.TupleVariation = tupleVariation;
        this.PointNumbers = pointNumbers;
        this.DeltasX = deltasX;
        this.DeltasY = deltasY;
        this.RawDeltaData = rawDeltaData;
    }

    /// <summary>
    /// Gets the tuple variation header containing peak coordinates and flags.
    /// </summary>
    public TupleVariation TupleVariation { get; }

    /// <summary>
    /// Gets the point indices this tuple applies to.
    /// An empty array means all points are referenced.
    /// Null means no point data was available.
    /// </summary>
    public ushort[]? PointNumbers { get; }

    /// <summary>
    /// Gets the X coordinate deltas for the referenced points.
    /// Null when deltas apply to all points and were deferred (see <see cref="RawDeltaData"/>).
    /// </summary>
    public short[]? DeltasX { get; }

    /// <summary>
    /// Gets the Y coordinate deltas for the referenced points.
    /// Null when deltas apply to all points and were deferred (see <see cref="RawDeltaData"/>).
    /// </summary>
    public short[]? DeltasY { get; }

    /// <summary>
    /// Gets the raw serialized delta data for deferred decoding.
    /// This is used when point numbers indicate "all points" and the actual point count
    /// is not known until <see cref="GlyphVariationProcessor.TransformPoints"/> is called.
    /// </summary>
    public byte[]? RawDeltaData { get; }
}
