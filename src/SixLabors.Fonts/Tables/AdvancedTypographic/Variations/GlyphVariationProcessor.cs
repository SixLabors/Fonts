// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// <para>
/// This class is transforms TrueType glyphs according to the data from
/// the Apple Advanced Typography variation tables(fvar, gvar, and avar).
/// These tables allow infinite adjustments to glyph weight, width, slant,
/// and optical size without the designer needing to specify every exact style.
/// </para>
/// <para>Implementation is based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/glyph/GlyphVariationProcessor.js"/></para>
/// <para>Docs for the item variations: <see href="https://learn.microsoft.com/en-us/typography/opentype/otspec191alpha/otvarcommonformats_delta#item-variation-store"/></para>
/// </summary>
internal class GlyphVariationProcessor
{
    private readonly ItemVariationStore itemStore;

    private readonly FVarTable fvar;

    private readonly AVarTable? avar;

    private readonly GVarTable? gVar;

    private readonly HVarTable? hVar;

    private readonly float[] normalizedCoords;

    private readonly Dictionary<ItemVariationData, float[]> blendVectors;

    /// <summary>
    /// Epsilon as used in fontkit reference implementation.
    /// </summary>
    private const float Epsilon = 2.2204460492503130808472633361816E-16F;

    public GlyphVariationProcessor(ItemVariationStore itemStore, FVarTable fVar, AVarTable? aVar = null, GVarTable? gVar = null, HVarTable? hVar = null)
    {
        DebugGuard.NotNull(itemStore, nameof(itemStore));
        DebugGuard.NotNull(fVar, nameof(fVar));

        this.itemStore = itemStore;
        this.fvar = fVar;
        this.avar = aVar;
        this.gVar = gVar;
        this.hVar = hVar;
        this.normalizedCoords = this.NormalizeDefaultCoords();
        this.blendVectors = new Dictionary<ItemVariationData, float[]>();
    }

    private float[] NormalizeDefaultCoords()
    {
        float[] coords = new float[this.fvar.AxisCount];
        for (int i = 0; i < this.fvar.AxisCount; i++)
        {
            coords[i] = this.fvar.Axes[i].DefaultValue;
        }

        // The default mapping is linear along each axis, in two segments:
        // from the minValue to defaultValue, and from defaultValue to maxValue.
        float[] normalized = new float[this.fvar.AxisCount];
        for (int i = 0; i < this.fvar.AxisCount; i++)
        {
            VariationAxisRecord axis = this.fvar.Axes[i];
            if (coords[i] < axis.DefaultValue)
            {
                normalized[i] = (coords[i] - axis.DefaultValue + Epsilon) / (axis.DefaultValue - axis.MinValue + Epsilon);
            }
            else
            {
                normalized[i] = (coords[i] - axis.DefaultValue + Epsilon) / (axis.MaxValue - axis.DefaultValue + Epsilon);
            }
        }

        // If there is an avar table, the normalized value is calculated
        // by interpolating between the two nearest mapped values.
        if (this.avar is not null)
        {
            for (int i = 0; i < this.avar.SegmentMaps.Length; i++)
            {
                SegmentMapRecord segment = this.avar.SegmentMaps[i];
                for (int j = 0; j < segment.AxisValueMap.Length; j++)
                {
                    AxisValueMapRecord pair = segment.AxisValueMap[j];
                    if (j >= 1 && normalized[i] < pair.FromCoordinate)
                    {
                        AxisValueMapRecord prev = segment.AxisValueMap[j - 1];
                        normalized[i] = ((((normalized[i] - prev.FromCoordinate) * (pair.ToCoordinate - prev.ToCoordinate)) + Epsilon) /
                                        (pair.FromCoordinate - prev.FromCoordinate + Epsilon)) + prev.ToCoordinate;
                        break;
                    }
                }
            }
        }

        return normalized;
    }

