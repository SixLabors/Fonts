// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.Cff;

internal struct CffGlyphData
{
    private readonly byte[][] globalSubrBuffers;
    private readonly byte[][] localSubrBuffers;
    private readonly byte[] charStrings;
    private readonly int nominalWidthX;

    public CffGlyphData(
        ushort glyphIndex,
        byte[][] globalSubrBuffers,
        byte[][] localSubrBuffers,
        int nominalWidthX,
        byte[] charStrings)
    {
        this.GlyphIndex = glyphIndex;
        this.globalSubrBuffers = globalSubrBuffers;
        this.localSubrBuffers = localSubrBuffers;
        this.nominalWidthX = nominalWidthX;
        this.charStrings = charStrings;

        this.GlyphName = null;
    }

    public readonly ushort GlyphIndex { get; }

    public string? GlyphName { get; set; }

    public readonly Bounds GetBounds()
    {
        using var engine = new CffEvaluationEngine(
            this.charStrings,
            this.globalSubrBuffers,
            this.localSubrBuffers,
            this.nominalWidthX);

        return engine.GetBounds();
    }

    public readonly void RenderTo(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
    {
        using var engine = new CffEvaluationEngine(
             this.charStrings,
             this.globalSubrBuffers,
             this.localSubrBuffers,
             this.nominalWidthX);

        engine.RenderTo(renderer, origin, scale, offset, transform);
    }
}
