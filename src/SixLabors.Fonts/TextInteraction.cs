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
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="graphemes">The flattened grapheme metrics in visual line order.</param>
    /// <param name="point">The point to test in pixel units.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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
        int graphemeOffset = GetGraphemeOffset(graphemes, lines[lineIndex]);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, lines[lineIndex].GraphemeCount);

        return HitTestLine(lineIndex, lineGraphemes, point, isHorizontal);
    }

    /// <summary>
    /// Hit tests a point against one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="point">The point to test in pixel units.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="graphemes">The flattened grapheme metrics in visual line order.</param>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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
        int graphemeOffset = GetGraphemeOffset(graphemes, line);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);

        return GetCaretPositionLine(lineIndex, line, lineGraphemes, graphemeIndex, layoutMode);
    }

    /// <summary>
    /// Gets a caret position from one laid-out line.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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
    /// Moves a caret within a complete laid-out text box.
    /// </summary>
    /// <param name="lines">The laid-out line metrics.</param>
    /// <param name="graphemes">The flattened grapheme metrics in visual line order.</param>
    /// <param name="caret">The current caret position.</param>
    /// <param name="movement">The movement operation.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public static CaretPosition MoveCaret(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPosition caret,
        CaretMovement movement,
        LayoutMode layoutMode)
    {
        if (lines.IsEmpty || graphemes.IsEmpty)
        {
            return caret;
        }

        bool isHorizontal = layoutMode.IsHorizontal();
        int lineIndex = GetCaretLineIndex(lines, caret);
        LineMetrics line = lines[lineIndex];
        int graphemeOffset = GetGraphemeOffset(graphemes, line);
        ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);
        int target = caret.GraphemeIndex;
        switch (movement)
        {
            case CaretMovement.Previous:
                target = Math.Max(GetTextStart(lines), caret.GraphemeIndex - 1);
                break;

            case CaretMovement.Next:
                target = Math.Min(GetTextEnd(lines), caret.GraphemeIndex + 1);
                break;

            case CaretMovement.LineStart:
                target = line.GraphemeIndex;
                break;

            case CaretMovement.LineEnd:
                target = GetLineEndInsertionIndex(line, lineGraphemes);
                break;

            case CaretMovement.TextStart:
                target = GetTextStart(lines);
                break;

            case CaretMovement.TextEnd:
                target = GetTextEnd(lines);
                break;

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
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="caret">The current caret position.</param>
    /// <param name="movement">The movement operation.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
    /// <returns>The moved caret position in pixel units.</returns>
    public static CaretPosition MoveCaretLine(
        int lineIndex,
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        CaretPosition caret,
        CaretMovement movement,
        LayoutMode layoutMode)
    {
        if (graphemes.IsEmpty)
        {
            return caret;
        }

        int target = caret.GraphemeIndex;
        switch (movement)
        {
            case CaretMovement.Previous:
                target = Math.Max(line.GraphemeIndex, caret.GraphemeIndex - 1);
                break;

            case CaretMovement.Next:
                target = Math.Min(GetLineEndInsertionIndex(line, graphemes), caret.GraphemeIndex + 1);
                break;

            case CaretMovement.LineStart:
            case CaretMovement.TextStart:
                target = line.GraphemeIndex;
                break;

            case CaretMovement.LineEnd:
            case CaretMovement.TextEnd:
                target = GetLineEndInsertionIndex(line, graphemes);
                break;

            case CaretMovement.LineUp:
            case CaretMovement.LineDown:
                return caret;
        }

        return GetCaretPositionLine(lineIndex, line, graphemes, target, layoutMode);
    }

    /// <summary>
    /// Gets selection rectangles from a complete laid-out text box.
    /// </summary>
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="graphemes">The flattened grapheme metrics in visual line order.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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

        int start = Math.Min(graphemeStart, graphemeEnd);
        int end = Math.Max(graphemeStart, graphemeEnd);
        FontRectangle[] result = new FontRectangle[CountSelectionBounds(lines, graphemes, start, end)];
        int count = 0;
        bool isHorizontal = layoutMode.IsHorizontal();

        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i];
            if (!LineIntersectsSelection(line, start, end))
            {
                continue;
            }

            int graphemeOffset = GetGraphemeOffset(graphemes, line);
            ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);
            count += FillSelectionBoundsLine(line, lineGraphemes, start, end, isHorizontal, result.AsSpan(count));
        }

        return result;
    }

    /// <summary>
    /// Gets selection rectangles for one laid-out line.
    /// </summary>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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

        int start = Math.Min(graphemeStart, graphemeEnd);
        int end = Math.Max(graphemeStart, graphemeEnd);
        if (!LineIntersectsSelection(line, start, end))
        {
            return ReadOnlyMemory<FontRectangle>.Empty;
        }

        FontRectangle[] result = new FontRectangle[CountSelectionBoundsLine(graphemes, start, end)];
        FillSelectionBoundsLine(line, graphemes, start, end, layoutMode.IsHorizontal(), result);
        return result;
    }

    /// <summary>
    /// Finds the visual line nearest to a point.
    /// </summary>
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="point">The point to test in pixel units.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
    /// <returns>The nearest owning line index.</returns>
    private static int FindLineByGraphemeIndex(ReadOnlySpan<LineMetrics> lines, int graphemeIndex)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            int start = lines[i].GraphemeIndex;
            int end = start + lines[i].GraphemeCount;
            if (graphemeIndex >= start && graphemeIndex < end)
            {
                return i;
            }
        }

        return graphemeIndex < lines[0].GraphemeIndex ? 0 : lines.Length - 1;
    }

    /// <summary>
    /// Hit tests a point against one laid-out line after the layout mode has been normalized.
    /// </summary>
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="point">The point to test in pixel units.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeIndex">The grapheme insertion index in the original text.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="line">The line metrics.</param>
    /// <param name="grapheme">The grapheme metrics.</param>
    /// <param name="trailing">Whether to use the logical trailing edge.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <param name="start">The caret start point.</param>
    /// <param name="end">The caret end point.</param>
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
            float x = useEnd ? advance.Right : advance.Left;
            start = new Vector2(x, line.Start.Y);
            end = new Vector2(x, line.Start.Y + line.Extent.Y);
            return;
        }

        float y = useEnd ? advance.Bottom : advance.Top;
        start = new Vector2(line.Start.X, y);
        end = new Vector2(line.Start.X + line.Extent.X, y);
    }

    /// <summary>
    /// Moves the caret to the nearest matching position on an adjacent visual line.
    /// </summary>
    /// <param name="lines">The laid-out line metrics.</param>
    /// <param name="graphemes">The flattened grapheme metrics.</param>
    /// <param name="caret">The current caret position.</param>
    /// <param name="lineIndex">The current line index.</param>
    /// <param name="lineDown">Whether to move toward the next visual line.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <param name="layoutMode">The layout mode used when the metrics were produced.</param>
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
        int graphemeOffset = GetGraphemeOffset(graphemes, targetLine);
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
    /// <param name="lineIndex">The zero-based line index.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="point">The point to test in pixel units.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="primary">The primary-axis coordinate in pixel units.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
            if (!ContributesToCaretNavigation(graphemes[i]))
            {
                continue;
            }

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
    /// <param name="lines">The laid-out line metrics.</param>
    /// <param name="lineIndex">The current line index.</param>
    /// <param name="lineDown">Whether to move toward the next visual line.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="lines">The laid-out line metrics.</param>
    /// <param name="caret">The current caret position.</param>
    /// <returns>The line index.</returns>
    private static int GetCaretLineIndex(ReadOnlySpan<LineMetrics> lines, in CaretPosition caret)
    {
        if ((uint)caret.LineIndex < (uint)lines.Length)
        {
            return caret.LineIndex;
        }

        return FindLineByGraphemeIndex(lines, caret.GraphemeIndex);
    }

    /// <summary>
    /// Gets the first grapheme insertion index in the laid-out text.
    /// </summary>
    /// <param name="lines">The laid-out line metrics.</param>
    /// <returns>The text start insertion index.</returns>
    private static int GetTextStart(ReadOnlySpan<LineMetrics> lines)
    {
        int start = lines[0].GraphemeIndex;
        for (int i = 1; i < lines.Length; i++)
        {
            start = Math.Min(start, lines[i].GraphemeIndex);
        }

        return start;
    }

    /// <summary>
    /// Gets the final grapheme insertion index in the laid-out text.
    /// </summary>
    /// <param name="lines">The laid-out line metrics.</param>
    /// <returns>The text end insertion index.</returns>
    private static int GetTextEnd(ReadOnlySpan<LineMetrics> lines)
    {
        int end = lines[0].GraphemeIndex + lines[0].GraphemeCount;
        for (int i = 1; i < lines.Length; i++)
        {
            end = Math.Max(end, lines[i].GraphemeIndex + lines[i].GraphemeCount);
        }

        return end;
    }

    /// <summary>
    /// Gets the line end insertion index for caret navigation.
    /// </summary>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <returns>The line end insertion index.</returns>
    private static int GetLineEndInsertionIndex(in LineMetrics line, ReadOnlySpan<GraphemeMetrics> graphemes)
    {
        int end = line.GraphemeIndex + line.GraphemeCount;
        int finalGraphemeIndex = end - 1;
        for (int i = 0; i < graphemes.Length; i++)
        {
            GraphemeMetrics grapheme = graphemes[i];
            if (grapheme.GraphemeIndex == finalGraphemeIndex
                && !ContributesToCaretNavigation(grapheme))
            {
                return finalGraphemeIndex;
            }
        }

        return end;
    }

    /// <summary>
    /// Gets the cross-axis start of a line.
    /// </summary>
    /// <param name="line">The line metrics.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <returns>The cross-axis start.</returns>
    private static float GetLineCrossStart(in LineMetrics line, bool isHorizontal)
        => isHorizontal ? line.Start.Y : line.Start.X;

    /// <summary>
    /// Gets the cross-axis end of a line.
    /// </summary>
    /// <param name="line">The line metrics.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <returns>The cross-axis end.</returns>
    private static float GetLineCrossEnd(in LineMetrics line, bool isHorizontal)
        => isHorizontal ? line.Start.Y + line.Extent.Y : line.Start.X + line.Extent.X;

    /// <summary>
    /// Gets the coordinate to preserve for repeated visual line movement.
    /// </summary>
    /// <param name="start">The caret start point.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <returns>The line navigation position.</returns>
    private static float GetLineNavigationPosition(Vector2 start, bool isHorizontal)
        => isHorizontal ? start.X : start.Y;

    /// <summary>
    /// Creates a copy of the caret with a specific preserved line navigation position.
    /// </summary>
    /// <param name="caret">The caret position.</param>
    /// <param name="lineNavigationPosition">The line navigation position.</param>
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
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <param name="result">The target selection rectangles.</param>
    /// <returns>The number of selection rectangles written.</returns>
    private static int FillSelectionBoundsLine(
        in LineMetrics line,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeStart,
        int graphemeEnd,
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
            if (grapheme.GraphemeIndex < graphemeStart
                || grapheme.GraphemeIndex >= graphemeEnd
                || !ContributesToSelectionBounds(grapheme))
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
    /// <param name="line">The line metrics.</param>
    /// <param name="start">The selection start on the primary axis.</param>
    /// <param name="end">The selection end on the primary axis.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
    /// <returns>The selection rectangle in pixel units.</returns>
    private static FontRectangle CreateSelectionBounds(
        in LineMetrics line,
        float start,
        float end,
        bool isHorizontal)
    {
        return isHorizontal
            ? FontRectangle.FromLTRB(start, line.Start.Y, end, line.Start.Y + line.Extent.Y)
            : FontRectangle.FromLTRB(line.Start.X, start, line.Start.X + line.Extent.X, end);
    }

    /// <summary>
    /// Counts how many selection rectangles are required for a grapheme range.
    /// </summary>
    /// <param name="lines">The laid-out line metrics in visual line order.</param>
    /// <param name="graphemes">The flattened grapheme metrics in visual line order.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <returns>The number of selection rectangles.</returns>
    private static int CountSelectionBounds(
        ReadOnlySpan<LineMetrics> lines,
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeStart,
        int graphemeEnd)
    {
        int count = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            LineMetrics line = lines[i];
            if (!LineIntersectsSelection(line, graphemeStart, graphemeEnd))
            {
                continue;
            }

            int graphemeOffset = GetGraphemeOffset(graphemes, line);
            ReadOnlySpan<GraphemeMetrics> lineGraphemes = graphemes.Slice(graphemeOffset, line.GraphemeCount);
            count += CountSelectionBoundsLine(lineGraphemes, graphemeStart, graphemeEnd);
        }

        return count;
    }

    /// <summary>
    /// Counts visually contiguous selected grapheme runs in one line.
    /// </summary>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <returns>The number of selected visual runs.</returns>
    private static int CountSelectionBoundsLine(
        ReadOnlySpan<GraphemeMetrics> graphemes,
        int graphemeStart,
        int graphemeEnd)
    {
        int count = 0;
        bool hasSelection = false;
        for (int i = 0; i < graphemes.Length; i++)
        {
            GraphemeMetrics grapheme = graphemes[i];
            bool isSelected = grapheme.GraphemeIndex >= graphemeStart
                && grapheme.GraphemeIndex < graphemeEnd
                && ContributesToSelectionBounds(grapheme);

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
    /// Gets a value indicating whether a line intersects the supplied grapheme range.
    /// </summary>
    /// <param name="line">The line metrics.</param>
    /// <param name="graphemeStart">The inclusive start grapheme index in the original text.</param>
    /// <param name="graphemeEnd">The exclusive end grapheme index in the original text.</param>
    /// <returns><see langword="true"/> when the line intersects the range.</returns>
    private static bool LineIntersectsSelection(in LineMetrics line, int graphemeStart, int graphemeEnd)
    {
        int lineStart = line.GraphemeIndex;
        int lineEnd = lineStart + line.GraphemeCount;
        return graphemeStart < lineEnd && graphemeEnd > lineStart;
    }

    /// <summary>
    /// Finds the grapheme whose advance contains the primary coordinate, or the nearest edge grapheme.
    /// </summary>
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="primary">The primary-axis coordinate in pixel units.</param>
    /// <param name="isHorizontal">Whether primary advance flows horizontally.</param>
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
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
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
    /// <param name="graphemes">The line's grapheme metrics in visual order.</param>
    /// <param name="graphemeIndex">The grapheme index in the original text.</param>
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
    /// <param name="grapheme">The grapheme metrics.</param>
    /// <returns><see langword="true"/> when the resolved bidi level is odd.</returns>
    private static bool IsRightToLeft(in GraphemeMetrics grapheme)
        => (grapheme.BidiLevel & 1) != 0;

    /// <summary>
    /// Gets a value indicating whether the grapheme should create visual selection bounds.
    /// </summary>
    /// <param name="grapheme">The grapheme metrics.</param>
    /// <returns><see langword="true"/> when the grapheme should contribute to selection bounds.</returns>
    private static bool ContributesToSelectionBounds(in GraphemeMetrics grapheme)
    {
        // Hard breaks remain logical graphemes for caret movement and source ranges, but
        // a non-measuring hard break should not create its own painted selection box.
        return !grapheme.IsLineBreak || grapheme.ContributesToMeasurement;
    }

    /// <summary>
    /// Gets a value indicating whether the grapheme should participate in keyboard caret navigation.
    /// </summary>
    /// <param name="grapheme">The grapheme metrics.</param>
    /// <returns><see langword="true"/> when the grapheme should be a caret navigation target.</returns>
    private static bool ContributesToCaretNavigation(in GraphemeMetrics grapheme)
    {
        // Non-measuring hard breaks still exist as source positions, but Up/Down and LineEnd
        // should target the visible line content rather than snapping after the hidden break.
        return !grapheme.IsLineBreak || grapheme.ContributesToMeasurement;
    }

    /// <summary>
    /// Gets the offset of a line's graphemes within the flattened metrics array.
    /// </summary>
    /// <param name="graphemes">The flattened grapheme metrics.</param>
    /// <param name="line">The line whose source range owns the graphemes.</param>
    /// <returns>The flattened grapheme metrics offset.</returns>
    private static int GetGraphemeOffset(ReadOnlySpan<GraphemeMetrics> graphemes, in LineMetrics line)
    {
        int lineStart = line.GraphemeIndex;
        int lineEnd = lineStart + line.GraphemeCount;
        for (int i = 0; i < graphemes.Length; i++)
        {
            int graphemeIndex = graphemes[i].GraphemeIndex;
            if (graphemeIndex >= lineStart && graphemeIndex < lineEnd)
            {
                return i;
            }
        }

        return 0;
    }
}
