// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

/// <summary>
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/otspec184/fvar#variationaxisrecord"/>
/// </summary>
[DebuggerDisplay("Name: {Name}, Tag: {Tag}, Min: {Min}, Max: {Max}, Default: {Default}")]
public readonly struct VariationAxis
{
    /// <summary>
    /// Gets the name of the axes.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets tag identifying the design variation for the axis.
    /// </summary>
    public string Tag { get; init; }

    /// <summary>
    /// Gets the minimum coordinate value for the axis.
    /// </summary>
    public float Min { get; init; }

    /// <summary>
    /// Gets the maximum coordinate value for the axis.
    /// </summary>
    public float Max { get; init; }

    /// <summary>
    /// Gets the default coordinate value for the axis.
    /// </summary>
    public float Default { get; init; }
}
