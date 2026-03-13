// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Represents a tuple variation header from the gvar or cvar table, containing
/// the variation data size, tuple index flags, and optional peak/intermediate coordinates.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/otvarcommonformats"/>
/// </summary>
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
    /// Flag indicating that the serialized data for this tuple variation table includes packed "point" number data.
    /// If set, this tuple variation table uses that number data; if clear, this tuple variation table uses shared number
    /// data found at the start of the serialized data for this glyph variation data or 'cvar' table.
    /// </summary>
    internal const int PrivatePointNumbersMask = 0x2000;

    /// <summary>
    /// Mask for the low 12 bits to give the shared tuple records index.
    /// </summary>
    internal const int TupleIndexMask = 0x0FFF;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleVariation"/> class.
    /// </summary>
    /// <param name="axisCount">The number of variation axes.</param>
    /// <param name="variationDataSize">The size in bytes of the serialized data for this tuple.</param>
    /// <param name="tupleIndex">The packed tuple index containing flags and shared tuple index.</param>
    /// <param name="embeddedPeak">The optional embedded peak tuple coordinates, or null if using shared tuples.</param>
    /// <param name="intermediateStartRegion">The optional intermediate region start coordinates.</param>
    /// <param name="intermediateEndRegion">The optional intermediate region end coordinates.</param>
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

    /// <summary>
    /// Gets the number of variation axes.
    /// </summary>
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
    /// Gets the shared tuple records index (low 12 bits of <see cref="TupleIndex"/>).
    /// Used to look up peak coordinates from <see cref="GVarTable.SharedTuples" /> when no embedded peak is present.
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

    /// <summary>
    /// Gets the embedded peak tuple coordinates, or null if the peak is referenced from shared tuples.
    /// </summary>
    public float[]? EmbeddedPeak { get; }

    /// <summary>
    /// Gets the intermediate region start coordinates, or null if this is not an intermediate tuple.
    /// </summary>
    public float[]? IntermediateStartRegion { get; }

    /// <summary>
    /// Gets the intermediate region end coordinates, or null if this is not an intermediate tuple.
    /// </summary>
    public float[]? IntermediateEndRegion { get; }

    /// <summary>
    /// Loads a <see cref="TupleVariation"/> from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="axisCount">The number of variation axes.</param>
    /// <returns>The <see cref="TupleVariation"/>.</returns>
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
