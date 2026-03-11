// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

internal struct CffGlyphData
{
    private readonly byte[][] globalSubrBuffers;
    private readonly byte[][] localSubrBuffers;
    private readonly byte[] charStrings;
    private readonly int nominalWidthX;
    private readonly int version;
    private readonly ItemVariationStore? itemVariationStore;
    private readonly int vsIndex;

    public CffGlyphData(
        ushort glyphIndex,
        byte[][] globalSubrBuffers,
        byte[][] localSubrBuffers,
        int nominalWidthX,
        byte[] charStrings,
        int version,
        ItemVariationStore? itemVariationStore = null,
        int vsIndex = 0)
    {
        this.GlyphIndex = glyphIndex;
        this.globalSubrBuffers = globalSubrBuffers;
        this.localSubrBuffers = localSubrBuffers;
        this.nominalWidthX = nominalWidthX;
        this.charStrings = charStrings;
        this.version = version;
        this.itemVariationStore = itemVariationStore;
        this.vsIndex = vsIndex;

        this.GlyphName = null;

        // Variations tables are only present for CFF2 format.
        this.FVar = null;
        this.AVar = null;
        this.GVar = null;
    }

    public ushort GlyphIndex { get; }

    public string? GlyphName { get; set; }

    public FVarTable? FVar { get; set; }

    public AVarTable? AVar { get; set; }

    public GVarTable? GVar { get; set; }

    public double[]? FontMatrix { get; set; }

    public readonly Bounds GetBounds()
    {
        using CffEvaluationEngine engine = new(
            this.charStrings,
            this.globalSubrBuffers,
            this.localSubrBuffers,
            this.nominalWidthX,
            this.version,
            this.itemVariationStore,
            this.FVar,
            this.AVar,
            this.vsIndex);

        return engine.GetBounds();
    }

    public readonly void RenderTo(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
    {
        using CffEvaluationEngine engine = new(
             this.charStrings,
             this.globalSubrBuffers,
             this.localSubrBuffers,
             this.nominalWidthX,
             this.version,
             this.itemVariationStore,
             this.FVar,
             this.AVar,
             this.vsIndex);

        engine.RenderTo(renderer, origin, scale, offset, transform);
    }
}
