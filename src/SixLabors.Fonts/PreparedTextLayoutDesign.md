# Text Measurement and Interaction APIs

This document describes the public measurement and selection surface for laid-out
text. The intent is that callers can measure, render, hit-test, place carets,
and draw selections without reimplementing bidi, grapheme, hard-break, or layout
mode rules outside the library.

All positional metrics exposed by these APIs are in pixel units.

## API Layers

There are four layers:

- `TextMeasurer`: one-shot convenience APIs for measuring a string.
- `TextBlock`: prepared text that can be measured or rendered repeatedly.
- `TextMetrics`: the full measurement result for one laid-out text block.
- `LineLayout`: one laid-out line with line-local measurement and interaction APIs.

Use `TextMeasurer` for simple one-off work. Use `TextBlock` when the same text
will be measured, rendered, wrapped, or inspected more than once.

## One-Shot Measurement

`TextMeasurer` is the shortest path from text and options to measurements.
`TextOptions.WrappingLength` controls wrapping for these methods.

```csharp
TextOptions options = new(font)
{
    Origin = new Vector2(20, 30),
    // TextMeasurer reads WrappingLength from TextOptions.
    WrappingLength = 320
};

TextMetrics metrics = TextMeasurer.Measure(text, options);
FontRectangle advance = TextMeasurer.MeasureAdvance(text, options);
FontRectangle bounds = TextMeasurer.MeasureBounds(text, options);
FontRectangle renderableBounds = TextMeasurer.MeasureRenderableBounds(text, options);
```

The aggregate rectangles answer different questions:

- `MeasureAdvance`: the logical line-box advance of the text.
- `MeasureBounds`: the rendered glyph bounds.
- `MeasureRenderableBounds`: the union of logical advance and rendered glyph bounds.

Use `MeasureAdvance` for layout flow. Use `MeasureBounds` for tight ink bounds.
Use `MeasureRenderableBounds` when both typographic advance and rendered glyph
overshoot must fit.

## Prepared Measurement

`TextBlock` prepares the wrapping-independent text work once. Pass the wrapping
length to each operation. `TextOptions.WrappingLength` is ignored by the
constructor.

```csharp
TextBlock block = new(text, options);

// Each operation supplies the wrapping length; the constructor does not.
TextMetrics narrow = block.Measure(240);
TextMetrics wide = block.Measure(480);

FontRectangle narrowBounds = block.MeasureBounds(240);
FontRectangle wideBounds = block.MeasureBounds(480);
```

Use `-1` as the wrapping length to disable wrapping.

```csharp
// -1 disables wrapping for TextBlock operations.
TextMetrics unwrapped = block.Measure(-1);
```

`TextBlock` also exposes direct detail APIs when a full `TextMetrics` object is
not needed:

```csharp
ReadOnlySpan<LineMetrics> lines = block.GetLineMetrics(320);
ReadOnlySpan<GraphemeMetrics> graphemes = block.GetGraphemeMetrics(320);
ReadOnlySpan<GlyphBounds> glyphBounds = block.MeasureGlyphBounds(320);
```

## TextMetrics

`TextMetrics` is the result to keep when callers need several measurements from
the same laid-out text.

```csharp
TextMetrics metrics = TextMeasurer.Measure(text, options);

// These aggregate measurements answer different layout and rendering questions.
FontRectangle advance = metrics.Advance;
FontRectangle bounds = metrics.Bounds;
FontRectangle renderableBounds = metrics.RenderableBounds;
int lineCount = metrics.LineCount;

ReadOnlySpan<LineMetrics> lines = metrics.LineMetrics;
ReadOnlySpan<GraphemeMetrics> graphemes = metrics.GraphemeMetrics;
```

The line and grapheme collections are in final layout order. That matters for
bidi text and reverse line-order layout modes: source order and visual order can
be different.

## Line Metrics

`LineMetrics` describes one laid-out line.

```csharp
foreach (LineMetrics line in metrics.LineMetrics)
{
    // Start and Extent describe the positioned line box.
    Vector2 start = line.Start;
    Vector2 extent = line.Extent;
    float baseline = line.Baseline;
}
```

