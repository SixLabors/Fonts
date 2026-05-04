// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// Represents a Unicode grapheme cluster and metadata derived while enumerating it.
/// </summary>
public readonly ref struct GraphemeCluster
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphemeCluster"/> struct.
    /// </summary>
    /// <param name="span">The UTF-16 span containing the grapheme cluster.</param>
    /// <param name="utf16Offset">The UTF-16 offset of the cluster in the original source.</param>
    /// <param name="codePointCount">The number of Unicode scalar values in the cluster.</param>
    /// <param name="terminalCellWidth">The policy-resolved terminal cell width of the cluster.</param>
    /// <param name="flags">The cluster flags derived while scanning the cluster.</param>
    /// <param name="firstCodePoint">The first code point in the cluster.</param>
    public GraphemeCluster(
        ReadOnlySpan<char> span,
        int utf16Offset,
        int codePointCount,
        int terminalCellWidth,
        GraphemeClusterFlags flags,
        CodePoint firstCodePoint)
    {
        this.Span = span;
        this.Utf16Offset = utf16Offset;
        this.CodePointCount = codePointCount;
        this.Utf16Length = span.Length;
        this.TerminalCellWidth = terminalCellWidth;
        this.Flags = flags;
        this.FirstCodePoint = firstCodePoint;
    }

    /// <summary>
    /// Gets the UTF-16 span containing the grapheme cluster.
    /// </summary>
    public ReadOnlySpan<char> Span { get; }

    /// <summary>
    /// Gets the UTF-16 offset of the cluster in the original source.
    /// </summary>
    public int Utf16Offset { get; }

    /// <summary>
    /// Gets the UTF-16 length of the cluster.
    /// </summary>
    public int Utf16Length { get; }

    /// <summary>
    /// Gets the number of Unicode scalar values in the cluster.
    /// </summary>
    public int CodePointCount { get; }

    /// <summary>
    /// Gets the policy-resolved terminal cell width of the cluster.
    /// </summary>
    public int TerminalCellWidth { get; }

    /// <summary>
    /// Gets the cluster flags derived while scanning the cluster.
    /// </summary>
    public GraphemeClusterFlags Flags { get; }

    /// <summary>
    /// Gets the first code point in the cluster.
    /// </summary>
    public CodePoint FirstCodePoint { get; }
}
