// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Walks a <see cref="TextBlock"/> one laid-out line at a time.
/// </summary>
/// <remarks>
/// Each produced line is positioned independently, without cumulative offsets from earlier or later lines.
/// </remarks>
public sealed class LineLayoutEnumerator
{
    private readonly TextBlock textBlock;
    private readonly TextLineBreakEnumerator lineEnumerator;
    private readonly TextDirection textDirection;
    private readonly bool suppressLayout;
    private LineLayout? current;

    /// <summary>
    /// Initializes a new instance of the <see cref="LineLayoutEnumerator"/> class.
    /// </summary>
    /// <param name="textBlock">The prepared text block to enumerate.</param>
    internal LineLayoutEnumerator(TextBlock textBlock)
    {
        this.textBlock = textBlock;
        this.lineEnumerator = new(textBlock.LogicalLine, textBlock.Options);
        this.textDirection = TextLayout.GetTextDirection(textBlock.LogicalLine, textBlock.Options);
        this.suppressLayout = textBlock.Options.MaxLines == 0;
    }

    /// <summary>
    /// Gets the current line layout.
    /// </summary>
    public LineLayout Current => this.current!;

    /// <summary>
    /// Advances to the next line using the supplied wrapping length.
    /// </summary>
    /// <remarks>
    /// The wrapping length applies only to the line being produced by this call.
    /// </remarks>
    /// <param name="wrappingLength">The wrapping length in pixels. Use <c>-1</c> to disable wrapping.</param>
    /// <returns><see langword="true"/> when a line was produced.</returns>
    public bool MoveNext(float wrappingLength)
    {
        if (this.suppressLayout)
        {
            return false;
        }

        if (!this.lineEnumerator.MoveNext(wrappingLength))
        {
            return false;
        }

        // The walker lays out each produced line independently so callers can
        // place variable-width lines into custom columns, shapes, or virtualized
        // surfaces without inheriting block-level line offsets.
        this.current = this.textBlock.GetLineLayout(
            this.lineEnumerator.Current,
            wrappingLength,
            this.textDirection);

        return true;
    }
}
