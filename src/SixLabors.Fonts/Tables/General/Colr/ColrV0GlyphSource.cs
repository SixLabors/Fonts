// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Supplies painted glyphs for COLR v0 fonts.
/// Flattens paint graphs into a linear <see cref="PaintedLayer"/> stream and emits a <see cref="PaintedCanvas"/>.
/// </summary>
internal sealed class ColrV0GlyphSource : ColrGlyphSourceBase
{
    private static readonly ConcurrentDictionary<ushort, (PaintedGlyph Glyph, PaintedCanvas Canvas)> CachedGlyphs = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrV0GlyphSource"/> class.
    /// </summary>
    /// <param name="colr">The COLR table.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="glyphLoader">Delegate that loads a glyph outline for the given glyph id.</param>
    public ColrV0GlyphSource(ColrTable colr, CpalTable? cpal, Func<ushort, GlyphVector?> glyphLoader)
        : base(colr, cpal, glyphLoader)
    {
    }

    /// <inheritdoc/>
    public override bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvas canvas)
    {
        (PaintedGlyph Glyph, PaintedCanvas Canvas) result = CachedGlyphs.GetOrAdd(glyphId, id =>
        {
            if (this.Colr.TryGetColrV0Layers(id, out Span<LayerRecord> resolved))
            {
                List<PaintedLayer> layers = new(resolved.Length);
                for (int i = 0; i < resolved.Length; i++)
                {
                    LayerRecord rl = resolved[i];
                    GlyphVector? gv = this.GlyphLoader(rl.GlyphId);
                    if (gv is null || !gv.Value.HasValue())
                    {
                        continue;
                    }

                    // Build geometry once for this layer.
                    List<PathCommand> path = BuildPath(gv.Value);

                    // Flatten paint graph: attach composite mode to leaves.
                    List<Rendering.Paint> leafPaints = [];
                    PaintSolid paint = new() { PaletteIndex = rl.PaletteIndex, Alpha = 1, Format = 2 };
                    FlattenPaint(paint, Matrix3x2.Identity, CompositeMode.SrcOver, this.Cpal, leafPaints);

                    // Emit one layer per leaf paint.
                    for (int p = 0; p < leafPaints.Count; p++)
                    {
                        // Unlike COLR v1, COLR v0 leaves have no transform so we can reuse the same path.
                        Rendering.Paint leaf = leafPaints[p];
                        layers.Add(new PaintedLayer(leaf, FillRule.NonZero, leaf.Transform, null, path));
                    }
                }

                if (layers.Count > 0)
                {
                    // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
                    PaintedGlyph glyph = new(layers);
                    PaintedCanvas canvas = new(FontRectangle.Empty, isYDown: false, rootTransform: Matrix3x2.Identity);
                    return (glyph, canvas);
                }
            }

            return (default, default);
        });

        glyph = result.Glyph;
        canvas = result.Canvas;
        return result.Glyph.Layers.Count > 0;
    }
}