    public int AdvanceAdjustment(int glyphId)
    {
        if (this.hVar is null)
        {
            throw new InvalidFontFileException("Missing HVAR table");
        }

        int outerIndex;
        int innerIndex;
        if (this.hVar?.AdvanceWidthMapping != null && this.hVar?.AdvanceWidthMapping.Length > 0)
        {
            DeltaSetIndexMap[]? advanceWidthMapping = this.hVar?.AdvanceWidthMapping;
            int idx = glyphId;
            if (idx >= advanceWidthMapping?.Length)
            {
                idx = advanceWidthMapping.Length - 1;
            }

            outerIndex = advanceWidthMapping![idx].OuterIndex;
            innerIndex = advanceWidthMapping[idx].InnerIndex;
        }
        else
        {
            outerIndex = 0;
            innerIndex = glyphId;
        }

        return this.Delta(outerIndex, innerIndex);
    }

    public float[] BlendVector(int outerIndex)
    {
        ItemVariationData variationData = this.itemStore.ItemVariations[outerIndex];
        if (this.blendVectors.ContainsKey(variationData))
        {
            return this.blendVectors[variationData];
        }

        float[] blendVector = new float[variationData.RegionIndexes.Length];

        // Outer loop steps through master designs to be blended.
        for (int i = 0; i < variationData.RegionIndexes.Length; i++)
        {
            float scalar = 1.0f;
            ushort regionIndex = variationData.RegionIndexes[i];
            RegionAxisCoordinates[] axes = this.itemStore.VariationRegionList.VariationRegions[regionIndex];

            // Inner loop steps through axes in this region.
            for (int j = 0; j < axes.Length; j++)
            {
                RegionAxisCoordinates axis = axes[j];

                // Compute the scalar contribution of this axis, ignore invalid ranges.
                float axisScalar;
                if (axis.StartCoord > axis.PeakCoord || axis.PeakCoord > axis.EndCoord)
                {
                    axisScalar = 1;
                }
                else if (axis.StartCoord < 0 && axis.EndCoord > 0 && axis.PeakCoord != 0)
                {
                    axisScalar = 1;
                }
                else if (axis.PeakCoord == 0)
                {
                    // Peak of 0 means ignore this axis.
                    axisScalar = 1;
                }
                else if (this.normalizedCoords[j] < axis.StartCoord || this.normalizedCoords[j] > axis.EndCoord)
                {
                    // Ignore this region if coords are out of range
                    axisScalar = 0;
                }
                else
                {
                    // Calculate a proportional factor.
                    if (this.normalizedCoords[j] == axis.PeakCoord)
                    {
                        axisScalar = 1;
                    }
                    else if (this.normalizedCoords[j] < axis.PeakCoord)
                    {
                        axisScalar = (this.normalizedCoords[j] - axis.StartCoord + Epsilon) /
                                     (axis.PeakCoord - axis.StartCoord + Epsilon);
                    }
                    else
                    {
                        axisScalar = (axis.EndCoord - this.normalizedCoords[j] + Epsilon) /
                                     (axis.EndCoord - axis.PeakCoord + Epsilon);
                    }
                }

                // Take product of all the axis scalars.
                scalar *= axisScalar;
            }

            blendVector[i] = scalar;
        }

        this.blendVectors[variationData] = blendVector;

        return blendVector;
    }

    private int Delta(int outerIndex, int innerIndex)
    {
        if (outerIndex >= this.itemStore.ItemVariations.Length)
        {
            return 0;
        }

        ItemVariationData variationData = this.itemStore.ItemVariations[outerIndex];
        if (innerIndex >= variationData.DeltaSets.Length)
        {
            return 0;
        }

        DeltaSet deltaSet = variationData.DeltaSets[innerIndex];
        float[] blendVector = this.BlendVector(outerIndex);
        int netAdjustment = 0;
        for (int master = 0; master < variationData.RegionIndexes.Length; master++)
        {
            // TODO: disabled, no deltaSet does not have Deltas field.
            // netAdjustment += deltaSet.Deltas[master] * blendVector[master];
        }

        return netAdjustment;
    }
}