`Start` and `Extent` describe the positioned line box in pixel units. Selection
and caret APIs use the line box for the cross-axis size, which matches normal
text editor and browser behavior: selecting mixed font sizes on the same line
paints a consistent line-height rectangle rather than one rectangle per glyph
height.

`StringIndex`, `GraphemeIndex`, and `GraphemeCount` describe the source text
range owned by the line. `GraphemeCount` is not a glyph count.

## Grapheme Metrics

Use `GraphemeMetrics` for text interaction: hit testing, caret positioning,
range selection, and UI overlays.

```csharp
foreach (GraphemeMetrics grapheme in metrics.GraphemeMetrics)
{
    // Use Advance for interaction and Bounds for rendered ink.
    FontRectangle advance = grapheme.Advance;
    FontRectangle bounds = grapheme.Bounds;
    FontRectangle renderableBounds = grapheme.RenderableBounds;
    bool isLineBreak = grapheme.IsLineBreak;
    bool contributesToMeasurement = grapheme.ContributesToMeasurement;
}
```

The rectangles answer different questions:

- `Advance`: the positioned logical advance rectangle for the grapheme.
- `Bounds`: the rendered glyph bounds for the grapheme.
- `RenderableBounds`: the union of advance and rendered glyph bounds.

Use `Advance` for hit targets, carets, and selection geometry. Ink bounds can be
empty, overhang the advance, or exclude whitespace, so they are not a reliable
interaction target.

`IsLineBreak` identifies hard-break graphemes. `ContributesToMeasurement`
identifies whether the grapheme expands text measurements and painted selection
rectangles. A grapheme can remain present for source mapping and caret positions
without contributing to those visual measurements.

## Glyph Bounds

Glyph detail APIs expose laid-out glyph entries.

```csharp
ReadOnlySpan<GlyphBounds> advances = metrics.MeasureGlyphAdvances();
ReadOnlySpan<GlyphBounds> bounds = metrics.MeasureGlyphBounds();
ReadOnlySpan<GlyphBounds> renderable = metrics.MeasureGlyphRenderableBounds();
```

Use glyph detail for rendering diagnostics, glyph-level visualization, or
advanced inspection. Do not use glyph entries as character or caret positions:
ligatures, decomposition, fallback, emoji, and combining marks mean one
grapheme can map to multiple glyph entries, and multiple source characters can
map to one visual glyph sequence.

## Per-Line Layout

`TextBlock.LayoutLines` returns line objects when callers want line-local
inspection or interaction.

```csharp
TextBlock block = new(text, options);
ReadOnlySpan<LineLayout> layout = block.LayoutLines(320);

foreach (LineLayout line in layout)
{
    // LineLayout exposes the slice of grapheme metrics owned by this line.
    LineMetrics lineMetrics = line.LineMetrics;
    ReadOnlySpan<GraphemeMetrics> lineGraphemes = line.GraphemeMetrics;
}
```

`LineLayout` mirrors the interaction and glyph-detail surface for a single line:

```csharp
TextHit hit = line.HitTest(point);
// Passing the hit keeps trailing-edge and bidi handling inside the library.
CaretPosition caret = line.GetCaretPosition(hit);
CaretPosition next = line.MoveCaret(caret, CaretMovement.Next);
ReadOnlyMemory<FontRectangle> selection = line.GetSelectionBounds(caret, next);
ReadOnlySpan<GlyphBounds> glyphs = line.MeasureGlyphRenderableBounds();
```

Use the full `TextMetrics` interaction methods for selections that can cross
line boundaries. Use `LineLayout` when the caller already knows interaction is
line-local.

## Hit Testing

Hit testing maps a point to the nearest grapheme and side.

```csharp
TextHit hit = metrics.HitTest(mousePosition);

int lineIndex = hit.LineIndex;
int graphemeIndex = hit.GraphemeIndex;
// Use this value for carets and selection endpoints.
int insertionIndex = hit.GraphemeInsertionIndex;
```

