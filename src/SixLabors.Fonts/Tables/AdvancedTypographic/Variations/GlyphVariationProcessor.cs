// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations
{
    /// <summary>
    /// This class is transforms TrueType glyphs according to the data from
    /// the Apple Advanced Typography variation tables(fvar, gvar, and avar).
    /// These tables allow infinite adjustments to glyph weight, width, slant,
    /// and optical size without the designer needing to specify every exact style.
    ///
    /// Implementation is based on fontkit: https://github.com/foliojs/fontkit/blob/master/src/glyph/GlyphVariationProcessor.js
    /// </summary>
    internal class GlyphVariationProcessor
    {
        private readonly ItemVariationStore itemStore;

        private readonly FVarTable fvar;

        private readonly AVarTable? avar;

        private readonly float[] normalizedCoords;

        public GlyphVariationProcessor(ItemVariationStore itemStore, FVarTable fVar, AVarTable? aVar = null)
        {
            DebugGuard.NotNull(itemStore, nameof(itemStore));
            DebugGuard.NotNull(fVar, nameof(fVar));

            this.itemStore = itemStore;
            this.fvar = fVar;
            this.avar = aVar;
            this.normalizedCoords = this.NormalizeDefaultCoords();
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
                    normalized[i] = (coords[i] - axis.DefaultValue + float.Epsilon) / (axis.DefaultValue - axis.MinValue + float.Epsilon);
                }
                else
                {
                    normalized[i] = (coords[i] - axis.DefaultValue + float.Epsilon) / (axis.MaxValue - axis.DefaultValue + float.Epsilon);
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
                            normalized[i] = ((((normalized[i] - prev.FromCoordinate) * (pair.ToCoordinate - prev.ToCoordinate)) + float.Epsilon) /
                                            (pair.FromCoordinate - prev.FromCoordinate + float.Epsilon)) + prev.ToCoordinate;
                        }
                    }
                }
            }

            return normalized;
        }

        public float[] BlendVector(int outerIndex)
        {
            ItemVariationData variationData = this.itemStore.ItemVariations[outerIndex];

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
                            axisScalar = (this.normalizedCoords[j] - axis.StartCoord + float.Epsilon) /
                                         (axis.PeakCoord - axis.StartCoord + float.Epsilon);
                        }
                        else
                        {
                            axisScalar = (axis.EndCoord - this.normalizedCoords[j] + float.Epsilon) /
                                         (axis.EndCoord - axis.PeakCoord + float.Epsilon);
                        }
                    }

                    // Take product of all the axis scalars.
                    scalar *= axisScalar;
                }

                blendVector[i] = scalar;
            }

            return blendVector;
        }
    }
}
