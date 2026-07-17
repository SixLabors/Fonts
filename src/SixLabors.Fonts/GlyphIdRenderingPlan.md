# Rendering glyphs by glyph id

## Problem

Shaped text pipelines (Avalonia's `IGlyphRunImpl`, HarfBuzz output, PDF/SVG glyph runs)
hand us **glyph ids + positions**, not characters. SixLabors.Fonts currently has **no public
way to turn a glyph id into something renderable** through `IGlyphRenderer`.

Every public rendering entry point is text-driven:

```
TextRenderer.Render(text, options)
  -> new TextBlock(text, options)        // shaping + layout from characters
  -> block.RenderTo(renderer)            // walks laid-out glyphs, calls FontGlyphMetrics.RenderTo
```

See `Rendering/TextRenderer.cs:50`. There is no equivalent that starts from a glyph id.

Consequence in the ImageSharp.Drawing Avalonia backend: `ImageSharpDrawingContext.DrawGlyphRun`
bails whenever the run carries only glyph ids (`Text is null`), which is the common Avalonia
path — so most real text never renders.

## What already exists (and works per-glyph)

- **`IGlyphRenderer`** (`Rendering/IGlyphRenderer.cs`) is already glyph-agnostic: `BeginGlyph` /
  figures (`MoveTo`/`LineTo`/`QuadraticBezierTo`/`CubicBezierTo`/`ArcTo`) / `EndGlyph`, plus
  `BeginLayer`/`EndLayer`/`Paint` for colour glyphs. Nothing about it requires text.

- **`Glyph`** (`Glyph.cs`) — public readonly struct wrapping a `FontGlyphMetrics` + point size.
  Has public `BoundingBox(...)` but its constructor and `RenderTo(...)` are **internal**.

- **`FontGlyphMetrics`** (`FontGlyphMetrics.cs`) — public abstract base. Public surface already
  includes `GlyphId`, `CodePoint`, advances, bounds, `ScaleFactor`, `UnitsPerEm`. The actual
  render is `internal abstract RenderTo(IGlyphRenderer, int graphemeIndex, Vector2 glyphOrigin,
  Vector2 decorationOrigin, GlyphLayoutMode, TextOptions)`, plus `CloneForRendering(TextRun)`.
  Concrete impls: `TrueTypeGlyphMetrics`, `CffGlyphMetrics`, `PaintedGlyphMetrics`.

- **`FontMetrics`** (`FontMetrics.cs`) — the id plumbing is present but internal:
  - `internal abstract bool TryGetGlyphId(CodePoint, out ushort glyphId)` (`:125`)
  - `internal abstract bool TryGetCodePoint(ushort glyphId, out CodePoint)` (`:156`) — reverse map
  - `public abstract bool TryGetGlyphMetrics(CodePoint, ...)` (`:210`) — **codepoint-keyed only**
  - `internal abstract FontGlyphMetrics GetGlyphMetrics(CodePoint, ushort glyphId, ...)` (`:237`)
    — id-aware, but internal and still requires a codepoint.

- **`TrueTypeGlyphMetrics.RenderTo`** (`Tables/TrueType/TrueTypeGlyphMetrics.cs:135`) is the
  reference implementation. It already renders purely from glyph outline data; its only couplings
  to "text" are:
  - `pointSize = this.TextRun.Font?.Size ?? options.Font.Size` — needs a `TextRun` **or** `TextOptions`.
  - `dpi`, `HintingMode` from `TextOptions`.
  - `this.CodePoint` used for `ShouldSkipGlyphRendering` / whitespace short-circuit.
  - Builds `GlyphRendererParameters(this, this.TextRun, pointSize, dpi, mode, graphemeIndex)`.

- **`GlyphRendererParameters`** (`Rendering/GlyphRendererParameters.cs`) — passed to `BeginGlyph`.
  Public fields include `GlyphId`, `CodePoint`, `TextRun`, `PointSize`, `Dpi`, `LayoutMode`,
  `GraphemeIndex`. Used as a cache key by renderers, so synthesized values must be stable.

## The gap, precisely

1. **No public id→metrics accessor.** `GetGlyphMetrics(codePoint, glyphId, …)` is internal and
   demands a codepoint a caller with only an id doesn't have.
2. **No public render entry.** `Glyph.RenderTo` / `FontGlyphMetrics.RenderTo` are internal.
3. **`RenderTo` is coupled to `TextRun` + `TextOptions`.** A bare glyph id has neither.
4. **`CodePoint` coupling.** `RenderTo` uses `this.CodePoint` for skip/whitespace checks; an id may
   resolve to no codepoint. Recoverable via `TryGetCodePoint`, or treated as "render the outline".
5. **`CloneForRendering(TextRun)`.** Render-time metrics are clones bound to a text run; the id path
   needs a synthesized default run.

## Proposed design

Recommended: **(A) public id accessors**, **(B) an internal refactor decoupling the per-glyph render
from `TextOptions`/`TextRun`**, and **(C) one id-based public render entry** that builds the run via
`GlyphOptions.CreateTextRun`. Keep `Glyph`/`FontGlyphMetrics` lean; centralize the `TextRun`/options
synthesis in one place. There is no run-level helper (see Section C).

### A. Public id accessors

- `FontMetrics`:
  ```csharp
  public abstract bool TryGetGlyphMetrics(
      ushort glyphId,
      TextAttributes textAttributes,
      TextDecorations textDecorations,
      LayoutMode layoutMode,
      ColorFontSupport support,
      [NotNullWhen(true)] out FontGlyphMetrics? metrics);
  ```
  Default impl: recover a codepoint via the existing `TryGetCodePoint(glyphId, …)` (fall back to
  `default`/`CodePoint(0)` when none), then delegate to the existing internal
  `GetGlyphMetrics(codePoint, glyphId, …)`. Consider promoting `TryGetCodePoint` to public — useful
  for consumers doing hit-testing / fallback.

  This stays available for callers that want **metrics/bounds without rendering** (measurement,
  hit-testing). It is **not** on the render path — see below.

- `Font.TryGetGlyph(ushort glyphId, …, out Glyph)` is **not required**. The render entry point takes
  the id directly (Section C), so there is no need to expose a pre-fetched `Glyph` for rendering, nor
  to make `Glyph.RenderTo` public. A `TryGetGlyph` may still be added later purely as a convenience
  for measurement, but it is out of scope for enabling glyph-id rendering.

### B. Decouple `RenderTo` from `TextRun`/`TextOptions` (internal refactor)

Extract the body of `FontGlyphMetrics.RenderTo` so it takes explicit primitives
(`pointSize`, `dpi`, `HintingMode`, `GlyphLayoutMode`, `TextRun`) rather than the
layout-oriented `TextOptions`. The existing text path passes values from its `TextOptions`; the id
path passes values read off `GlyphOptions`. This is a real refactor across the three concrete metrics
(`TrueType`/`Cff`/`Painted`) and the text-path caller (`TextBlock.Visitors.cs:805`), so it is gated
behind tests in Phase 1.

**Why re-plumb rather than synthesize a `TextOptions`?** Performance. The caller pools/mutates a
single `GlyphOptions` across the whole run, so manufacturing a fresh `TextOptions` per glyph would
allocate in the hot loop and defeat that pooling. Passing primitives extracted from the pooled
options keeps the per-glyph path allocation-light; the only unavoidable per-glyph allocation is the
`TextRun` from `CreateTextRun`, and the rich case needs a per-glyph `RichTextRun` regardless.

**Codepoint is best-effort on the id path.** The metrics' `CodePoint` is recovered via
`TryGetCodePoint(glyphId, …)`, which is a lossy reverse lookup: ligatures and GSUB-substituted glyphs
— precisely what shaped runs produce — frequently map to *no* codepoint, yielding `default`. This is
acceptable because the **outline is keyed by glyph id (`vector`), not codepoint** — it renders
correctly regardless. `CodePoint` only drives the skip/whitespace short-circuits (skipped when
unknown, so we simply emit the outline) and the informational `GlyphRendererParameters.CodePoint`.
Document this as an explicit, accepted limitation rather than a bug.

Introduce a minimal render-options type so callers don't have to build a full `TextOptions`:
```csharp
public class GlyphOptions   // deliberately unsealed (see below); mirrors TextOptions
{
    public required Font Font { get; set; }
    public float Dpi { get; set; } = 72;
    public HintingMode HintingMode { get; set; }
    public LayoutMode LayoutMode { get; set; } = LayoutMode.HorizontalTopBottom;
    public ColorFontSupport ColorFontSupport { get; set; } = ColorFontSupport.None;

    // The rendering origin, mirroring TextOptions.Origin. Per-glyph, like the rest of GlyphOptions:
    // the caller advances it per glyph rather than passing a separate origin argument.
    public Vector2 Origin { get; set; } = Vector2.Zero;

    // The grapheme this glyph belongs to. Per-glyph state (like Origin), so it lives on the options
    // rather than as a Render argument. Defaults to 0; shaping sources that know the cluster
    // (e.g. Avalonia's GlyphInfo.GlyphCluster) set it so grapheme-aware renderers can group correctly.
    public int GraphemeIndex { get; set; }

    // Required, not optional: attributes drive metric selection (subscript/superscript scaling in
    // FontGlyphMetrics' ctor) and both feed the synthesized TextRun and TryGetGlyphMetrics(id, ...).
    public TextAttributes TextAttributes { get; set; } = TextAttributes.None;
    public TextDecorations TextDecorations { get; set; } = TextDecorations.None;

    protected internal virtual TextRun CreateTextRun()
        => new()
        {
            Start = this.GraphemeIndex,
            End = this.GraphemeIndex + 1,
            Font = this.Font,
            TextAttributes = this.TextAttributes,
            TextDecorations = this.TextDecorations
        };
}
```

**The class must stay unsealed.** Downstream renderers need to derive richer option types — e.g.
ImageSharp.Drawing's `RichGlyphOptions : GlyphOptions` adding per-glyph `Pen`/`Brush`
so individual glyphs can be filled/stroked differently. The base render path reads the common members
above, then calls `CreateTextRun` and passes that run through `GlyphRendererParameters.TextRun`.
Drawing can override `CreateTextRun` to return a `RichTextRun`, which preserves the existing
`RichTextGlyphRenderer` extension point without adding Drawing-specific API to Fonts. Keep the base
type's members non-`init`-only where a caller or subclass needs to layer on per-glyph state, and avoid
`sealed`/`record` forms that would block inheritance.

`TextAttributes`/`TextDecorations` are not cosmetic extras here: `TryGetGlyphMetrics(glyphId,
textAttributes, textDecorations, …)` takes them directly, the `FontGlyphMetrics` constructor uses
`TextAttributes` to compute the subscript/superscript scale factor and offset, and the synthesized
single-glyph `TextRun` must carry both so `RenderTo`/`RenderDecorationsTo` behave like the text path.

### C. Public render entry — id-based

`TextRenderer` accepts the **glyph id**, not a pre-fetched `Glyph`, and owns everything internally:
fetch metrics (with the options' attributes) → clone for rendering → synthesize a single-glyph
`TextRun` → drive the refactored render core. The caller never touches `FontGlyphMetrics`/`Glyph`,
and a stale/mismatched `Glyph` is structurally impossible. The render position comes from
`GlyphOptions.Origin` (mirroring `TextOptions.Origin`), not a separate argument.
```csharp
// Single glyph, positioned via GlyphOptions.Origin.
public static void Render(
    IGlyphRenderer renderer, ushort glyphId, GlyphOptions options);
```
`Render` uses `options.CreateTextRun()` for the render clone, so derived options can carry
per-glyph state into the renderer via a derived `TextRun`.

**Grapheme index — must carry the real cluster.** The index is not a positional argument; it lives
on `GlyphOptions.GraphemeIndex` and flows into the synthesized `TextRun` and
`GlyphRendererParameters.GraphemeIndex`. It is **load-bearing**: `BaseGlyphBuilder.BeginGlyph`
(`ImageSharp.Drawing/Text/BaseGlyphBuilder.cs:135`), the base class of `RichTextGlyphRenderer`,
accumulates each glyph's paths into a per-grapheme `GlyphPathCollection` and flushes it whenever
`parameters.GraphemeIndex` changes. A constant `0` would merge the **entire run into a single
grapheme group**, destroying those boundaries (per-grapheme path grouping, layer/brush application).
Therefore the caller must feed the shaping cluster: the Avalonia backend sets
`GlyphOptions.GraphemeIndex = GlyphInfo.GlyphCluster` per glyph. It defaults to `0` only for callers
that genuinely render one glyph in isolation.

**No run overload.** `GlyphOptions` is per-glyph state — the per-glyph `Pen`/`Brush` on
`RichGlyphOptions` is exactly what varies across a run — so any run-level helper taking one options
object would contradict the feature, and one taking per-glyph options (`ReadOnlySpan<GlyphOptions>`
etc.) saves nothing over the caller's own loop. The caller loops `Render`, passing a fresh
(often pooled/mutated) `GlyphOptions` per glyph, and brackets the loop with its own
`BeginText`/`EndText`.
It should **not** synthesize decorations by default (run-level consumers position
underline/strikeout themselves); `GlyphOptions` can carry an opt-in. `BeginText`/`EndText` are the
caller's responsibility so a run can batch many glyphs in one text scope.

## Colour / painted glyphs

COLR/CPAL glyphs flow through `CffGlyphMetrics` / `PaintedGlyphMetrics` / `IPaintedGlyphSource`
(`Rendering/PaintedGlyph*.cs`) and the `BeginLayer`/`Paint`/`EndLayer` parts of `IGlyphRenderer`.
These metrics are already keyed by glyph id, so `GetGlyphMetrics(…, colorSupport: …)` returns the
painted variant for an id. The id path must pass `ColorFontSupport` through unchanged and be tested
against a COLR font so painted runs render by id, not just monochrome outlines.

## Consumer integration (ImageSharp.Drawing Avalonia backend)

The data is already in hand — `ImageSharpGlyphRun` (`samples/AvaloniaControlCatalog/ImageSharpGlyphRun.cs`)
exposes `GlyphTypeface`, `FontRenderingEmSize`, `BaselineOrigin`, and `GlyphInfos`, where each
`Avalonia.Media.TextFormatting.GlyphInfo` is `(ushort GlyphIndex, int GlyphCluster, double GlyphAdvance,
Vector GlyphOffset)`.

`DrawGlyphRun` becomes:
1. Map `GlyphTypeface` → SixLabors `Font` (the typeface impl already wraps one).
2. Walk `GlyphInfos`, per glyph setting `options.Origin = BaselineOrigin + (penX, 0) + GlyphOffset`
   (advancing `penX += GlyphAdvance`) and `options.GraphemeIndex = GlyphCluster` so the renderer's
   per-grapheme grouping stays correct. Set `options.LayoutMode` from the run's text layout direction.
3. For each, `TextRenderer.Render(richTextGlyphRenderer, GlyphIndex, options)` —
   a `RichGlyphOptions` carrying this glyph's `Origin`, `GraphemeIndex`, and the run's `Pen`/`Brush` —
   into the existing `IGlyphRenderer` the backend already feeds to `DrawingCanvas`. No intermediate
   `Glyph` is fetched.
4. Delete the `Text is null` gate, rendering shaped/id-only runs.

## Risks / open questions

- **`TextOptions` is heavy and layout-centric.** Introducing `GlyphOptions` avoids forcing
  callers through the shaping/layout machinery. Confirm naming + that nothing essential is lost.
- **`GlyphRendererParameters` exposes `TextRun`/`CodePoint` publicly** and is used as a cache key.
  Synthesized defaults must be deterministic so id-rendered glyphs cache correctly and don't collide
  with text-rendered ones. Settled: `GraphemeIndex` carries the shaping cluster (Avalonia's
  `GlyphCluster`) — it is consumed by `BaseGlyphBuilder`'s per-grapheme grouping, so a constant would
  be wrong; `CodePoint` is the value recovered via `TryGetCodePoint`, falling back to `default` when
  the id has no codepoint.
- **Decorations** should be opt-out by default on the id path.
- **Public API surface.** All of A and C are additive public API on a stable library — needs
  maintainer review and a `PublicApiAnalyzers` shipped/unshipped update.
- **Vertical layout & substitution.** The id path renders the id as given; it performs no GSUB/GPOS
  (the caller already shaped). Document that explicitly.

## Test plan

- `TryGetGlyphMetrics(glyphId, …)` returns metrics matching the codepoint path for known ids
  (TrueType, CFF, COLR).
- Render a known id to a recording `IGlyphRenderer`; assert the emitted figure commands match the
  text-driven render of the same glyph (golden path equality).
- Round-trip: `TryGetGlyphId(cp) → Render(id)` equals `Render(cp)` for simple Latin.
- COLR font: painted layers emitted for a colour glyph rendered by id.
- Vertical + rotated layout modes produce the same bounds as the text path.

## Phasing

1. **Fonts – accessors + refactor.** Add `TryGetGlyphMetrics(id)`; extract the
   `RenderTo` core away from `TextOptions`, with `TextRun` passed explicitly. Internal, fully
   covered by tests. No public render entry yet.
2. **Fonts – public render entry.** `GlyphOptions` + `TextRenderer.Render`. API review + tests.
3. **ImageSharp.Drawing – consume.** Rework the Avalonia backend `DrawGlyphRun`; verify visual parity
   against the Skia backend on the text-heavy catalog pages.