`GraphemeIndex` identifies the hit grapheme. `GraphemeInsertionIndex` identifies
the logical caret position represented by the hit. For left-to-right text, the
trailing side is usually `GraphemeIndex + 1`. For right-to-left text, the
physical side is reversed, but callers do not need to apply that rule. Use
`GraphemeInsertionIndex` or pass the `TextHit` directly to caret and selection
APIs.

## Caret Positioning

Caret APIs return positioned caret lines in pixel units. A caret is also the
navigation token for keyboard/editor interaction.

```csharp
TextHit hit = metrics.HitTest(mousePosition);
// The hit overload applies the correct grapheme insertion index.
CaretPosition caret = metrics.GetCaretPosition(hit);

DrawCaret(caret.Start, caret.End);

if (caret.HasSecondary)
{
    DrawSecondaryCaret(caret.SecondaryStart, caret.SecondaryEnd);
}
```

The integer overload is for callers that already have a logical insertion index:

```csharp
// Use this when the caller already owns a logical insertion index.
CaretPosition caret = metrics.GetCaretPosition(graphemeInsertionIndex);
```

At bidi boundaries, one logical insertion position can have two visual edges.
`CaretPosition` exposes the secondary edge so editor-style callers can choose how
to present or navigate that boundary without recomputing bidi affinity.

## Caret Movement

`MoveCaret` applies editor-style movement to a caret and returns the new caret.

```csharp
CaretPosition caret = metrics.GetCaretPosition(graphemeInsertionIndex);

// Previous and Next move through logical grapheme insertion positions.
caret = metrics.MoveCaret(caret, CaretMovement.Next);

// LineStart and LineEnd are the Home/End-style line movement operations.
caret = metrics.MoveCaret(caret, CaretMovement.LineEnd);

// TextStart and TextEnd are the whole-block equivalents.
caret = metrics.MoveCaret(caret, CaretMovement.TextStart);
```

`LineUp` and `LineDown` move to adjacent visual lines while preserving the
caret's requested position on the line.

```csharp
CaretPosition firstLineEnd = metrics.GetCaretPosition(firstLineEndIndex);

// Repeated LineDown keeps the original line position even when an intermediate
// line is shorter and the visible caret has to clamp to that line's end.
CaretPosition middleLine = metrics.MoveCaret(firstLineEnd, CaretMovement.LineDown);
CaretPosition finalLine = metrics.MoveCaret(middleLine, CaretMovement.LineDown);
```

This preserves normal rich-text editor behavior: moving down through a short line
does not permanently lose the user's original horizontal or vertical line
position.

## Selection Bounds

Selection APIs return rectangles in visual order and pixel units. The result is
`ReadOnlyMemory<FontRectangle>` so callers can store it with selection state and
use `.Span` when drawing.

```csharp
ReadOnlyMemory<FontRectangle> selection = metrics.GetSelectionBounds(start, end);

foreach (FontRectangle rectangle in selection.Span)
{
    // Draw every rectangle; bidi selections can be visually discontinuous.
    FillSelectionRectangle(rectangle);
}
```

The integer overload accepts logical grapheme insertion indices. It is useful
when the caller already owns a source range.

For pointer selection, use the hit overload. This keeps bidi and trailing-edge
logic inside the library.

```csharp
TextHit anchor = metrics.HitTest(mouseDown);
TextHit focus = metrics.HitTest(mouseMove);

// The hit overload converts both endpoints to logical insertion indices.
ReadOnlyMemory<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus);

foreach (FontRectangle rectangle in selection.Span)
{
    FillSelectionRectangle(rectangle);
}
```

For keyboard selection, keep an anchor caret and move the focus caret.

```csharp
CaretPosition anchor = metrics.GetCaretPosition(selectionStart);
CaretPosition focus = anchor;

// Shift+Right-style behavior updates only the focus caret.
focus = metrics.MoveCaret(focus, CaretMovement.Next);

ReadOnlyMemory<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus);
```

