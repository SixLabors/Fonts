// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Represents a hit-tested grapheme position in laid-out text.
/// </summary>
public readonly struct TextHit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextHit"/> struct.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
    /// <param name="stringIndex">The UTF-16 index in the original text.</param>
    /// <param name="isTrailing">Whether the hit is on the trailing side of the grapheme.</param>
    internal TextHit(int lineIndex, int graphemeIndex, int stringIndex, bool isTrailing)
    {
        this.LineIndex = lineIndex;
        this.GraphemeIndex = graphemeIndex;
        this.StringIndex = stringIndex;
        this.IsTrailing = isTrailing;
    }

    /// <summary>
    /// Gets the zero-based line index.
    /// </summary>
    public int LineIndex { get; }

    /// <summary>
    /// Gets the zero-based grapheme index in the original text.
    /// </summary>
    public int GraphemeIndex { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 code unit index in the original text.
    /// </summary>
    public int StringIndex { get; }

    /// <summary>
    /// Gets the grapheme insertion index represented by this hit.
    /// </summary>
    public int GraphemeInsertionIndex => this.GraphemeIndex + (this.IsTrailing ? 1 : 0);

    /// <summary>
    /// Gets a value indicating whether the hit is on the trailing side of the grapheme.
    /// </summary>
    public bool IsTrailing { get; }
}
