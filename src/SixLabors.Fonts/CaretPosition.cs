// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Represents a caret line in laid-out text.
/// </summary>
public readonly struct CaretPosition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CaretPosition"/> struct.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text.</param>
    /// <param name="start">The caret start point in pixel units.</param>
    /// <param name="end">The caret end point in pixel units.</param>
    /// <param name="hasSecondary">Whether the caret has a second visual position.</param>
    /// <param name="secondaryStart">The secondary caret start point in pixel units.</param>
    /// <param name="secondaryEnd">The secondary caret end point in pixel units.</param>
    /// <param name="lineNavigationPosition">The position to preserve when moving between visual lines.</param>
    internal CaretPosition(
        int lineIndex,
        int graphemeIndex,
        int stringIndex,
        Vector2 start,
        Vector2 end,
        bool hasSecondary,
        Vector2 secondaryStart,
        Vector2 secondaryEnd,
        float lineNavigationPosition)
    {
        this.LineIndex = lineIndex;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
        this.Start = start;
        this.End = end;
        this.HasSecondary = hasSecondary;
        this.SecondaryStart = secondaryStart;
        this.SecondaryEnd = secondaryEnd;
        this.LineNavigationPosition = lineNavigationPosition;
    }

    /// <summary>
    /// Gets the zero-based line index.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the grapheme insertion index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the UTF-16 index in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets the caret start point in pixel units.
    /// </summary>
    public Vector2 Start { get; }

    /// <summary>
    /// Gets the caret end point in pixel units.
    /// </summary>
    public Vector2 End { get; }

    /// <summary>
    /// Gets a value indicating whether a second visual caret position is available.
    /// </summary>
    public bool HasSecondary { get; }

    /// <summary>
    /// Gets the secondary caret start point in pixel units.
    /// </summary>
    public Vector2 SecondaryStart { get; }

    /// <summary>
    /// Gets the secondary caret end point in pixel units.
    /// </summary>
    public Vector2 SecondaryEnd { get; }

    /// <summary>
    /// Gets the position to preserve when moving between visual lines.
    /// </summary>
    internal float LineNavigationPosition { get; }
}
