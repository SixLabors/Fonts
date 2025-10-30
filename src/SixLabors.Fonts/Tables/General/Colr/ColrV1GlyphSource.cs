// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;
using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Supplies painted glyphs for COLR v1 fonts.
/// Flattens paint graphs into a linear <see cref="PaintedLayer"/> stream and emits a <see cref="PaintedCanvas"/>.
/// </summary>
internal sealed class ColrV1GlyphSource : ColrGlyphSourceBase
{
    private static readonly ConcurrentDictionary<ushort, (PaintedGlyph Glyph, PaintedCanvas Canvas)> CachedGlyphs = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrV1GlyphSource"/> class.
    /// </summary>
    /// <param name="colr">The COLR table.</param>
    /// <param name="cpal">The CPAL table, or null if not present.</param>
    /// <param name="glyphLoader">Delegate that loads a glyph outline for the given glyph id.</param>
    public ColrV1GlyphSource(ColrTable colr, CpalTable? cpal, Func<ushort, GlyphVector?> glyphLoader)
        : base(colr, cpal, glyphLoader)
    {
    }

    /// <inheritdoc/>
    public override bool TryGetPaintedGlyph(ushort glyphId, out PaintedGlyph glyph, out PaintedCanvas canvas)
    {
        if (CachedGlyphs.TryGetValue(glyphId, out (PaintedGlyph Glyph, PaintedCanvas Canvas) cached))
        {
            glyph = cached.Glyph;
            canvas = cached.Canvas;
            return true;
        }

        glyph = default;
        canvas = default;

        if (!this.Colr.TryGetColrV1Layers(glyphId, out List<ResolvedGlyphLayer>? resolved))
        {
            return false;
        }

        List<PaintedLayer> layers = new(resolved.Count);

        for (int i = 0; i < resolved.Count; i++)
        {
            ResolvedGlyphLayer rl = resolved[i];

            GlyphVector? gv = this.GlyphLoader(rl.GlyphId);
            if (gv is null || !gv.Value.HasValue())
            {
                continue;
            }

            // Build geometry once for this layer.
            List<PathCommand> path = BuildPath(gv.Value);

            // Flatten paint graph: accumulate wrapper transforms; attach composite mode to leaves.
            List<Rendering.Paint> leafPaints = [];

            FlattenPaint(rl.Paint, rl.Transform, rl.CompositeMode, this.Cpal, leafPaints);

            // Emit one layer per leaf paint.
            Bounds? clip = rl.ClipBox;
            for (int p = 0; p < leafPaints.Count; p++)
            {
                Rendering.Paint leaf = leafPaints[p];

                Matrix3x2 xForm = Matrix3x2.Identity;
                if (leaf is SolidPaint solid)
                {
                    // Move the transform from the paint to the layer.
                    // We do this so that solid paints are also transformed correctly as
                    // their location is defined in the local space of the layer.
                    xForm = solid.Transform;
                    solid.Transform = Matrix3x2.Identity;
                }

                layers.Add(new PaintedLayer(leaf, FillRule.NonZero, xForm, clip, path));
            }
        }

        if (layers.Count == 0)
        {
            return false;
        }

        // Canvas viewBox in Y-up; renderer downstream decides orientation via flag.
        glyph = new PaintedGlyph(layers);
        canvas = new PaintedCanvas(FontRectangle.Empty, isYDown: false, rootTransform: Matrix3x2.Identity);
        CachedGlyphs[glyphId] = (glyph, canvas);
        return true;
    }
}
