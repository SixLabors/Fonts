// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Represents the data for a single CFF glyph, including the raw charstring program
/// and subroutine references needed for evaluation and rendering.
/// </summary>
internal struct CffGlyphData
{
    private readonly byte[][] globalSubrBuffers;
    private readonly byte[][] localSubrBuffers;
    private readonly byte[] charStrings;
    private readonly int nominalWidthX;
    private readonly int version;
    private readonly ItemVariationStore? itemVariationStore;
    private readonly int vsIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CffGlyphData"/> struct.
    /// </summary>
    /// <param name="glyphIndex">The glyph index (GID).</param>
    /// <param name="globalSubrBuffers">The global subroutine buffers.</param>
    /// <param name="localSubrBuffers">The local subroutine buffers.</param>
    /// <param name="nominalWidthX">The nominal width bias for charstring width values.</param>
    /// <param name="charStrings">The raw charstring byte data for this glyph.</param>
    /// <param name="version">The CFF version (1 or 2).</param>
    /// <param name="itemVariationStore">The optional item variation store for CFF2 blend operations.</param>
    /// <param name="vsIndex">The variation store index for blend operations.</param>
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

    /// <summary>
    /// Gets the glyph index (GID) within the font.
    /// </summary>
    public ushort GlyphIndex { get; }

    /// <summary>
    /// Gets or sets the glyph name from the charset data.
    /// </summary>
    public string? GlyphName { get; set; }

    /// <summary>
    /// Gets or sets the font variations table for CFF2 variable fonts.
    /// </summary>
    public FVarTable? FVar { get; set; }

    /// <summary>
    /// Gets or sets the axis variations table for CFF2 variable fonts.
    /// </summary>
    public AVarTable? AVar { get; set; }

    /// <summary>
    /// Gets or sets the glyph variations table for TrueType-style glyph variations.
    /// </summary>
    public GVarTable? GVar { get; set; }

    /// <summary>
    /// Gets or sets the FontMatrix that transforms charstring coordinates to design units.
    /// </summary>
    public double[]? FontMatrix { get; set; }

    /// <summary>
    /// Computes the bounding box of this glyph by evaluating the charstring program.
    /// </summary>
    /// <returns>The <see cref="Bounds"/> of the glyph.</returns>
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

    /// <summary>
    /// Renders this glyph to the specified renderer by evaluating the charstring program.
    /// </summary>
    /// <param name="renderer">The glyph renderer to output path operations to.</param>
    /// <param name="origin">The origin point for rendering.</param>
    /// <param name="scale">The scale factor to apply.</param>
    /// <param name="offset">The offset to apply.</param>
    /// <param name="transform">The transformation matrix to apply.</param>
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
