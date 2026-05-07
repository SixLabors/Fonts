// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts;

/// <summary>
/// Provides shared helpers for text interaction metrics.
/// </summary>
/// <remarks>
/// Text interaction uses grapheme advance rectangles as the logical hit target. Ink bounds can be
/// empty, overhang the advance, or exclude whitespace, which makes them unsuitable for caret
/// positioning and selection highlighting.
/// </remarks>
internal static class TextInteraction
{
    /// <summary>
    /// Hit tests a point against a complete laid-out text box.
    /// </summary>
    /// <param name="lines">All laid-out lines ordered by their visual position.</param>
    /// <param name="graphemes">The full grapheme metrics buffer flattened in visual order.</param>
    /// <param name="point">The text-space coordinate to resolve to a grapheme hit.</param>
    /// <param name="layoutMode">The orientation used to interpret the line and grapheme advances.</param>
    /// <returns>The nearest grapheme hit.</returns>
    public static TextHit HitTest(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        Vector2 point,
        LayoutMode layoutMode)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return new(-1, -1, -1, false);
        }

        bool isHorizontal = layoutMode.IsHorizontal();
        int lineIndex = FindLine(lines, point, isHorizontal);

        // LineMetrics preserve their source line index, while grapheme metrics are emitted in
        // visual line order. Locate the line slice by source range so reverse line-order modes
        // pair the hit-tested line with its own graphemes.
        int graphemeOffset = GetGraphemeOffset(lines[lineIndex]);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, lines[lineIndex].GraphemeCount);

        return HitTestLine(lineIndex, lineGraphemes, point, isHorizontal);
    }

    /// <summary>
    /// Hit tests a point against one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the line being hit tested.</param>
    /// <param name="graphemes">Only the grapheme metrics belonging to the target line.</param>
    /// <param name="point">The coordinate to compare against the line's primary advance axis.</param>
    /// <param name="layoutMode">The line orientation that determines which axis is primary.</param>
    /// <returns>The nearest grapheme hit.</returns>
    public static TextHit HitTestLine(
        int lineIndex,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        Vector2 point,
        LayoutMode layoutMode)
        => HitTestLine(lineIndex, graphemes, point, layoutMode.IsHorizontal());

    /// <summary>
    /// Gets a caret position from a complete laid-out text box.
    /// </summary>
    /// <param name="lines">All laid-out lines available for caret placement.</param>
    /// <param name="graphemes">The flattened grapheme metrics that back the full text box.</param>
    /// <param name="graphemeIndex">The logical insertion position to convert into a visual caret.</param>
    /// <param name="layoutMode">The layout orientation used when the caret geometry was calculated.</param>
    /// <returns>The caret position in pixel units.</returns>
    public static CaretPosition GetCaretPosition(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeIndex,
        LayoutMode layoutMode)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return new(-1, -1, -1, default, default, false, default, default, 0);
        }

        int lineIndex = FindLineByGraphemeIndex(lines, graphemeIndex);
        LineMetrics line = lines[lineIndex];

        // See HitTest: line source indices and flattened storage offsets are deliberately
        // separate because bidi reordering can make source order differ from visual order.
        int graphemeOffset = GetGraphemeOffset(line);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);

        return GetCaretPositionLine(lineIndex, line, lineGraphemes, graphemeIndex, layoutMode);
    }

    /// <summary>
    /// Gets a caret position from one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the supplied line.</param>
    /// <param name="line">The metrics for the single line that will host the caret.</param>
    /// <param name="graphemes">The visual-order grapheme metrics for that one line.</param>
    /// <param name="graphemeIndex">The logical insertion position to place within the supplied line.</param>
    /// <param name="layoutMode">The orientation that determines the caret edge direction.</param>
    /// <returns>The caret position in pixel units.</returns>
    public static CaretPosition GetCaretPositionLine(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeIndex,
        LayoutMode layoutMode)
    {
        if (graphemes.IsEmpty)
        {
            return new(lineIndex, line.GraphemeIndex, line.StringIndex, default, default, false, default, default, 0);
        }

        return CreateCaret(lineIndex, line, graphemes, graphemeIndex, layoutMode.IsHorizontal());
    }

    /// <summary>
    /// Gets an absolute caret position from a complete laid-out text box.
    /// </summary>
    /// <param name="lines">All laid-out lines available for caret placement.</param>
    /// <param name="graphemes">The flattened grapheme metrics that back the full text box.</param>
    /// <param name="placement">The absolute placement within the text box.</param>
    /// <param name="layoutMode">The layout orientation used when the caret geometry was calculated.</param>
    /// <param name="direction">The resolved text direction used to choose the visual start or end of the scope.</param>
    /// <returns>The caret position in pixel units.</returns>
    public static CaretPosition GetCaret(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPlacement placement,
        LayoutMode layoutMode,
        TextDirection direction)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return new(-1, -1, -1, default, default, false, default, default, 0);
        }

        int targetGraphemeIndex = placement == CaretPlacement.Start
            ? GetSourceTextStart(graphemes)
            : GetSourceTextEnd(graphemes);

        int lineIndex = FindLineByGraphemeIndex(lines, targetGraphemeIndex);

        LineMetrics line = lines[lineIndex];
        int graphemeOffset = GetGraphemeOffset(line);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);

        return GetCaretLine(lineIndex, line, lineGraphemes, placement, layoutMode, direction);
    }

    /// <summary>
    /// Gets an absolute caret position from one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the supplied line.</param>
    /// <param name="line">The metrics for the single line that will host the caret.</param>
    /// <param name="graphemes">The visual-order grapheme metrics for that one line.</param>
    /// <param name="placement">The absolute placement within the line.</param>
    /// <param name="layoutMode">The orientation that determines the caret edge direction.</param>
    /// <param name="direction">The resolved text direction used to choose the visual start or end of the scope.</param>
    /// <returns>The caret position in pixel units.</returns>
    public static CaretPosition GetCaretLine(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPlacement placement,
        LayoutMode layoutMode,
        TextDirection direction)
    {
        if (graphemes.IsEmpty)
        {
            return new(lineIndex, line.GraphemeIndex, line.StringIndex, default, default, false, default, default, 0);
        }

        return CreateCaretAtVisualLineEdge(lineIndex, line, graphemes, placement, layoutMode.IsHorizontal(), direction);
    }

    /// <summary>
    /// Moves a caret within a complete laid-out text box.
    /// </summary>
    /// <param name="lines">The visual lines across which the caret may move.</param>
    /// <param name="graphemes">The flattened grapheme metrics used to resolve movement targets.</param>
    /// <param name="wordMetrics">The source-order word-boundary segment metrics used for word movement.</param>
    /// <param name="caret">The starting caret location before applying the movement.</param>
    /// <param name="movement">The requested caret navigation command.</param>
    /// <param name="layoutMode">The orientation rules that control horizontal versus vertical motion.</param>
    /// <param name="direction">The resolved text direction used to choose line and text start/end.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public static CaretPosition MoveCaret(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        ReadOnlySpan<WordMetrics> wordMetrics,
        CaretPosition caret,
        CaretMovement movement,
        LayoutMode layoutMode,
        TextDirection direction)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return caret;
        }

        bool isHorizontal = layoutMode.IsHorizontal();
        int lineIndex = GetCaretLineIndex(lines, graphemes, caret);
        LineMetrics line = lines[lineIndex];
        int graphemeOffset = GetGraphemeOffset(line);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);
        int target = caret.GraphemeIndex;
        switch (movement)
        {
            case CaretMovement.Previous:
                target = GetPreviousInsertionIndex(graphemes, caret.GraphemeIndex, GetSourceTextStart(graphemes));
                break;

            case CaretMovement.Next:
                target = GetNextInsertionIndex(graphemes, caret.GraphemeIndex, GetSourceTextEnd(graphemes));
                break;

            case CaretMovement.PreviousWord:
                target = GetPreviousWordBoundary(wordMetrics, caret.GraphemeIndex, GetSourceTextStart(graphemes));
                break;

            case CaretMovement.NextWord:
                target = GetNextWordBoundary(wordMetrics, caret.GraphemeIndex, GetSourceTextEnd(graphemes));
                break;

            case CaretMovement.LineStart:
                return GetCaretLine(lineIndex, line, lineGraphemes, CaretPlacement.Start, layoutMode, direction);

            case CaretMovement.LineEnd:
                return GetCaretLine(lineIndex, line, lineGraphemes, CaretPlacement.End, layoutMode, direction);

            case CaretMovement.TextStart:
                return GetCaret(lines, graphemes, CaretPlacement.Start, layoutMode, direction);

            case CaretMovement.TextEnd:
                return GetCaret(lines, graphemes, CaretPlacement.End, layoutMode, direction);

            case CaretMovement.LineUp:
                return MoveCaretToAdjacentLine(
                    lines,
                    graphemes,
                    caret,
                    lineIndex,
                    lineDown: false,
                    isHorizontal: isHorizontal,
                    layoutMode: layoutMode);

            case CaretMovement.LineDown:
                return MoveCaretToAdjacentLine(
                    lines,
                    graphemes,
                    caret,
                    lineIndex,
                    lineDown: true,
                    isHorizontal: isHorizontal,
                    layoutMode: layoutMode);
        }

        return GetCaretPosition(lines, graphemes, target, layoutMode);
    }

    /// <summary>
    /// Moves a caret within one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the current line.</param>
    /// <param name="line">The line metrics that constrain the movement.</param>
    /// <param name="graphemes">The grapheme metrics available within that line.</param>
    /// <param name="wordMetrics">The source-order word-boundary segment metrics used for word movement.</param>
    /// <param name="caret">The caret location to move inside the line.</param>
    /// <param name="movement">The in-line caret navigation command to execute.</param>
    /// <param name="layoutMode">The orientation used to choose the caret axis within the line.</param>
    /// <param name="direction">The resolved text direction used to choose line start/end.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public static CaretPosition MoveCaretLine(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        ReadOnlySpan<WordMetrics> wordMetrics,
        CaretPosition caret,
        CaretMovement movement,
        LayoutMode layoutMode,
        TextDirection direction)
    {
        if (graphemes.IsEmpty)
        {
            return caret;
        }

        int lineStart = GetSourceLineStart(graphemes);
        int lineEnd = GetSourceLineEnd(graphemes);
        int target = caret.GraphemeIndex;
        switch (movement)
        {
            case CaretMovement.Previous:
                target = GetPreviousInsertionIndex(graphemes, caret.GraphemeIndex, lineStart);
                break;

            case CaretMovement.Next:
                target = GetNextInsertionIndex(graphemes, caret.GraphemeIndex, lineEnd);
                break;

            case CaretMovement.PreviousWord:
                target = Math.Max(
                    lineStart,
                    GetPreviousWordBoundary(wordMetrics, caret.GraphemeIndex, lineStart));
                break;

            case CaretMovement.NextWord:
                target = Math.Min(
                    lineEnd,
                    GetNextWordBoundary(wordMetrics, caret.GraphemeIndex, lineEnd));
                break;

            case CaretMovement.LineStart:
            case CaretMovement.TextStart:
                return GetCaretLine(lineIndex, line, graphemes, CaretPlacement.Start, layoutMode, direction);

            case CaretMovement.LineEnd:
            case CaretMovement.TextEnd:
                return GetCaretLine(lineIndex, line, graphemes, CaretPlacement.End, layoutMode, direction);

            case CaretMovement.LineUp:
            case CaretMovement.LineDown:
                return caret;
        }

        return GetCaretPositionLine(lineIndex, line, graphemes, target, layoutMode);
    }

    /// <summary>
    /// Gets the word-boundary segment metrics containing the supplied grapheme insertion index.
    /// </summary>
    /// <param name="wordMetrics">The source-order word metrics to search.</param>
    /// <param name="graphemeIndex">The grapheme insertion index to locate.</param>
    /// <returns>The matching word metrics.</returns>
    public static WordMetrics GetWordMetrics(ReadOnlySpan<WordMetrics> wordMetrics, int graphemeIndex)
    {
        if (wordMetrics.IsEmpty)
        {
            return default;
        }

        for (int i = 0; i < wordMetrics.Length; i++)
        {
            WordMetrics metrics = wordMetrics[i];
            if (graphemeIndex >= metrics.GraphemeStart && graphemeIndex < metrics.GraphemeEnd)
            {
                return metrics;
            }

            if (graphemeIndex < metrics.GraphemeStart)
            {
                return metrics;
            }
        }

        return wordMetrics[^1];
    }

    /// <summary>
    /// Gets selection rectangles from a complete laid-out text box.
    /// </summary>
    /// <param name="lines">The visual lines that may contribute selection rectangles.</param>
    /// <param name="graphemes">The flattened grapheme metrics scanned for the selected range.</param>
    /// <param name="graphemeStart">The first source grapheme insertion boundary in the selection.</param>
    /// <param name="graphemeEnd">The final source grapheme insertion boundary in the selection.</param>
    /// <param name="layoutMode">The orientation used when converting ranges into rectangles.</param>
    /// <returns>A read-only memory region containing the selection rectangles in visual order.</returns>
    public static ReadOnlyMemory<FontRectangle> GetSelectionBounds(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeStart,
        int graphemeEnd,
        LayoutMode layoutMode)
    {
        if (lines.IsEmpty || graphemes.IsEmpty || graphemeStart == graphemeEnd)
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        int selectionStart = Math.Min(graphemeStart, graphemeEnd);
        int selectionEnd = Math.Max(graphemeStart, graphemeEnd);
        int rectangleCount = CountSelectionBounds(lines, graphemes, selectionStart, selectionEnd);
        if (rectangleCount == 0)
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        FontRectangle[] result = new FontRectangle[rectangleCount];
        int count = 0;
        bool isHorizontal = layoutMode.IsHorizontal();

        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i];
            int graphemeOffset = GetGraphemeOffset(line);
            ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);
            if (CountSelectionBoundsLine(lineGraphemes, selectionStart, selectionEnd) == 0)
            {
                continue;
            }

            count += FillSelectionBoundsLine(line, lineGraphemes, selectionStart, selectionEnd, isHorizontal, result.AsSpan(count));
        }

        return result;
    }

    /// <summary>
    /// Gets selection rectangles for one laid-out line.
    /// </summary>
    /// <param name="line">The single line for which selection rectangles are produced.</param>
    /// <param name="graphemes">The line-local grapheme metrics scanned in visual order.</param>
    /// <param name="graphemeStart">The first source grapheme insertion boundary applied to this line.</param>
    /// <param name="graphemeEnd">The final source grapheme insertion boundary applied to this line.</param>
    /// <param name="layoutMode">The orientation used to map the selected run onto the line box.</param>
    /// <returns>A read-only memory region containing the line selection rectangles in visual order.</returns>
    public static ReadOnlyMemory<FontRectangle> GetSelectionBoundsLine(
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeStart,
        int graphemeEnd,
        LayoutMode layoutMode)
    {
        if (graphemes.IsEmpty || graphemeStart == graphemeEnd)
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        int selectionStart = Math.Min(graphemeStart, graphemeEnd);
        int selectionEnd = Math.Max(graphemeStart, graphemeEnd);
        int count = CountSelectionBoundsLine(graphemes, selectionStart, selectionEnd);
        if (count == 0)
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        FontRectangle[] result = new FontRectangle[count];
        _ = FillSelectionBoundsLine(line, graphemes, selectionStart, selectionEnd, layoutMode.IsHorizontal(), result);
        return result;
    }

    /// <summary>
    /// Gets selection bounds for one measured grapheme.
    /// </summary>
    /// <param name="lines">The visual lines used to find the grapheme's line box.</param>
    /// <param name="graphemes">The flattened grapheme metrics that back the full text box.</param>
    /// <param name="grapheme">The measured grapheme to select.</param>
    /// <param name="layoutMode">The orientation used to map the grapheme advance onto the line box.</param>
    /// <returns>A read-only memory region containing the grapheme selection bounds.</returns>
    public static ReadOnlyMemory<FontRectangle> GetSelectionBounds(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        in GraphemeMetrics grapheme,
        LayoutMode layoutMode)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        int lineIndex = FindLineByGraphemeIndex(lines, grapheme.GraphemeIndex);
        FontRectangle[] result = [CreateSelectionBounds(lines[lineIndex], grapheme, layoutMode.IsHorizontal())];
        return result;
    }

    /// <summary>
    /// Gets selection bounds for one measured grapheme within one laid-out line.
    /// </summary>
    /// <param name="line">The line that provides the cross-axis selection extent.</param>
    /// <param name="grapheme">The measured grapheme to select.</param>
    /// <param name="layoutMode">The orientation used to map the grapheme advance onto the line box.</param>
    /// <returns>A read-only memory region containing the grapheme selection bounds.</returns>
    public static ReadOnlyMemory<FontRectangle> GetSelectionBoundsLine(
        in LineMetrics line,
        in GraphemeMetrics grapheme,
        LayoutMode layoutMode)
    {
        FontRectangle[] result = [CreateSelectionBounds(line, grapheme, layoutMode.IsHorizontal())];
        return result;
    }

    /// <summary>
    /// Finds the visual line nearest to a point.
    /// </summary>
    /// <param name="lines">The candidate visual lines to compare with the point.</param>
    /// <param name="point">The coordinate whose cross-axis position selects the nearest line.</param>
    /// <param name="isHorizontal">Indicates whether line advances are measured along the x-axis.</param>
    /// <returns>The nearest line index.</returns>
    private static int FindLine(
        ReadOnlySpan<LineMetrics> lines,
        Vector2 point,
        bool isHorizontal)
    {
        float cross = isHorizontal ? point.Y : point.X;
        for (int i = 0; i < lines.Length; i++)
        {
            float lineStart = isHorizontal ? lines[i].Start.Y : lines[i].Start.X;
            float lineEnd = isHorizontal ? lines[i].Start.Y + lines[i].Extent.Y : lines[i].Start.X + lines[i].Extent.X;
            if (cross >= lineStart && cross < lineEnd)
            {
                return i;
            }
        }

        float lineFirstStart = isHorizontal ? lines[0].Start.Y : lines[0].Start.X;
        return cross < lineFirstStart ? 0 : lines.Length - 1;
    }

    /// <summary>
    /// Finds the line that owns the supplied grapheme index.
    /// </summary>
    /// <param name="lines">The visual lines whose source ranges are searched.</param>
    /// <param name="graphemeIndex">The source grapheme index to locate.</param>
    /// <returns>The nearest owning line index.</returns>
    private static int FindLineByGraphemeIndex(
        ReadOnlySpan<LineMetrics> lines,
        int graphemeIndex)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i];
            int lineStart = line.GraphemeIndex;
            int lineEnd = lineStart + line.GraphemeCount;
            if (graphemeIndex >= lineStart && graphemeIndex <= lineEnd)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>
    /// Hit tests a point against one laid-out line after the layout mode has been normalized.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the normalized line.</param>
    /// <param name="graphemes">The grapheme metrics already isolated for that line.</param>
    /// <param name="point">The coordinate to compare with each grapheme advance rectangle.</param>
    /// <param name="isHorizontal">Indicates whether the primary hit-test axis is horizontal.</param>
    /// <returns>The nearest grapheme hit.</returns>
    private static TextHit HitTestLine(
        int lineIndex,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        Vector2 point,
        bool isHorizontal)
    {
        int index = FindNearestGrapheme(graphemes, isHorizontal ? point.X : point.Y, isHorizontal);
        GraphemeMetrics grapheme = graphemes[index];
        FontRectangle advance = grapheme.Advance;
        float midpoint = isHorizontal
            ? advance.Left + (advance.Width * 0.5F)
            : advance.Top + (advance.Height * 0.5F);
        float primary = isHorizontal ? point.X : point.Y;
        bool trailing = IsRightToLeft(grapheme)
            ? primary < midpoint
            : primary >= midpoint;

        return new(lineIndex, grapheme.GraphemeIndex, grapheme.StringIndex, trailing);
    }

    /// <summary>
    /// Creates a caret line for a grapheme insertion index.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the caret's line.</param>
    /// <param name="line">The line metrics used to size the caret segment.</param>
    /// <param name="graphemes">The line-local grapheme metrics searched for neighboring edges.</param>
    /// <param name="graphemeIndex">The logical insertion position to materialize as a caret.</param>
    /// <param name="isHorizontal">Indicates whether the caret spans vertically or horizontally.</param>
    /// <returns>The caret position in pixel units.</returns>
    private static CaretPosition CreateCaret(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeIndex,
        bool isHorizontal)
    {
        int previousIndex = FindGraphemeBySourceIndex(graphemes, graphemeIndex - 1);
        int nextIndex = FindGraphemeBySourceIndex(graphemes, graphemeIndex);

        if (nextIndex < 0 && previousIndex < 0)
        {
            int nearestIndex = FindNearestGraphemeIndex(graphemes, graphemeIndex);
            GraphemeMetrics nearest = graphemes[nearestIndex];
            bool trailing = graphemeIndex > nearest.GraphemeIndex;
            CreateCaretEdge(line, nearest, trailing, isHorizontal, out Vector2 start, out Vector2 end);

            return new(
                lineIndex,
                graphemeIndex,
                nearest.StringIndex,
                start,
                end,
                false,
                default,
                default,
                GetLineNavigationPosition(start, isHorizontal));
        }

        if (nextIndex >= 0)
        {
            GraphemeMetrics next = graphemes[nextIndex];
            CreateCaretEdge(line, next, trailing: false, isHorizontal, out Vector2 start, out Vector2 end);

            if (previousIndex >= 0)
            {
                GraphemeMetrics previous = graphemes[previousIndex];
                CreateCaretEdge(line, previous, trailing: true, isHorizontal, out Vector2 secondaryStart, out Vector2 secondaryEnd);

                // At a bidi boundary the same logical insertion point has one visual edge on
                // each neighboring run. Return both instead of asking callers to choose affinity.
                if (start != secondaryStart || end != secondaryEnd)
                {
                    return new(
                        lineIndex,
                        graphemeIndex,
                        next.StringIndex,
                        start,
                        end,
                        true,
                        secondaryStart,
                        secondaryEnd,
                        GetLineNavigationPosition(start, isHorizontal));
                }
            }

            return new(
                lineIndex,
                graphemeIndex,
                next.StringIndex,
                start,
                end,
                false,
                default,
                default,
                GetLineNavigationPosition(start, isHorizontal));
        }

        GraphemeMetrics previousOnly = graphemes[previousIndex];
        CreateCaretEdge(line, previousOnly, trailing: true, isHorizontal, out Vector2 primaryStart, out Vector2 primaryEnd);

        return new(
            lineIndex,
            graphemeIndex,
            previousOnly.StringIndex,
            primaryStart,
            primaryEnd,
            false,
            default,
            default,
            GetLineNavigationPosition(primaryStart, isHorizontal));
    }

    /// <summary>
    /// Creates one visual caret edge for a grapheme.
    /// </summary>
    /// <param name="line">The containing line that defines the caret span.</param>
    /// <param name="grapheme">The grapheme whose leading or trailing edge is used.</param>
    /// <param name="trailing">Specifies whether the logical trailing side should be chosen.</param>
    /// <param name="isHorizontal">Indicates whether caret edges vary along the x-axis.</param>
    /// <param name="start">Receives the first endpoint of the caret segment.</param>
    /// <param name="end">Receives the second endpoint of the caret segment.</param>
    private static void CreateCaretEdge(
        in LineMetrics line,
        in GraphemeMetrics grapheme,
        bool trailing,
        bool isHorizontal,
        out Vector2 start,
        out Vector2 end)
    {
        FontRectangle advance = grapheme.Advance;
        bool useEnd = IsRightToLeft(grapheme) ? !trailing : trailing;

        if (isHorizontal)
        {
            // Bidi layout can produce negative advance widths. Left/Right are
            // rectangle construction edges in that case, so choose the physical
            // min/max x edge after logical leading/trailing has been resolved.
            float physicalStart = MathF.Min(advance.Left, advance.Right);
            float physicalEnd = MathF.Max(advance.Left, advance.Right);
            float x = useEnd ? physicalEnd : physicalStart;

            start = new Vector2(x, line.Start.Y);
            end = new Vector2(x, line.Start.Y + line.Extent.Y);
            return;
        }

        float physicalTop = MathF.Min(advance.Top, advance.Bottom);
        float physicalBottom = MathF.Max(advance.Top, advance.Bottom);
        float y = useEnd ? physicalBottom : physicalTop;

        start = new Vector2(line.Start.X, y);
        end = new Vector2(line.Start.X + line.Extent.X, y);
    }

    /// <summary>
    /// Creates a caret at the source start or end boundary of a laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the line.</param>
    /// <param name="line">The line metrics used to size the caret segment.</param>
    /// <param name="graphemes">The line-local grapheme metrics in visual order.</param>
    /// <param name="placement">The source boundary to place within the line.</param>
    /// <param name="isHorizontal">Indicates whether the caret spans vertically or horizontally.</param>
    /// <param name="direction">The resolved text direction used to choose the visual start or end of the scope.</param>
    /// <returns>The caret position at the requested line boundary.</returns>
    private static CaretPosition CreateCaretAtVisualLineEdge(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPlacement placement,
        bool isHorizontal,
        TextDirection direction)
    {
        bool isStart = placement == CaretPlacement.Start;
        int insertionIndex = isStart ? GetSourceLineStart(graphemes) : GetSourceLineEnd(graphemes);
        int visualIndex = FindGraphemeBySourceIndex(graphemes, isStart ? insertionIndex : insertionIndex - 1);
        GraphemeMetrics grapheme = graphemes[visualIndex];
        bool isRightToLeft = direction == TextDirection.RightToLeft;
        bool useEnd = isStart == isRightToLeft;

        // Start/end placement is anchored to the source boundary grapheme for
        // the returned insertion index, but the visible caret sits on the line
        // box edge. The resolved paragraph direction chooses which physical
        // line edge represents start or end.
        Vector2 start;
        Vector2 end;
        if (isHorizontal)
        {
            float x = useEnd ? line.Start.X + line.Extent.X : line.Start.X;
            start = new Vector2(x, line.Start.Y);
            end = new Vector2(x, line.Start.Y + line.Extent.Y);
        }
        else
        {
            float y = useEnd ? line.Start.Y + line.Extent.Y : line.Start.Y;
            start = new Vector2(line.Start.X, y);
            end = new Vector2(line.Start.X + line.Extent.X, y);
        }

        return new(
            lineIndex,
            insertionIndex,
            grapheme.StringIndex,
            start,
            end,
            false,
            default,
            default,
            GetLineNavigationPosition(start, isHorizontal));
    }

    /// <summary>
    /// Moves the caret to the nearest matching position on an adjacent visual line.
    /// </summary>
    /// <param name="lines">The set of visual lines available for adjacent-line navigation.</param>
    /// <param name="graphemes">The flattened grapheme metrics used to resolve the new caret target.</param>
    /// <param name="caret">The caret location before moving to the neighbor line.</param>
    /// <param name="lineIndex">The visual index of the line that currently contains the caret.</param>
    /// <param name="lineDown">Specifies whether movement is toward the next visual line.</param>
    /// <param name="isHorizontal">Indicates whether preserved column data uses the x-axis.</param>
    /// <param name="layoutMode">The orientation used when reconstructing the destination caret.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    private static CaretPosition MoveCaretToAdjacentLine(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPosition caret,
        int lineIndex,
        bool lineDown,
        bool isHorizontal,
        LayoutMode layoutMode)
    {
        int targetLineIndex = FindAdjacentLine(lines, lineIndex, lineDown, isHorizontal);
        if (targetLineIndex == lineIndex)
        {
            return caret;
        }

        LineMetrics targetLine = lines[targetLineIndex];
        int graphemeOffset = GetGraphemeOffset(targetLine);
        ReadOnlySpan<GraphemeMetrics> targetGraphemes = graphemes.Slice(graphemeOffset, targetLine.GraphemeCount);

        Vector2 hitPoint = isHorizontal
            ? new(caret.LineNavigationPosition, targetLine.Start.Y + (targetLine.Extent.Y * 0.5F))
            : new(targetLine.Start.X + (targetLine.Extent.X * 0.5F), caret.LineNavigationPosition);

        TextHit hit = HitTestLineForCaretNavigation(targetLineIndex, targetGraphemes, hitPoint, isHorizontal);
        CaretPosition moved = GetCaretPositionLine(
            targetLineIndex,
            targetLine,
            targetGraphemes,
            hit.GraphemeInsertionIndex,
            layoutMode);

        // Preserve the original requested line position so repeated LineUp/LineDown movement
        // returns to the same visual column after passing through shorter lines.
        return WithLineNavigationPosition(moved, caret.LineNavigationPosition);
    }

    /// <summary>
    /// Hit tests a line for keyboard caret navigation.
    /// </summary>
    /// <param name="lineIndex">The zero-based visual index of the line being navigated.</param>
    /// <param name="graphemes">The line-local grapheme metrics considered as navigation targets.</param>
    /// <param name="point">The projected point used to preserve visual column alignment.</param>
    /// <param name="isHorizontal">Indicates whether navigation compares x coordinates first.</param>
    /// <returns>The nearest grapheme hit.</returns>
    private static TextHit HitTestLineForCaretNavigation(
        int lineIndex,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        Vector2 point,
        bool isHorizontal)
    {
        int index = FindNearestCaretNavigationGrapheme(graphemes, isHorizontal ? point.X : point.Y, isHorizontal);
        GraphemeMetrics grapheme = graphemes[index];
        FontRectangle advance = grapheme.Advance;
        float midpoint = isHorizontal
            ? advance.Left + (advance.Width * 0.5F)
            : advance.Top + (advance.Height * 0.5F);
        float primary = isHorizontal ? point.X : point.Y;
        bool trailing = IsRightToLeft(grapheme)
            ? primary < midpoint
            : primary >= midpoint;

        return new(lineIndex, grapheme.GraphemeIndex, grapheme.StringIndex, trailing);
    }

    /// <summary>
    /// Finds the nearest grapheme that should participate in keyboard caret navigation.
    /// </summary>
    /// <param name="graphemes">The visual-order graphemes filtered for caret navigation.</param>
    /// <param name="primary">The coordinate on the primary advance axis to compare.</param>
    /// <param name="isHorizontal">Indicates whether the primary axis maps to horizontal movement.</param>
    /// <returns>The nearest grapheme metrics index within <paramref name="graphemes"/>.</returns>
    private static int FindNearestCaretNavigationGrapheme(
        ReadOnlySpan<GraphemeMetrics> graphemes,
        float primary,
        bool isHorizontal)
    {
        int first = -1;
        int last = -1;
        for (int i = 0; i < graphemes.Length; i++)
        {
            first = first < 0 ? i : first;
            last = i;

            FontRectangle advance = graphemes[i].Advance;
            float start = isHorizontal ? advance.Left : advance.Top;
            float end = isHorizontal ? advance.Right : advance.Bottom;
            if (primary >= start && primary < end)
            {
                return i;
            }
        }

        FontRectangle firstAdvance = graphemes[first].Advance;
        float firstStart = isHorizontal ? firstAdvance.Left : firstAdvance.Top;
        return primary < firstStart ? first : last;
    }

    /// <summary>
    /// Finds the adjacent visual line in the requested direction.
    /// </summary>
    /// <param name="lines">The visual lines among which an adjacent line is searched.</param>
    /// <param name="lineIndex">The current visual line index.</param>
    /// <param name="lineDown">Specifies whether the search moves forward in visual order.</param>
    /// <param name="isHorizontal">Indicates whether cross-axis distances are measured vertically.</param>
    /// <returns>The adjacent line index, or <paramref name="lineIndex"/> when no line exists in that direction.</returns>
    private static int FindAdjacentLine(
        ReadOnlySpan<LineMetrics> lines,
        int lineIndex,
        bool lineDown,
        bool isHorizontal)
    {
        float currentStart = GetLineCrossStart(lines[lineIndex], isHorizontal);
        float currentEnd = GetLineCrossEnd(lines[lineIndex], isHorizontal);
        int targetLineIndex = lineIndex;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < lines.Length; i++)
        {
            if (i == lineIndex)
            {
                continue;
            }

            float distance = lineDown
                ? GetLineCrossStart(lines[i], isHorizontal) - currentEnd
                : currentStart - GetLineCrossEnd(lines[i], isHorizontal);

            if (distance >= 0 && distance < bestDistance)
            {
                targetLineIndex = i;
                bestDistance = distance;
            }
        }

        return targetLineIndex;
    }

    /// <summary>
    /// Gets a valid line index for the supplied caret.
    /// </summary>
    /// <param name="lines">The laid-out lines used to validate the caret's stored line index.</param>
    /// <param name="graphemes">The flattened grapheme metrics used to resolve the caret when its line index is stale.</param>
    /// <param name="caret">The caret whose associated visual line must be resolved.</param>
    /// <returns>The line index.</returns>
    private static int GetCaretLineIndex(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        in CaretPosition caret)
    {
        if ((uint)caret.LineIndex < (uint)lines.Length)
        {
            return caret.LineIndex;
        }

        return FindLineByGraphemeIndex(lines, caret.GraphemeIndex);
    }

    /// <summary>
    /// Gets the nearest Unicode word boundary before the supplied grapheme insertion index.
    /// </summary>
    /// <param name="wordMetrics">The source-order word metrics to search.</param>
    /// <param name="graphemeIndex">The grapheme insertion index to move from.</param>
    /// <param name="limit">The minimum grapheme insertion index that can be returned.</param>
    /// <returns>The previous word boundary.</returns>
    private static int GetPreviousWordBoundary(
        ReadOnlySpan<WordMetrics> wordMetrics,
        int graphemeIndex,
        int limit)
    {
        int target = limit;
        for (int i = 0; i < wordMetrics.Length; i++)
        {
            WordMetrics metrics = wordMetrics[i];
            if (metrics.GraphemeStart >= graphemeIndex)
            {
                break;
            }

            target = Math.Max(target, metrics.GraphemeStart);
            if (metrics.GraphemeEnd < graphemeIndex)
            {
                target = Math.Max(target, metrics.GraphemeEnd);
            }
        }

        return target;
    }

    /// <summary>
    /// Gets the nearest Unicode word boundary after the supplied grapheme insertion index.
    /// </summary>
    /// <param name="wordMetrics">The source-order word metrics to search.</param>
    /// <param name="graphemeIndex">The grapheme insertion index to move from.</param>
    /// <param name="limit">The maximum grapheme insertion index that can be returned.</param>
    /// <returns>The next word boundary.</returns>
    private static int GetNextWordBoundary(
        ReadOnlySpan<WordMetrics> wordMetrics,
        int graphemeIndex,
        int limit)
    {
        for (int i = 0; i < wordMetrics.Length; i++)
        {
            WordMetrics metrics = wordMetrics[i];
            if (metrics.GraphemeStart > graphemeIndex)
            {
                return Math.Min(limit, metrics.GraphemeStart);
            }

            if (metrics.GraphemeEnd > graphemeIndex)
            {
                return Math.Min(limit, metrics.GraphemeEnd);
            }
        }

        return limit;
    }

    /// <summary>
    /// Gets the previous measured grapheme insertion index.
    /// </summary>
    /// <param name="graphemes">The grapheme metrics that define valid caret stops.</param>
    /// <param name="graphemeIndex">The caret insertion index to move from.</param>
    /// <param name="limit">The minimum grapheme insertion index that can be returned.</param>
    /// <returns>The previous measured grapheme insertion index.</returns>
    private static int GetPreviousInsertionIndex(
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeIndex,
        int limit)
    {
        int target = limit;
        for (int i = 0; i < graphemes.Length; i++)
        {
            int start = graphemes[i].GraphemeIndex;
            if (start < graphemeIndex)
            {
                target = Math.Max(target, start);
            }

            // The trailing boundary is derived only from an actual measured grapheme.
            // This avoids walking through sparse source indices left by trimmed text.
            int end = start + 1;
            if (end < graphemeIndex)
            {
                target = Math.Max(target, end);
            }
        }

        return target;
    }

    /// <summary>
    /// Gets the next measured grapheme insertion index.
    /// </summary>
    /// <param name="graphemes">The grapheme metrics that define valid caret stops.</param>
    /// <param name="graphemeIndex">The caret insertion index to move from.</param>
    /// <param name="limit">The maximum grapheme insertion index that can be returned.</param>
    /// <returns>The next measured grapheme insertion index.</returns>
    private static int GetNextInsertionIndex(
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeIndex,
        int limit)
    {
        int target = limit;
        for (int i = 0; i < graphemes.Length; i++)
        {
            int start = graphemes[i].GraphemeIndex;
            if (start > graphemeIndex)
            {
                target = Math.Min(target, start);
            }

            // The trailing boundary is derived only from an actual measured grapheme.
            // This avoids walking through sparse source indices left by trimmed text.
            int end = start + 1;
            if (end > graphemeIndex)
            {
                target = Math.Min(target, end);
            }
        }

        return target;
    }

    /// <summary>
    /// Gets the first source grapheme insertion index in the laid-out text.
    /// </summary>
    /// <param name="graphemes">The laid-out grapheme metrics searched for the earliest source insertion point.</param>
    /// <returns>The source text start insertion index.</returns>
    private static int GetSourceTextStart(ReadOnlySpan<GraphemeMetrics> graphemes)
    {
        int start = graphemes[0].GraphemeIndex;
        for (int i = 1; i < graphemes.Length; i++)
        {
            start = Math.Min(start, graphemes[i].GraphemeIndex);
        }

        return start;
    }

    /// <summary>
    /// Gets the final source grapheme insertion index in the laid-out text.
    /// </summary>
    /// <param name="graphemes">The laid-out grapheme metrics searched for the final source insertion point.</param>
    /// <returns>The source text end insertion index.</returns>
    private static int GetSourceTextEnd(ReadOnlySpan<GraphemeMetrics> graphemes)
    {
        int end = graphemes[0].GraphemeIndex + 1;
        for (int i = 1; i < graphemes.Length; i++)
        {
            end = Math.Max(end, graphemes[i].GraphemeIndex + 1);
        }

        return end;
    }

    /// <summary>
    /// Gets the first source grapheme insertion index for a line.
    /// </summary>
    /// <param name="graphemes">The line-local grapheme metrics.</param>
    /// <returns>The source line start insertion index.</returns>
    private static int GetSourceLineStart(ReadOnlySpan<GraphemeMetrics> graphemes)
    {
        int start = graphemes[0].GraphemeIndex;
        for (int i = 1; i < graphemes.Length; i++)
        {
            start = Math.Min(start, graphemes[i].GraphemeIndex);
        }

        return start;
    }

    /// <summary>
    /// Gets the final source grapheme insertion index for a line.
    /// </summary>
    /// <param name="graphemes">The line-local grapheme metrics.</param>
    /// <returns>The source line end insertion index.</returns>
    private static int GetSourceLineEnd(ReadOnlySpan<GraphemeMetrics> graphemes)
    {
        int end = graphemes[0].GraphemeIndex + 1;
        for (int i = 1; i < graphemes.Length; i++)
        {
            end = Math.Max(end, graphemes[i].GraphemeIndex + 1);
        }

        return end;
    }

    /// <summary>
    /// Gets the cross-axis start of a line.
    /// </summary>
    /// <param name="line">The line whose cross-axis origin is requested.</param>
    /// <param name="isHorizontal">Indicates whether the cross axis corresponds to y coordinates.</param>
    /// <returns>The cross-axis start.</returns>
    private static float GetLineCrossStart(in LineMetrics line, bool isHorizontal)
        => isHorizontal ? line.Start.Y : line.Start.X;

    /// <summary>
    /// Gets the cross-axis end of a line.
    /// </summary>
    /// <param name="line">The line whose cross-axis limit is requested.</param>
    /// <param name="isHorizontal">Indicates whether the cross axis corresponds to y coordinates.</param>
    /// <returns>The cross-axis end.</returns>
    private static float GetLineCrossEnd(in LineMetrics line, bool isHorizontal)
        => isHorizontal ? line.Start.Y + line.Extent.Y : line.Start.X + line.Extent.X;

    /// <summary>
    /// Gets the coordinate to preserve for repeated visual line movement.
    /// </summary>
    /// <param name="start">The primary caret endpoint used to preserve visual column movement.</param>
    /// <param name="isHorizontal">Indicates whether the preserved coordinate is taken from x.</param>
    /// <returns>The line navigation position.</returns>
    private static float GetLineNavigationPosition(Vector2 start, bool isHorizontal)
        => isHorizontal ? start.X : start.Y;

    /// <summary>
    /// Creates a copy of the caret with a specific preserved line navigation position.
    /// </summary>
    /// <param name="caret">The caret value to clone with updated navigation metadata.</param>
    /// <param name="lineNavigationPosition">The preserved visual column or row coordinate.</param>
    /// <returns>The caret position.</returns>
    private static CaretPosition WithLineNavigationPosition(
        in CaretPosition caret,
        float lineNavigationPosition)
        => new(
            caret.LineIndex,
            caret.GraphemeIndex,
            caret.StringIndex,
            caret.Start,
            caret.End,
            caret.HasSecondary,
            caret.SecondaryStart,
            caret.SecondaryEnd,
            lineNavigationPosition);

    /// <summary>
    /// Fills one line's selection rectangles from visually contiguous selected grapheme advances.
    /// </summary>
    /// <param name="line">The line that will receive one or more selection rectangles.</param>
    /// <param name="graphemes">The line-local grapheme metrics grouped into visual runs.</param>
    /// <param name="selectionStart">The first source grapheme insertion boundary in the selected range.</param>
    /// <param name="selectionEnd">The final source grapheme insertion boundary in the selected range.</param>
    /// <param name="isHorizontal">Indicates whether rectangles expand primarily along x.</param>
    /// <param name="result">The destination span that receives the generated rectangles.</param>
    /// <returns>The number of selection rectangles written.</returns>
    private static int FillSelectionBoundsLine(
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int selectionStart,
        int selectionEnd,
        bool isHorizontal,
        Span<FontRectangle> result)
    {
        int count = 0;
        bool hasSelection = false;
        float start = 0;
        float end = 0;
        for (int i = 0; i < graphemes.Length; i++)
        {
            GraphemeMetrics grapheme = graphemes[i];

            // Selections are caret boundary ranges: [start, end). A grapheme is selected
            // when its source start sits inside that boundary span.
            int graphemeStart = grapheme.GraphemeIndex;
            bool isSelected = graphemeStart >= selectionStart && graphemeStart < selectionEnd;
            if (!isSelected)
            {
                // A logical range can be visually discontinuous after bidi reordering. Flush at
                // the first unselected visual grapheme so selection never covers that gap.
                if (hasSelection)
                {
                    result[count++] = CreateSelectionBounds(line, start, end, isHorizontal);
                    hasSelection = false;
                }

                continue;
            }

            FontRectangle advance = grapheme.Advance;
            float currentStart = isHorizontal ? advance.Left : advance.Top;
            float currentEnd = isHorizontal ? advance.Right : advance.Bottom;
            if (!hasSelection)
            {
                start = currentStart;
                end = currentEnd;
                hasSelection = true;
                continue;
            }

            start = Math.Min(start, currentStart);
            end = Math.Max(end, currentEnd);
        }

        if (hasSelection)
        {
            result[count++] = CreateSelectionBounds(line, start, end, isHorizontal);
        }

        return count;
    }

    /// <summary>
    /// Creates a selection rectangle for a contiguous visual run.
    /// </summary>
    /// <param name="line">The containing line used to fill the rectangle on the secondary axis.</param>
    /// <param name="start">The first selected coordinate along the primary layout axis.</param>
    /// <param name="end">The last selected coordinate along the primary layout axis.</param>
    /// <param name="isHorizontal">Indicates whether the primary axis runs left to right.</param>
    /// <returns>The selection rectangle in pixel units.</returns>
    private static FontRectangle CreateSelectionBounds(
        in LineMetrics line,
        float start,
        float end,
        bool isHorizontal)
        =>
        isHorizontal
        ? FontRectangle.FromLTRB(start, line.Start.Y, end, line.Start.Y + line.Extent.Y)
        : FontRectangle.FromLTRB(line.Start.X, start, line.Start.X + line.Extent.X, end);

    /// <summary>
    /// Creates a selection rectangle for one measured grapheme.
    /// </summary>
    /// <param name="line">The containing line used to fill the rectangle on the secondary axis.</param>
    /// <param name="grapheme">The grapheme whose advance defines the primary-axis selection extent.</param>
    /// <param name="isHorizontal">Indicates whether the primary axis runs left to right.</param>
    /// <returns>The selection rectangle in pixel units.</returns>
    private static FontRectangle CreateSelectionBounds(
        in LineMetrics line,
        in GraphemeMetrics grapheme,
        bool isHorizontal)
    {
        FontRectangle advance = grapheme.Advance;
        float start = isHorizontal ? advance.Left : advance.Top;
        float end = isHorizontal ? advance.Right : advance.Bottom;
        return CreateSelectionBounds(line, start, end, isHorizontal);
    }

    /// <summary>
    /// Counts how many selection rectangles are required for a grapheme range.
    /// </summary>
    /// <param name="lines">The visual lines searched for selected graphemes.</param>
    /// <param name="graphemes">The flattened grapheme metrics used to count visual runs.</param>
    /// <param name="selectionStart">The first source grapheme insertion boundary used for counting.</param>
    /// <param name="selectionEnd">The final source grapheme insertion boundary used for counting.</param>
    /// <returns>The number of selection rectangles.</returns>
    private static int CountSelectionBounds(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int selectionStart,
        int selectionEnd)
    {
        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i];
            int graphemeOffset = GetGraphemeOffset(line);
            ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);

            // Source grapheme indices can have gaps because trailing whitespace is trimmed.
            // Count actual measured graphemes instead of deriving a dense range from the line.
            count += CountSelectionBoundsLine(lineGraphemes, selectionStart, selectionEnd);
        }

        return count;
    }

    /// <summary>
    /// Counts visually contiguous selected grapheme runs in one line.
    /// </summary>
    /// <param name="graphemes">The visual-order grapheme metrics for the current line.</param>
    /// <param name="selectionStart">The first source grapheme insertion boundary applied to that line.</param>
    /// <param name="selectionEnd">The final source grapheme insertion boundary applied to that line.</param>
    /// <returns>The number of selected visual runs.</returns>
    private static int CountSelectionBoundsLine(
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int selectionStart,
        int selectionEnd)
    {
        int count = 0;
        bool hasSelection = false;
        for (int i = 0; i < graphemes.Length; i++)
        {
            GraphemeMetrics grapheme = graphemes[i];

            // Selections are caret boundary ranges: [start, end). A grapheme is selected
            // when its source start sits inside that boundary span.
            int graphemeStart = grapheme.GraphemeIndex;
            bool isSelected = graphemeStart >= selectionStart && graphemeStart < selectionEnd;

            if (!isSelected)
            {
                hasSelection = false;
                continue;
            }

            if (!hasSelection)
            {
                count++;
                hasSelection = true;
            }
        }

        return count;
    }

    /// <summary>
    /// Finds the grapheme whose advance contains the primary coordinate, or the nearest edge grapheme.
    /// </summary>
    /// <param name="graphemes">The visual-order grapheme metrics searched for a hit target.</param>
    /// <param name="primary">The coordinate along the primary layout axis.</param>
    /// <param name="isHorizontal">Indicates whether the primary axis is horizontal.</param>
    /// <returns>The nearest grapheme metrics index within <paramref name="graphemes"/>.</returns>
    private static int FindNearestGrapheme(ReadOnlySpan<GraphemeMetrics> graphemes, float primary, bool isHorizontal)
    {
        for (int i = 0; i < graphemes.Length; i++)
        {
            FontRectangle advance = graphemes[i].Advance;
            float start = isHorizontal ? advance.Left : advance.Top;
            float end = isHorizontal ? advance.Right : advance.Bottom;
            if (primary >= start && primary < end)
            {
                return i;
            }
        }

        FontRectangle first = graphemes[0].Advance;
        float firstStart = isHorizontal ? first.Left : first.Top;
        return primary < firstStart ? 0 : graphemes.Length - 1;
    }

    /// <summary>
    /// Finds the metrics entry for a source grapheme index within one visual line.
    /// </summary>
    /// <param name="graphemes">The visual-order grapheme metrics belonging to one line.</param>
    /// <param name="graphemeIndex">The logical grapheme index to look up directly.</param>
    /// <returns>The grapheme metrics index, or <c>-1</c> when the grapheme is not in the line.</returns>
    private static int FindGraphemeBySourceIndex(ReadOnlySpan<GraphemeMetrics> graphemes, int graphemeIndex)
    {
        for (int i = 0; i < graphemes.Length; i++)
        {
            if (graphemes[i].GraphemeIndex == graphemeIndex)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the nearest metrics entry for a source grapheme index within one visual line.
    /// </summary>
    /// <param name="graphemes">The visual-order grapheme metrics used for nearest-index matching.</param>
    /// <param name="graphemeIndex">The logical grapheme index whose closest visual entry is needed.</param>
    /// <returns>The nearest grapheme metrics index within <paramref name="graphemes"/>.</returns>
    private static int FindNearestGraphemeIndex(ReadOnlySpan<GraphemeMetrics> graphemes, int graphemeIndex)
    {
        int nearest = 0;
        int distance = Math.Abs(graphemes[0].GraphemeIndex - graphemeIndex);
        for (int i = 1; i < graphemes.Length; i++)
        {
            int currentDistance = Math.Abs(graphemes[i].GraphemeIndex - graphemeIndex);
            if (currentDistance < distance)
            {
                nearest = i;
                distance = currentDistance;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Gets a value indicating whether the grapheme advances right-to-left in source order.
    /// </summary>
    /// <param name="grapheme">The grapheme whose resolved bidi level is inspected.</param>
    /// <returns><see langword="true"/> when the resolved bidi level is odd.</returns>
    private static bool IsRightToLeft(in GraphemeMetrics grapheme)
        => (grapheme.BidiLevel & 1) != 0;

    /// <summary>
    /// Gets the offset of a line's graphemes within the flattened metrics array.
    /// </summary>
    /// <param name="line">The line whose stored grapheme offset identifies the desired slice.</param>
    /// <returns>The flattened grapheme metrics offset.</returns>
    private static int GetGraphemeOffset(in LineMetrics line)
        => line.GraphemeOffset;
}
