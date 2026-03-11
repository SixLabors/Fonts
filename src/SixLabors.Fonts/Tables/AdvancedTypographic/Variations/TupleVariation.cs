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

    public TupleVariation(
        int axisCount,
        ushort variationDataSize,
        ushort tupleIndex,
        float[]? embeddedPeak,
        float[]? intermediateStartRegion,
        float[]? intermediateEndRegion)
    {
        this.AxisCount = axisCount;
        this.VariationDataSize = variationDataSize;
        this.TupleIndex = tupleIndex;
        this.EmbeddedPeak = embeddedPeak;
        this.IntermediateStartRegion = intermediateStartRegion;
        this.IntermediateEndRegion = intermediateEndRegion;
    }

    public int AxisCount { get; }

    /// <summary>
    /// Gets the size in bytes of the serialized data for this tuple variation table.
    /// </summary>
    public ushort VariationDataSize { get; }

    /// <summary>
    /// Gets the packed tuple index field containing flags (high 4 bits) and shared tuple records index (low 12 bits).
    /// </summary>
    public ushort TupleIndex { get; }

    /// <summary>
    /// Gets the shared tuple records index (low 12 bits of <see cref=”TupleIndex”/>).
    /// Used to look up peak coordinates from <see cref=”GVarTable.SharedTuples”/> when no embedded peak is present.
    /// </summary>
    public int SharedTupleIndex => this.TupleIndex & TupleIndexMask;

    /// <summary>
    /// Gets a value indicating whether this tuple has private point numbers in its serialized data.
    /// </summary>
    public bool HasPrivatePointNumbers => (this.TupleIndex & PrivatePointNumbersMask) != 0;

    /// <summary>
    /// Gets a value indicating whether this tuple has an intermediate region (start/end coordinates).
    /// </summary>
    public bool IsIntermediateRegion => (this.TupleIndex & IntermediateRegionMask) != 0;

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
        ushort tupleIndex = reader.ReadUInt16();

        bool hasEmbeddedPeakTuple = (tupleIndex & EmbeddedPeakTupleMask) != 0;
        bool hasIntermediateRegion = (tupleIndex & IntermediateRegionMask) != 0;

        float[]? embeddedPeak = null;
        if (hasEmbeddedPeakTuple)
        {
            embeddedPeak = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                embeddedPeak[i] = reader.ReadF2Dot14();
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
            }

            intermediateEndRegion = new float[axisCount];
            for (int i = 0; i < axisCount; i++)
            {
                intermediateEndRegion[i] = reader.ReadF2Dot14();
            }
        }

        return new TupleVariation(axisCount, variationDataSize, tupleIndex, embeddedPeak, intermediateStartRegion, intermediateEndRegion);
    }
}
