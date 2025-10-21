// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

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
        glyph = default;
        canvas = default;

        if (!this.Colr.TryGetColrV0Layers(glyphId, out Span<LayerRecord> resolved))
        {
            return false;
        }

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

            // Flatten paint graph: accumulate wrapper transforms; attach composite mode to leaves.
            List<Rendering.Paint> leafPaints = [];
            PaintSolid paint = new() { PaletteIndex = rl.PaletteIndex, Alpha = 1, Format = 2 };
            FlattenPaint(paint, Matrix3x2.Identity, CompositeMode.SrcOver, this.Cpal, leafPaints);

            // Emit one layer per leaf paint.
            for (int p = 0; p < leafPaints.Count; p++)
            {
                Rendering.Paint leaf = leafPaints[p];
                layers.Add(new PaintedLayer(leaf, FillRule.NonZero, leaf.Transform, null, path));
            }
        }

        if (layers.Count == 0)
        {
            return false;
        }

        // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
        glyph = new PaintedGlyph(layers);
        canvas = new PaintedCanvas(FontRectangle.Empty, isYDown: false, rootTransform: Matrix3x2.Identity);
        return true;
    }
}
