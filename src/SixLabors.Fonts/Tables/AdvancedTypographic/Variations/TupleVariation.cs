// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

internal class TupleVariation
{
    /// <summary>
    /// Flag indicating that this tuple variation header includes an embedded peak tuple record, immediately after the tupleIndex field.
    /// If set, the low 12 bits of the tupleIndex value are ignored.
    /// Note that this must always be set within the 'cvar' table.
    /// </summary>
    internal const int EmbeddedPeakTupleMask = 0x8000;

    /// <summary>
    /// Flag indicating that this tuple variation table applies to an intermediate region within the variation space.
    /// If set, the header includes the two intermediate-region, start and end tuple records, immediately after the peak tuple record (if present).
    /// </summary>
    internal const int IntermediateRegionMask = 0x4000;

    /// <summary>
    /// Flag indicating that the serialized data for this tuple variation table includes packed “point” number data.
    /// If set, this tuple variation table uses that number data; if clear, this tuple variation table uses shared number
    /// data found at the start of the serialized data for this glyph variation data or 'cvar' table.
    /// </summary>
    internal const int PrivatePointNumbersMask = 0x2000;

    /// <summary>
    /// Mask for the low 12 bits to give the shared tuple records index.
    /// </summary>
    internal const int TupleIndexMask = 0x0FFF;

    public TupleVariation(int axisCount, float[]? embeddedPeak, float[]? intermediateStartRegion, float[]? intermediateEndRegion)
    {
        this.AxisCount = axisCount;
        this.EmbeddedPeak = embeddedPeak;
        this.IntermediateStartRegion = intermediateStartRegion;
        this.IntermediateEndRegion = intermediateEndRegion;
    }

    public int AxisCount { get; }

    public float[]? EmbeddedPeak { get; }

    public float[]? IntermediateStartRegion { get; }

    public float[]? IntermediateEndRegion { get; }

    public static TupleVariation Load(BigEndianBinaryReader reader, int axisCount)
    {
        // TupleVariation
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Type                 | Name                                      | Description                                                                  |
        // +======================+===========================================+==============================================================================+
        // | uint16               | variationDataSize                         | The size in bytes of the serialized data for this tuple variation table.     |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | uint16               | tupleIndex                                | A packed field. The high 4 bits are flags.                                   |
        // |                      |                                           | The low 12 bits are an index into a shared tuple records array.              |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Tuple                | peakTuple                                 | Peak tuple record for this tuple variation table —                           |
        // |                      |                                           | optional, determined by flags in the tupleIndex value.                       |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Tuple                | intermediateStartTuple                    | Intermediate start tuple record for this tuple variation table —             |
        // |                      |                                           | optional, determined by flags in the tupleIndex value.                       |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        // | Tuple                | intermediateEndTuple                      | Intermediate end tuple record for this tuple variation table —               |
        // |                      |                                           | optional, determined by flags in the tupleIndex value.                       |
        // +----------------------+-------------------------------------------+------------------------------------------------------------------------------+
        ushort variationDataSize = reader.ReadUInt16();
        int bytesRead = 0;
        ushort tupleIndex = reader.ReadUInt16();
        bytesRead += 2;

        int sharedTupleRecords = tupleIndex & TupleIndexMask;
        bool hasPrivatePointNumbers = (tupleIndex & PrivatePointNumbersMask) == PrivatePointNumbersMask;
        bool hasEmbeddedPeakTuple = (tupleIndex & EmbeddedPeakTupleMask) == EmbeddedPeakTupleMask;
        bool hasIntermediateRegion = (tupleIndex & IntermediateRegionMask) == IntermediateRegionMask;

        float[]? embeddedPeak = null;
        if (hasEmbeddedPeakTuple)
        {
            embeddedPeak = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                embeddedPeak[i] = reader.ReadF2Dot14();
                bytesRead += 2;
            }
        }

        float[]? intermediateStartRegion = null;
        float[]? intermediateEndRegion = null;
        if (hasIntermediateRegion)
        {
            intermediateStartRegion = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                intermediateStartRegion[i] = reader.ReadF2Dot14();
                bytesRead += 2;
            }

            intermediateEndRegion = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                intermediateEndRegion[i] = reader.ReadF2Dot14();
                bytesRead += 2;
            }
        }

        return new TupleVariation(axisCount, embeddedPeak, intermediateStartRegion, intermediateEndRegion);
    }
}
