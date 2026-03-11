// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a single variation axis setting for a variable font,
/// consisting of a four-character tag and a value.
/// </summary>
/// <remarks>
/// Follows CSS <c>font-variation-settings</c> semantics.
/// Values are clamped to the axis range defined in the font's <c>fvar</c> table.
/// </remarks>
[DebuggerDisplay("Tag: {Tag}, Value: {Value}")]
public readonly struct FontVariation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontVariation"/> struct.
    /// </summary>
    /// <param name="tag">The four-character axis tag (e.g. "wght", "wdth", "opsz").</param>
    /// <param name="value">The axis value in design-space units.</param>
    public FontVariation(string tag, float value)
    {
        Guard.NotNullOrWhiteSpace(tag, nameof(tag));
        if (tag.Length != 4)
        {
            throw new ArgumentException("Variation axis tag must be exactly 4 characters.", nameof(tag));
        }

        this.Tag = tag;
        this.Value = value;
    }

    /// <summary>
    /// Gets the four-character axis tag identifying the design variation.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets the axis value in design-space units.
    /// </summary>
    public float Value { get; }
}