Do not sort, union, or merge the returned rectangles unless the UI explicitly
wants a different visual. A single logical selection can be visually
discontinuous inside one line when it crosses bidi runs. Returning multiple
rectangles allows browser-style selection where the unselected visual gap stays
unpainted.

## Bidi Drag Selection

Consider a line whose source text is:

```text
Tall שלום عرب
```

In a left-to-right paragraph, the right-to-left run can paint with Arabic before
Hebrew. When a user drags from the left edge of `Tall` toward the Hebrew word,
the selection can become visually split:

```text
[Tall ] عرب [שלום]
```

Application code should not manually decide which physical edge of the Hebrew
glyph means "before" or "after". The correct flow is:

```csharp
TextHit anchor = metrics.HitTest(mouseDown);
TextHit focus = metrics.HitTest(mouseMove);
// Bidi split selection is represented by the returned rectangle list.
ReadOnlyMemory<FontRectangle> rectangles = metrics.GetSelectionBounds(anchor, focus);
```

The hit-test result carries the logical insertion index. The selection result is
already split into the visual rectangles that should be painted.

## Hard Line Breaks

Hard line breaks remain graphemes for source ranges, hit testing, and caret
movement. Selection painting only creates a rectangle when the hard break owns
measurable line space.

For text with two hard breaks in the middle:

```text
Tall عرب שלום

Small مرحبا שלום
```

Full selection should paint three visual rows: the first text line, the blank
line, and the second text line. The line break that ends a non-empty line should
not add a separate painted box; the line break that owns the blank line should.

Consumers should not special-case this. Draw the rectangles returned by
`GetSelectionBounds`. Consumers that inspect individual graphemes can use
`IsLineBreak` and `ContributesToMeasurement` to understand why a line-break
grapheme is present but does not paint a selection rectangle.

## Recommended Workflows

For one-off measuring:

```csharp
// One-shot path for a single layout result.
TextMetrics metrics = TextMeasurer.Measure(text, options);
```

For repeated wrapping or rendering:

```csharp
TextBlock block = new(text, options);

// Reuse the prepared text for each requested wrapping length.
TextMetrics narrow = block.Measure(240);
TextMetrics wide = block.Measure(480);
block.RenderTo(renderer, 480);
```

For text editor interaction:

```csharp
TextMetrics metrics = block.Measure(wrappingLength);

TextHit anchor = metrics.HitTest(mouseDown);
TextHit focus = metrics.HitTest(mouseMove);
// Use hit-based overloads so interaction follows the laid-out bidi result.
CaretPosition caret = metrics.GetCaretPosition(focus);
ReadOnlyMemory<FontRectangle> selection = metrics.GetSelectionBounds(anchor, focus);
```

For keyboard navigation and selection:

```csharp
TextMetrics metrics = block.Measure(wrappingLength);
CaretPosition caret = metrics.GetCaretPosition(currentInsertionIndex);
CaretPosition anchor = caret;

// The movement operation owns grapheme, line, and hard-break navigation rules.
caret = metrics.MoveCaret(caret, CaretMovement.LineDown);
ReadOnlyMemory<FontRectangle> selection = metrics.GetSelectionBounds(anchor, caret);
```

For per-line UI:

```csharp
ReadOnlySpan<LineLayout> lines = block.LayoutLines(wrappingLength);

foreach (LineLayout line in lines)
{
    ReadOnlySpan<GraphemeMetrics> graphemes = line.GraphemeMetrics;
    ReadOnlySpan<GlyphBounds> glyphs = line.MeasureGlyphBounds();
}
```

## Design Principles

- The library owns bidi, grapheme, hard-break, wrapping, and layout-mode rules.
- Callers should pass points, hits, or logical ranges and draw the returned geometry.
- Caret movement should flow through `MoveCaret`, not caller-side grapheme arithmetic.
- Grapheme metrics are the text interaction unit.
- Glyph metrics are rendering-detail data, not caret or character data.
- Selection rectangles are visual geometry, not a single logical union.
- Per-line selection uses line-box height so selection remains visually stable
  across mixed fonts and font sizes.
