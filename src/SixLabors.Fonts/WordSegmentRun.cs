// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Describes one source-order Unicode word-boundary segment run.
/// </summary>
internal readonly struct WordSegmentRun
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WordSegmentRun"/> struct.
    /// </summary>
    /// <param name="graphemeStart">The inclusive grapheme insertion index where the word-boundary segment starts.</param>
    /// <param name="graphemeEnd">The exclusive grapheme insertion index where the word-boundary segment ends.</param>
    /// <param name="stringStart">The inclusive UTF-16 index where the word-boundary segment starts.</param>
    /// <param name="stringEnd">The exclusive UTF-16 index where the word-boundary segment ends.</param>
    public WordSegmentRun(
        int graphemeStart,
        int graphemeEnd,
        int stringStart,
        int stringEnd)
    {
        this.GraphemeStart = graphemeStart;
        this.GraphemeEnd = graphemeEnd;
        this.StringStart = stringStart;
        this.StringEnd = stringEnd;
    }

    /// <summary>
    /// Gets the inclusive grapheme insertion index where the word-boundary segment starts.
    /// </summary>
    public int GraphemeStart { get; }

    /// <summary>
    /// Gets the exclusive grapheme insertion index where the word-boundary segment ends.
    /// </summary>
    public int GraphemeEnd { get; }

    /// <summary>
    /// Gets the inclusive UTF-16 index where the word-boundary segment starts.
    /// </summary>
    public int StringStart { get; }

    /// <summary>
    /// Gets the exclusive UTF-16 index where the word-boundary segment ends.
    /// </summary>
    public int StringEnd { get; }
}
