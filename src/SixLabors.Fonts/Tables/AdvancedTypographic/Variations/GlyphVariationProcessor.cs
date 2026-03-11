// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// <para>
/// This class transforms TrueType glyphs according to the data from
/// the OpenType variation tables (fvar, gvar, avar, HVAR, VVAR).
/// These tables allow infinite adjustments to glyph weight, width, slant,
/// and optical size without the designer needing to specify every exact style.
/// </para>
/// <para>Implementation is based on fontkit: <see href="https://github.com/foliojs/fontkit/blob/master/src/glyph/GlyphVariationProcessor.js"/></para>
/// <para>Docs for the item variations: <see href="https://learn.microsoft.com/en-us/typography/opentype/otspec191alpha/otvarcommonformats_delta#item-variation-store"/></para>
/// </summary>
internal class GlyphVariationProcessor
{
    private readonly ItemVariationStore? itemStore;

    private readonly FVarTable fvar;

    private readonly AVarTable? avar;

    private readonly GVarTable? gVar;

    private readonly HVarTable? hVar;

    private readonly VVarTable? vVar;

    private readonly float[] normalizedCoords;

    private readonly Dictionary<ItemVariationData, float[]> blendVectors;

    public GlyphVariationProcessor(
        ItemVariationStore? itemStore,
        FVarTable fVar,
        AVarTable? aVar = null,
        GVarTable? gVar = null,
        HVarTable? hVar = null,
        VVarTable? vVar = null,
        float[]? userCoordinates = null)
    {
        DebugGuard.NotNull(fVar, nameof(fVar));

        this.itemStore = itemStore;
        this.fvar = fVar;
        this.avar = aVar;
        this.gVar = gVar;
        this.hVar = hVar;
        this.vVar = vVar;
        this.normalizedCoords = this.NormalizeCoords(userCoordinates);
        this.blendVectors = [];
    }

    /// <summary>
    /// Transforms glyph outline points by applying gvar variation deltas.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="glyphPoints">The glyph vector whose control points will be modified in-place.</param>
    public void TransformPoints(ushort glyphId, ref GlyphVector glyphPoints)
    {
        if (this.gVar is null)
        {
            return;
        }

        if (glyphId >= this.gVar.GlyphCount)
        {
            return;
        }

        GlyphVariationData variationData = this.gVar.GlyphVariations[glyphId];
        if (!variationData.HasData)
        {
            return;
        }

        IList<ControlPoint> controlPoints = glyphPoints.ControlPoints;
        int pointCount = controlPoints.Count;

        // Clone the original points for IUP reference (interpolation needs unmodified originals).
        GlyphVector originPoints = GlyphVector.DeepClone(glyphPoints);
        IList<ControlPoint> origPoints = originPoints.ControlPoints;

        foreach (TupleVariationHeader tupleHeader in variationData.TupleHeaders)
        {
            TupleVariation tuple = tupleHeader.TupleVariation;

            // Resolve peak coordinates: either embedded or from shared tuples.
            float[]? peakCoords = tuple.EmbeddedPeak;
            if (peakCoords is null)
            {
                int sharedIdx = tuple.SharedTupleIndex;
                if (sharedIdx >= this.gVar.SharedTuples.GetLength(0))
                {
                    continue;
                }

                peakCoords = new float[this.gVar.AxisCount];
                for (int a = 0; a < this.gVar.AxisCount; a++)
                {
                    peakCoords[a] = this.gVar.SharedTuples[sharedIdx, a];
                }
            }

            // Calculate the blending factor for this tuple.
            float factor = this.TupleFactor(
                tuple.IsIntermediateRegion,
                peakCoords,
                tuple.IntermediateStartRegion,
                tuple.IntermediateEndRegion);

            if (factor == 0)
            {
                continue;
            }

            // Resolve point numbers and deltas.
            ushort[]? pointNumbers = tupleHeader.PointNumbers;
            short[]? deltasX = tupleHeader.DeltasX;
            short[]? deltasY = tupleHeader.DeltasY;

            // If deltas were deferred (all-points case), decode them now that we know the point count.
            if (deltasX is null && tupleHeader.RawDeltaData is not null)
            {
                DecodeAllPointDeltas(tupleHeader.RawDeltaData, pointCount, out deltasX, out deltasY);
            }

            if (deltasX is null || deltasY is null)
            {
                continue;
            }

            bool allPoints = pointNumbers is null or { Length: 0 };

            if (allPoints)
            {
                // Deltas apply to all points directly.
                int deltaCount = Math.Min(deltasX.Length, pointCount);
                for (int i = 0; i < deltaCount; i++)
                {
                    ControlPoint cp = controlPoints[i];
                    cp.Point.X += MathF.Round(deltasX[i] * factor);
                    cp.Point.Y += MathF.Round(deltasY[i] * factor);
                    controlPoints[i] = cp;
                }
            }
            else
            {
                // Deltas apply to specific points only; interpolate the rest.
                // Use Buffer<T> to avoid per-tuple heap allocations.
                using Buffer<float> adjustXBuf = new(pointCount);
                using Buffer<float> adjustYBuf = new(pointCount);
                using Buffer<byte> hasDeltaBuf = new(pointCount);
                Span<float> adjustX = adjustXBuf.GetSpan();
                Span<float> adjustY = adjustYBuf.GetSpan();
                Span<byte> hasDelta = hasDeltaBuf.GetSpan();
                adjustX.Clear();
                adjustY.Clear();
                hasDelta.Clear();

                for (int i = 0; i < pointNumbers!.Length && i < deltasX.Length; i++)
                {
                    int ptIdx = pointNumbers[i];
                    if (ptIdx < pointCount)
                    {
                        hasDelta[ptIdx] = 1;
                        adjustX[ptIdx] = deltasX[i] * factor;
                        adjustY[ptIdx] = deltasY[i] * factor;
                    }
                }

                // Interpolate unreferenced points.
                InterpolateMissingDeltas(
                    controlPoints,
                    origPoints,
                    glyphPoints.EndPoints,
                    adjustX,
                    adjustY,
                    hasDelta);

                // Apply the accumulated deltas.
                for (int i = 0; i < pointCount; i++)
                {
                    ControlPoint cp = controlPoints[i];
                    cp.Point.X += MathF.Round(adjustX[i]);
                    cp.Point.Y += MathF.Round(adjustY[i]);
                    controlPoints[i] = cp;
                }
            }
        }

        // Recalculate bounds from the transformed points.
        glyphPoints.Bounds = CalculateBounds(controlPoints);
    }

