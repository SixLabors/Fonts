// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Contains the width-independent result of shaping text before logical line composition.
/// </summary>
internal readonly struct ShapedText
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShapedText"/> struct.
    /// </summary>
    /// <param name="positionings">The positioned glyph shaping collection.</param>
    /// <param name="bidiRuns">The resolved bidi runs covering the shaped text.</param>
    /// <param name="bidiMap">The code point to bidi-run mapping built during shaping.</param>
    /// <param name="layoutMode">The layout mode used while shaping.</param>
    public ShapedText(
        GlyphPositioningCollection positionings,
        BidiRun[] bidiRuns,
        Dictionary<int, int> bidiMap,
        LayoutMode layoutMode)
    {
        this.Positionings = positionings;
        this.BidiRuns = bidiRuns;
        this.BidiMap = bidiMap;
        this.LayoutMode = layoutMode;
    }

    /// <summary>
    /// Gets the positioned glyph shaping collection.
    /// </summary>
    public GlyphPositioningCollection Positionings { get; }

    /// <summary>
    /// Gets the resolved bidi runs covering the shaped text.
    /// </summary>
    public BidiRun[] BidiRuns { get; }

    /// <summary>
    /// Gets the code point to bidi-run mapping built during shaping.
    /// </summary>
    public Dictionary<int, int> BidiMap { get; }

    /// <summary>
    /// Gets the layout mode used while shaping.
    /// </summary>
    public LayoutMode LayoutMode { get; }
}
