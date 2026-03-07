// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// Each RegionAxisCoordinates record provides coordinate values for a region along a single axis.
/// The three values must all be within the range -1.0 to +1.0. startCoord must be less than or equal to peakCoord,
/// and peakCoord must be less than or equal to endCoord. The three values must be either all non-positive or all non-negative with one possible exception:
/// if peakCoord is zero, then startCoord can be negative or 0 while endCoord can be positive or zero.
/// <see href="https://docs.microsoft.com/de-de/typography/opentype/spec/otvarcommonformats#variation-regions"/>
/// </summary>
[DebuggerDisplay("StartCoord: {StartCoord}, PeakCoord: {PeakCoord}, EndCoord: {EndCoord}")]
public readonly struct RegionAxisCoordinates
{
    /// <summary>
    /// Gets the region start coordinate value for the current axis.
    /// </summary>
    public float StartCoord { get; init; }

    /// <summary>
    /// Gets the region peak coordinate value for the current axis.
    /// </summary>
    public float PeakCoord { get; init; }

    /// <summary>
    /// Gets the region end coordinate value for the current axis.
    /// </summary>
    public float EndCoord { get; init; }
}
