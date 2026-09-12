// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Supplies painted glyphs for COLR v1 fonts.
/// Flattens paint graphs into a linear <see cref="PaintedLayer"/> stream while preserving composite-group boundaries,
/// and emits a <see cref="PaintedCanvasMetadata"/>.
/// </summary>
internal sealed class ColrV1GlyphSource : ColrGlyphSourceBase
{
    /// <summary>
    /// Cache of previously resolved painted glyphs keyed by glyph ID.
    /// </summary>
    private readonly ConcurrentDictionary<ushort, (PaintedGlyph Glyph, PaintedCanvasMetadata Canvas)> cachedGlyphs = [];

    /// <summary>
    /// The glyph variation processor for variable fonts, or <see langword="null"/> for static fonts.
    /// </summary>
    private readonly GlyphVariationProcessor? processor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrV1GlyphSource"/> class.
    /// </summary>
    /// <param name="colr">The COLR table.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="palette">The palette selection, or null for the font's default palette.</param>
    /// <param name="glyphLoader">Delegate that loads a glyph outline for the given glyph id.</param>
    /// <param name="processor">The glyph variation processor for variable fonts, or null.</param>
    public ColrV1GlyphSource(ColrTable colr, CpalTable? cpal, FontPalette? palette, Func<ushort, GlyphVector?> glyphLoader, GlyphVariationProcessor? processor)
        : base(colr, cpal, palette, glyphLoader)
        => this.processor = processor;

    /// <inheritdoc/>
    public override bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvasMetadata canvas)
    {
        (PaintedGlyph Glyph, PaintedCanvasMetadata Canvas) result = this.cachedGlyphs.GetOrAdd(glyphId, _ =>
        {
            if (this.Colr.TryGetColrV1Layers(
                glyphId,
                this.processor,
                out List<ResolvedGlyphLayer>? resolved,
                out List<PaintedCompositeCommand>? resolvedCompositeCommands,
                out Bounds? clipBounds))
            {
                List<PaintedLayer> layers = new(resolved.Count);
                List<PaintedCompositeCommand> compositeCommands = new(resolvedCompositeCommands.Count);
                int compositeCommandIndex = 0;

                for (int i = 0; i < resolved.Count; i++)
                {
                    // Invalid or empty glyph outlines are omitted below. Remap each command to the
                    // next emitted painted layer so group transitions retain their original order.
                    while (compositeCommandIndex < resolvedCompositeCommands.Count
                        && resolvedCompositeCommands[compositeCommandIndex].LayerIndex == i)
                    {
                        PaintedCompositeCommand command = resolvedCompositeCommands[compositeCommandIndex++];
                        compositeCommands.Add(new PaintedCompositeCommand(layers.Count, command.Kind, command.Mode));
                    }

                    ResolvedGlyphLayer rl = resolved[i];
                    List<PathCommand> path;

                    if (rl.GlyphId.HasValue)
                    {
                        GlyphVector? gv = this.GlyphLoader(rl.GlyphId.Value);
                        if (gv is null || !gv.Value.HasValue())
                        {
                            continue;
                        }

                        // Build geometry once for this layer.
                        path = BuildPath(gv.Value);
                    }
                    else
                    {
                        // The paint owns no outline. The empty stream tells the streaming layer
                        // to emit the clip bounds or glyph bounds as the figure.
                        path = [];
                    }

                    // Composite modes belong to the group commands; ordinary leaf application is SrcOver.
                    List<Rendering.Paint> leafPaints = [];
                    FlattenPaint(rl.Paint, rl.PaintTransform, CompositeMode.SrcOver, this.PaletteColors, this.Colr, this.processor, leafPaints);

                    // Emit one layer per leaf paint.
                    for (int p = 0; p < leafPaints.Count; p++)
                    {
                        Rendering.Paint leaf = leafPaints[p];
                        layers.Add(new PaintedLayer(leaf, FillRule.NonZero, rl.GlyphTransform, path));
                    }
                }

                // Commands after the last resolved layer close any still-active nested groups.
                while (compositeCommandIndex < resolvedCompositeCommands.Count)
                {
                    PaintedCompositeCommand command = resolvedCompositeCommands[compositeCommandIndex++];
                    compositeCommands.Add(new PaintedCompositeCommand(layers.Count, command.Kind, command.Mode));
                }

                if (layers.Count > 0)
                {
                    // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
                    PaintedGlyph glyph = new(
                        layers,
                        compositeCommands.Count > 0 ? compositeCommands : null,
                        clipBounds);

                    PaintedCanvasMetadata canvas = new(FontRectangle.Empty, isYDown: false, rootTransform: Matrix3x2.Identity);
                    return (glyph, canvas);
                }
            }

            return (default, default);
        });

        glyph = result.Glyph;
        canvas = result.Canvas;

        // A base glyph whose paint graph yields no layers is cached as the default glyph,
        // whose layer list is null; it is reported as not painted so the plain outline is used.
        return result.Glyph.Layers?.Count > 0;
    }
}