    /// <summary>
    /// Gets the horizontal advance width adjustment for the given glyph from the HVAR table.
    /// Returns 0 if no HVAR table is present.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The advance width delta value.</returns>
    public float AdvanceAdjustment(int glyphId)
    {
        if (this.hVar is null)
        {
            return 0;
        }

        return this.GetMetricDelta(glyphId, this.hVar.AdvanceWidthMapping, this.hVar.ItemVariationStore);
    }

    /// <summary>
    /// Gets the vertical advance height adjustment for the given glyph from the VVAR table.
    /// Returns 0 if no VVAR table is present.
    /// </summary>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns>The advance height delta value.</returns>
    public float VerticalAdvanceAdjustment(int glyphId)
    {
        if (this.vVar is null)
        {
            return 0;
        }

        return this.GetMetricDelta(glyphId, this.vVar.AdvanceWidthMapping, this.vVar.ItemVariationStore);
    }

    /// <summary>
    /// Computes the blend vector for the given outer index in the item variation store.
    /// Used by the CFF2 blend operator.
    /// </summary>
    /// <param name="outerIndex">The outer index into the item variation store.</param>
    /// <returns>An array of blend scalars, one per region.</returns>
    public float[] BlendVector(int outerIndex)
    {
        if (this.itemStore is null)
        {
            return [];
        }

        ItemVariationData variationData = this.itemStore.ItemVariations[outerIndex];
        if (this.blendVectors.TryGetValue(variationData, out float[]? blendVector))
        {
            return blendVector;
        }

        blendVector = new float[variationData.RegionIndexes.Length];

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
                    // Ignore this region if coords are out of range.
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
                        axisScalar = (this.normalizedCoords[j] - axis.StartCoord) /
                                     (axis.PeakCoord - axis.StartCoord);
                    }
                    else
                    {
                        axisScalar = (axis.EndCoord - this.normalizedCoords[j]) /
                                     (axis.EndCoord - axis.PeakCoord);
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

    /// <summary>
    /// Computes the delta adjustment for a specific item in the item variation store.
    /// </summary>
    /// <param name="outerIndex">The outer index.</param>
    /// <param name="innerIndex">The inner index.</param>
    /// <returns>The delta value.</returns>
    internal float Delta(int outerIndex, int innerIndex)
    {
        if (this.itemStore is null || outerIndex >= this.itemStore.ItemVariations.Length)
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
        float netAdjustment = 0;
        for (int master = 0; master < variationData.RegionIndexes.Length; master++)
        {
            netAdjustment += deltaSet.Deltas[master] * blendVector[master];
        }

        return netAdjustment;
    }

    /// <summary>
    /// Calculates the blending factor for a gvar tuple variation based on normalized coordinates.
    /// </summary>
    /// <param name="isIntermediate">Whether this is an intermediate tuple with explicit start/end bounds.</param>
    /// <param name="peakCoords">The peak coordinates for this tuple.</param>
    /// <param name="startCoords">The start coordinates (only for intermediate tuples).</param>
    /// <param name="endCoords">The end coordinates (only for intermediate tuples).</param>
    /// <returns>A scalar factor in the range [0, 1] indicating how much this tuple contributes.</returns>
    private float TupleFactor(bool isIntermediate, float[] peakCoords, float[]? startCoords, float[]? endCoords)
    {
        float factor = 1.0f;

        for (int i = 0; i < this.normalizedCoords.Length && i < peakCoords.Length; i++)
        {
            if (peakCoords[i] == 0)
            {
                // This axis doesn't affect this tuple.
                continue;
            }

            if (this.normalizedCoords[i] == 0)
            {
                // Normalized coordinate is at default; this tuple has no effect.
                return 0;
            }

            if (!isIntermediate)
            {
                // Non-intermediate tuple: simple linear interpolation.
                // The valid range is between 0 and the peak coordinate.
                float minVal = MathF.Min(0, peakCoords[i]);
                float maxVal = MathF.Max(0, peakCoords[i]);

                if (this.normalizedCoords[i] < minVal || this.normalizedCoords[i] > maxVal)
                {
                    return 0;
                }

                factor *= this.normalizedCoords[i] / peakCoords[i];
            }
            else
            {
                // Intermediate tuple: piecewise linear between start → peak → end.
                if (this.normalizedCoords[i] < startCoords![i] || this.normalizedCoords[i] > endCoords![i])
                {
                    return 0;
                }

                if (this.normalizedCoords[i] < peakCoords[i])
                {
                    factor *= (this.normalizedCoords[i] - startCoords[i]) /
                              (peakCoords[i] - startCoords[i]);
                }
                else if (this.normalizedCoords[i] > peakCoords[i])
                {
                    factor *= (endCoords![i] - this.normalizedCoords[i]) /
                              (endCoords[i] - peakCoords[i]);
                }

                // If exactly at peak, factor contribution is 1 (no change).
            }
        }

        return factor;
    }

    /// <summary>
    /// Normalizes axis coordinates to the [-1, 1] range and applies avar remapping if present.
    /// </summary>
    /// <param name="userCoordinates">
    /// Optional user-specified axis values in design space (e.g. weight=700).
    /// If null, default axis values are used.
    /// </param>
    /// <returns>An array of normalized coordinates for each axis.</returns>
    private float[] NormalizeCoords(float[]? userCoordinates)
    {
        int axisCount = this.fvar.AxisCount;

        // Use Buffer<T> for temporary coords to avoid heap allocation.
        using Buffer<float> coordsBuf = new(axisCount);
        Span<float> coords = coordsBuf.GetSpan();

        // Use user coordinates if provided, otherwise use defaults.
        for (int i = 0; i < axisCount; i++)
        {
            VariationAxisRecord axis = this.fvar.Axes[i];
            if (userCoordinates is not null && i < userCoordinates.Length)
            {
                // Clamp to valid axis range.
                coords[i] = Math.Clamp(userCoordinates[i], axis.MinValue, axis.MaxValue);
            }
            else
            {
                coords[i] = axis.DefaultValue;
            }
        }

        // The default mapping is linear along each axis, in two segments:
        // from the minValue to defaultValue, and from defaultValue to maxValue.
        float[] normalized = new float[axisCount];
        for (int i = 0; i < axisCount; i++)
        {
            VariationAxisRecord axis = this.fvar.Axes[i];
            if (coords[i] < axis.DefaultValue)
            {
                float denominator = axis.DefaultValue - axis.MinValue;
                normalized[i] = denominator > 0
                    ? (coords[i] - axis.DefaultValue) / denominator
                    : 0;
            }
            else
            {
                float denominator = axis.MaxValue - axis.DefaultValue;
                normalized[i] = denominator > 0
                    ? (coords[i] - axis.DefaultValue) / denominator
                    : 0;
            }
        }

        // If there is an avar table, the normalized value is remapped
        // by interpolating between the two nearest mapped values.
        if (this.avar is not null)
        {
            int segmentCount = Math.Min(this.avar.SegmentMaps.Length, axisCount);
            for (int i = 0; i < segmentCount; i++)
            {
                SegmentMapRecord segment = this.avar.SegmentMaps[i];
                for (int j = 0; j < segment.AxisValueMap.Length; j++)
                {
                    AxisValueMapRecord pair = segment.AxisValueMap[j];
                    if (j >= 1 && normalized[i] < pair.FromCoordinate)
                    {
                        AxisValueMapRecord prev = segment.AxisValueMap[j - 1];
                        float fromDelta = pair.FromCoordinate - prev.FromCoordinate;
                        if (fromDelta > 0)
                        {
                            normalized[i] = (((normalized[i] - prev.FromCoordinate) * (pair.ToCoordinate - prev.ToCoordinate)) /
                                            fromDelta) + prev.ToCoordinate;
                        }

                        break;
                    }
                }
            }
        }

        return normalized;
    }

    private float GetMetricDelta(int glyphId, DeltaSetIndexMap[]? mapping, ItemVariationStore store)
    {
        int outerIndex;
        int innerIndex;
        if (mapping is { Length: > 0 })
        {
            int idx = Math.Min(glyphId, mapping.Length - 1);
            outerIndex = mapping[idx].OuterIndex;
            innerIndex = mapping[idx].InnerIndex;
        }
        else
        {
            outerIndex = 0;
            innerIndex = glyphId;
        }

        if (outerIndex >= store.ItemVariations.Length)
        {
            return 0;
        }

        ItemVariationData variationData = store.ItemVariations[outerIndex];
        if (innerIndex >= variationData.DeltaSets.Length)
        {
            return 0;
        }

        DeltaSet deltaSet = variationData.DeltaSets[innerIndex];
        float[] blendVector = this.BlendVector(outerIndex);
        float netAdjustment = 0;
        for (int master = 0; master < variationData.RegionIndexes.Length; master++)
        {
            netAdjustment += deltaSet.Deltas[master] * blendVector[master];
        }

        return netAdjustment;
    }

    /// <summary>
    /// Decodes deferred delta data for the all-points case.
    /// </summary>
    private static void DecodeAllPointDeltas(byte[] rawData, int pointCount, out short[]? deltasX, out short[]? deltasY)
    {
        using MemoryStream ms = new(rawData);
        using BigEndianBinaryReader reader = new(ms, false);
        deltasX = GlyphVariationData.DecodePackedDeltas(reader, pointCount);
        deltasY = GlyphVariationData.DecodePackedDeltas(reader, pointCount);
    }

    /// <summary>
    /// Interpolates deltas for points that don't have explicit delta values.
    /// Processes each contour independently.
    /// </summary>
    private static void InterpolateMissingDeltas(
        IList<ControlPoint> points,
        IList<ControlPoint> origPoints,
        IReadOnlyList<ushort> endPoints,
        Span<float> adjustX,
        Span<float> adjustY,
        Span<byte> hasDelta)
    {
        if (points.Count == 0 || endPoints.Count == 0)
        {
            return;
        }

        int contourStart = 0;
        for (int c = 0; c < endPoints.Count; c++)
        {
            int contourEnd = endPoints[c];

            // Find first point with a delta in this contour.
            int firstDelta = -1;
            for (int p = contourStart; p <= contourEnd; p++)
            {
                if (hasDelta[p] != 0)
                {
                    firstDelta = p;
                    break;
                }
            }

            if (firstDelta < 0)
            {
                // No deltas in this contour, skip.
                contourStart = contourEnd + 1;
                continue;
            }

            int curDelta = firstDelta;
            int p2 = firstDelta + 1;
            while (p2 <= contourEnd)
            {
                if (hasDelta[p2] != 0)
                {
                    // Interpolate the gap between curDelta and p2.
                    DeltaInterpolate(curDelta + 1, p2 - 1, curDelta, p2, origPoints, adjustX, adjustY);
                    curDelta = p2;
                }

                p2++;
            }

            if (curDelta == firstDelta)
            {
                // Only one delta point in this contour: shift all other points by the same amount.
                DeltaShift(contourStart, contourEnd, curDelta, adjustX, adjustY);
            }
            else
            {
                // Interpolate remaining points that wrap around the contour boundary.
                // Points after the last delta point to end of contour, and start of contour to first delta.
                DeltaInterpolate(curDelta + 1, contourEnd, curDelta, firstDelta, origPoints, adjustX, adjustY);
                if (firstDelta > contourStart)
                {
                    DeltaInterpolate(contourStart, firstDelta - 1, curDelta, firstDelta, origPoints, adjustX, adjustY);
                }
            }

            contourStart = contourEnd + 1;
        }
    }

    /// <summary>
    /// Interpolates delta values for points between two reference points.
    /// Handles X and Y independently using linear interpolation with clamping.
    /// </summary>
    private static void DeltaInterpolate(
        int p1,
        int p2,
        int ref1,
        int ref2,
        IList<ControlPoint> origPoints,
        Span<float> adjustX,
        Span<float> adjustY)
    {
        if (p1 > p2)
        {
            return;
        }

        // Process X axis.
        InterpolateAxis(p1, p2, ref1, ref2, origPoints, adjustX, isX: true);

        // Process Y axis.
        InterpolateAxis(p1, p2, ref1, ref2, origPoints, adjustY, isX: false);
    }

    private static void InterpolateAxis(
        int p1,
        int p2,
        int ref1,
        int ref2,
        IList<ControlPoint> origPoints,
        Span<float> adjust,
        bool isX)
    {
        float in1 = isX ? origPoints[ref1].Point.X : origPoints[ref1].Point.Y;
        float in2 = isX ? origPoints[ref2].Point.X : origPoints[ref2].Point.Y;
        float out1 = in1 + adjust[ref1];
        float out2 = in2 + adjust[ref2];

        // Ensure in1 <= in2 for interpolation.
        if (in1 > in2)
        {
            (in1, in2) = (in2, in1);
            (out1, out2) = (out2, out1);
        }

        float scale = (in1 == in2 || out1 == out2)
            ? 0
            : (out2 - out1) / (in2 - in1);

        for (int p = p1; p <= p2; p++)
        {
            float inVal = isX ? origPoints[p].Point.X : origPoints[p].Point.Y;

            float outVal;
            if (inVal <= in1)
            {
                outVal = inVal + (out1 - in1);
            }
            else if (inVal >= in2)
            {
                outVal = inVal + (out2 - in2);
            }
            else
            {
                outVal = out1 + ((inVal - in1) * scale);
            }

            adjust[p] = outVal - inVal;
        }
    }

    /// <summary>
    /// Shifts all points in a contour range by the same delta as the reference point.
    /// Used when only one point in a contour has an explicit delta.
    /// </summary>
    private static void DeltaShift(int p1, int p2, int refPoint, Span<float> adjustX, Span<float> adjustY)
    {
        float deltaX = adjustX[refPoint];
        float deltaY = adjustY[refPoint];

        if (deltaX == 0 && deltaY == 0)
        {
            return;
        }

        for (int p = p1; p <= p2; p++)
        {
            if (p != refPoint)
            {
                adjustX[p] = deltaX;
                adjustY[p] = deltaY;
            }
        }
    }

    private static Bounds CalculateBounds(IList<ControlPoint> points)
    {
        if (points.Count == 0)
        {
            return default;
        }

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 pt = points[i].Point;
            if (pt.X < minX)
            {
                minX = pt.X;
            }

            if (pt.Y < minY)
            {
                minY = pt.Y;
            }

            if (pt.X > maxX)
            {
                maxX = pt.X;
            }

            if (pt.Y > maxY)
            {
                maxY = pt.Y;
            }
        }

        return new Bounds(minX, minY, maxX, maxY);
    }
}
