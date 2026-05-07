// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents an atomic inline placeholder with caller-supplied dimensions.
/// </summary>
public readonly struct TextPlaceholder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextPlaceholder"/> struct.
    /// </summary>
    /// <param name="width">The placeholder width in pixel units.</param>
    /// <param name="height">The placeholder height in pixel units.</param>
    /// <param name="alignment">The placeholder alignment against surrounding text.</param>
    /// <param name="baselineOffset">The distance from the placeholder top edge to its baseline in pixel units.</param>
    public TextPlaceholder(
        float width,
        float height,
        TextPlaceholderAlignment alignment,
        float baselineOffset)
    {
        this.Width = width;
        this.Height = height;
        this.Alignment = alignment;
        this.BaselineOffset = baselineOffset;
    }

    /// <summary>
    /// Gets the placeholder width in pixel units.
    /// </summary>
    public float Width { get; }

    /// <summary>
    /// Gets the placeholder height in pixel units.
    /// </summary>
    public float Height { get; }

    /// <summary>
    /// Gets the placeholder alignment against surrounding text.
    /// </summary>
    public TextPlaceholderAlignment Alignment { get; }

    /// <summary>
    /// Gets the distance from the placeholder top edge to its baseline in pixel units.
    /// </summary>
    public float BaselineOffset { get; }
}
