// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.General.Colr;

/// <summary>
/// Represents the OpenType COLR table, which defines color glyph data for both v0 (layer-based)
/// and v1 (paint-based) color fonts.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/colr"/>
/// </summary>
internal class ColrTable : Table
{
    /// <summary>
    /// The table tag name "COLR".
    /// </summary>
    internal const string TableName = "COLR";

    /// <summary>
    /// The COLR v0 base glyph records mapping glyph IDs to layer ranges.
    /// </summary>
    private readonly BaseGlyphRecord[] glyphRecords;

    /// <summary>
    /// The COLR v0 layer records defining color layers.
    /// </summary>
    private readonly LayerRecord[] layers;

    /// <summary>
    /// The COLR v1 BaseGlyphList, or <see langword="null"/> if not present.
    /// </summary>
    private readonly BaseGlyphList? baseGlyphList;

    /// <summary>
    /// The COLR v1 LayerList, or <see langword="null"/> if not present.
    /// </summary>
    private readonly LayerList? layerList;

    /// <summary>
    /// The COLR v1 ClipList, or <see langword="null"/> if not present.
    /// </summary>
    private readonly ClipList? clipList;

    /// <summary>
    /// The ItemVariationStore for variable font data, or <see langword="null"/> if not present.
    /// </summary>
    private readonly ItemVariationStore? itemVariationStore;

    /// <summary>
    /// The DeltaSetIndexMap array for mapping variation indices, or <see langword="null"/> if not present.
    /// </summary>
    private readonly DeltaSetIndexMap[]? deltaSetIndexMap;

    /// <summary>
    /// Cache of resolved paint objects keyed by their COLR-relative offset.
    /// </summary>
    private readonly Dictionary<uint, Paint>? paintCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrTable"/> class for COLR v0 data only.
    /// </summary>
    /// <param name="glyphRecords">The base glyph records.</param>
    /// <param name="layers">The layer records.</param>
    public ColrTable(
        BaseGlyphRecord[] glyphRecords,
        LayerRecord[] layers)
        : this(glyphRecords, layers, null, null, null, null, null, null, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColrTable"/> class with both v0 and optional v1 data.
    /// </summary>
    /// <param name="glyphRecords">The COLR v0 base glyph records.</param>
    /// <param name="layers">The COLR v0 layer records.</param>
    /// <param name="baseGlyphList">The COLR v1 base glyph list, or <see langword="null"/>.</param>
    /// <param name="layerList">The COLR v1 layer list, or <see langword="null"/>.</param>
    /// <param name="clipList">The COLR v1 clip list, or <see langword="null"/>.</param>
    /// <param name="itemVariationStore">The ItemVariationStore for variable font data, or <see langword="null"/>.</param>
    /// <param name="deltaSetIndexMap">The DeltaSetIndexMap array, or <see langword="null"/>.</param>
    /// <param name="paintCache">The pre-loaded paint cache, or <see langword="null"/>.</param>
    /// <param name="version">The COLR table version.</param>
    public ColrTable(
        BaseGlyphRecord[] glyphRecords,
        LayerRecord[] layers,
        BaseGlyphList? baseGlyphList,
        LayerList? layerList,
        ClipList? clipList,
        ItemVariationStore? itemVariationStore,
        DeltaSetIndexMap[]? deltaSetIndexMap,
        Dictionary<uint, Paint>? paintCache = null,
        int version = 1)
    {
        this.glyphRecords = glyphRecords;
        this.layers = layers;
        this.baseGlyphList = baseGlyphList;
        this.layerList = layerList;
        this.clipList = clipList;
        this.itemVariationStore = itemVariationStore;
        this.deltaSetIndexMap = deltaSetIndexMap;
        this.paintCache = paintCache;
        this.Version = version;
    }

    /// <summary>
    /// Gets the COLR table version (0 or 1).
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Resolves a variation delta for a given variable index using the COLR table's
    /// own ItemVariationStore and optional DeltaSetIndexMap.
    /// </summary>
    /// <param name="processor">The glyph variation processor (null for non-variable fonts).</param>
    /// <param name="varIdx">The variable index (VarIndexBase + field offset).</param>
    /// <returns>The delta value, or 0 if no variation data is available.</returns>
    internal float ResolveDelta(GlyphVariationProcessor? processor, uint varIdx)
    {
        if (processor is null || this.itemVariationStore is null)
        {
            return 0;
        }

        int outer;
        int inner;
        if (this.deltaSetIndexMap is not null && varIdx < (uint)this.deltaSetIndexMap.Length)
        {
            DeltaSetIndexMap mapping = this.deltaSetIndexMap[varIdx];
            outer = mapping.OuterIndex;
            inner = mapping.InnerIndex;
        }
        else
        {
            // Implicit mapping: upper 16 bits = outer, lower 16 bits = inner.
            outer = (int)(varIdx >> 16);
            inner = (int)(varIdx & 0xFFFF);
        }

        return processor.Delta(this.itemVariationStore, outer, inner);
    }

    /// <summary>
    /// Loads the COLR table from the specified font reader.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <returns>The loaded <see cref="ColrTable"/>, or <see langword="null"/> if the table is not present.</returns>
    public static ColrTable? Load(FontReader fontReader)
    {
        if (!fontReader.TryGetReaderAtTablePosition(TableName, out BigEndianBinaryReader? binaryReader))
        {
            return null;
        }

        using (binaryReader)
        {
            return Load(binaryReader);
        }
    }

    /// <summary>
    /// Gets the COLR v0 layer records for the specified glyph.
    /// </summary>
    /// <param name="glyph">The glyph ID.</param>
    /// <returns>A span of layer records for the glyph, or an empty span if not found.</returns>
    internal Span<LayerRecord> GetLayers(ushort glyph)
    {
        foreach (BaseGlyphRecord g in this.glyphRecords)
        {
            if (g.GlyphId == glyph)
            {
                return this.layers.AsSpan().Slice(g.FirstLayerIndex, g.LayerCount);
            }
        }

        return [];
    }

    /// <summary>
    /// Determines whether the specified glyph has an associated COLR v0 color glyph definition.
    /// </summary>
    /// <param name="glyphId">The identifier of the glyph to check for a COLR v0 color glyph definition.</param>
    /// <returns>
    /// <see langword="true"/> if the specified glyph has a COLR v0 color glyph definition; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsColorV0Glyph(ushort glyphId)
    {
        for (int i = 0; i < this.glyphRecords.Length; i++)
        {
            BaseGlyphRecord g = this.glyphRecords[i];
            if (g.GlyphId == glyphId)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified glyph has an associated COLR v1 color glyph definition.
    /// </summary>
    /// <param name="glyphId">The identifier of the glyph to check for a COLR v1 color glyph definition.</param>
    /// <returns>
    /// <see langword="true"/> if the specified glyph has a COLR v1 color glyph definition; otherwise, <see langword="false"/>.
    /// </returns>
    public bool ContainsColorV1Glyph(ushort glyphId)
    {
        if (this.baseGlyphList is null || this.layerList is null || this.paintCache is null)
        {
            return false; // No COLR v1 data
        }

        return this.TryGetRootPaintOffset(glyphId, out uint _);
    }

    /// <summary>
    /// Attempts to retrieve the set of color layer records associated with the specified glyph.
    /// </summary>
    /// <param name="glyph">The glyph ID for which to retrieve color layer records.</param>
    /// <param name="records">
    /// When this method returns, contains a span of <see cref="LayerRecord"/> structures
    /// representing the color layers for the specified glyph, if found; otherwise, an empty span.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if color layer records are found for the specified glyph; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    internal bool TryGetColrV0Layers(ushort glyph, out Span<LayerRecord> records)
    {
        for (int i = 0; i < this.glyphRecords.Length; i++)
        {
            BaseGlyphRecord g = this.glyphRecords[i];
            if (g.GlyphId == glyph)
            {
                records = this.layers.AsSpan().Slice(g.FirstLayerIndex, g.LayerCount);
                return true;
            }
        }

        records = [];
        return false;
    }

    /// <summary>
    /// Attempts to resolve and retrieve the list of color glyph layers for the specified glyph ID.
    /// </summary>
    /// <param name="glyphId">The identifier of the glyph for which to resolve color layers.</param>
    /// <param name="processor">The glyph variation processor, or null for non-variable fonts.</param>
    /// <param name="layers">
    /// When this method returns, contains a list of resolved glyph layers if the operation succeeds; otherwise,
    /// <see langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns>
    /// <see langword="true"/> if the color glyph layers were successfully resolved; otherwise, <see langword="false"/>.
    /// </returns>
    internal bool TryGetColrV1Layers(
        ushort glyphId,
        GlyphVariationProcessor? processor,
        [NotNullWhen(true)] out List<ResolvedGlyphLayer>? layers)
    {
        layers = null;

        if (this.baseGlyphList is null || this.layerList is null || this.paintCache is null)
        {
            return false; // No COLR v1 data
        }

        // 1) Resolve root paint for the requested base glyph
        if (!this.TryGetRootPaintOffset(glyphId, out uint rootOff) || rootOff == 0)
        {
            return false;
        }

        if (!this.paintCache.TryGetValue(rootOff, out Paint? root) || root is null)
        {
            return false;
        }

        // 2) Flatten paint graph to layers. Start with no current glyph id.
        List<ResolvedGlyphLayer> acc = [];
        this.FlattenPaintToLayers(root, null, Matrix3x2.Identity, Matrix3x2.Identity, false, CompositeMode.SrcOver, processor, acc);

        // 3) If nothing emitted, the graph did not bind any geometry (no PaintGlyph/ColrGlyph reached).
        if (acc.Count == 0)
        {
            layers = null;
            return false;
        }

        layers = acc;
        return true;
    }

    /// <summary>
    /// Recursively flattens a COLR v1 paint subtree into <see cref="ResolvedGlyphLayer"/>s.
    /// A layer is emitted only when a leaf paint is reached under an active glyph-binding node:
    /// <list type="bullet">
    /// <item><description><b>PaintGlyph</b> sets the current glyph id to its <c>GlyphId</c> and recurses into its child paint.</description></item>
    /// <item><description><b>PaintColrGlyph</b> resolves that glyph's root paint, sets the current glyph id, and recurses.</description></item>
    /// <item><description>Wrapper nodes (transform/translate/scale/rotate/skew, var forms) forward the current glyph id unchanged.</description></item>
    /// <item><description><b>PaintComposite</b> flattens both branches independently, forwarding the current glyph id to each.</description></item>
    /// <item><description>Leaf paints (solid/linear/radial/sweep, var forms) emit a layer only if <paramref name="currentGlyphId"/> has a value.</description></item>
    /// </list>
    /// </summary>
    /// <param name="node">The paint node to flatten.</param>
    /// <param name="currentGlyphId">
    /// The glyph id whose outline will receive the paint. Set by <c>PaintGlyph</c>/<c>PaintColrGlyph</c>.
    /// </param>
    /// <param name="transform">Accumulated transform.</param>
    /// <param name="compositeMode">Accumulated composite mode.</param>
    /// <param name="processor">The glyph variation processor, or null for non-variable fonts.</param>
    /// <param name="outLayers">Accumulator for resolved layers.</param>
    private void FlattenPaintToLayers(
        Paint node,
        ushort? currentGlyphId,
        Matrix3x2 glyphTransform,
        Matrix3x2 paintTransform,
        bool transformPaint,
        CompositeMode compositeMode,
        GlyphVariationProcessor? processor,
        List<ResolvedGlyphLayer> outLayers)
    {
        switch (node)
        {
            // ---------------------------
            // Containers and indirections
            // ---------------------------
            case PaintColrLayers pcl:
            {
                // Iterates layer indices and flattens each addressed paint subtree.
                // No glyph id is implied here; child subtrees must bind via PaintGlyph/ColrGlyph.
                int first = (int)pcl.FirstLayerIndex;
                int count = pcl.NumLayers;
                ReadOnlySpan<uint> offs = this.GetLayerPaintOffsets(first, count);

                for (int i = 0; i < offs.Length; i++)
                {
                    uint off = offs[i];
                    if (off == 0)
                    {
                        continue;
                    }

                    if (this.paintCache!.TryGetValue(off, out Paint? child) && child is not null)
                    {
                        this.FlattenPaintToLayers(child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                    }
                }

                return;
            }

            case PaintColrGlyph pcg:
            {
                // Resolve the referenced glyph's root paint and recurse through its own bindings.
                if (this.TryGetRootPaintOffset(pcg.GlyphId, out uint off) && off != 0
                    && this.paintCache!.TryGetValue(off, out Paint? colrRoot) && colrRoot is not null)
                {
                    this.FlattenPaintToLayers(colrRoot, null, glyphTransform, Matrix3x2.Identity, false, compositeMode, processor, outLayers);
                }

                return;
            }

            case PaintGlyph pg:
            {
                // Bind geometry to the specified glyph id and recurse into its child paint.
                this.FlattenPaintToLayers(pg.Child, pg.GlyphId, glyphTransform, Matrix3x2.Identity, true, compositeMode, processor, outLayers);
                return;
            }

            // ---------------------------
            // Wrappers: forward glyph id
            // ---------------------------
            case PaintTransform pt:
            {
                Affine2x3 a = pt.Transform;
                Matrix3x2 next = new(a.Xx, a.Yx, a.Xy, a.Yy, a.Dx, a.Dy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(pt.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintVarTransform pvt:
            {
                VarAffine2x3 a = pvt.Transform;
                uint vib = a.VarIndexBase;
                float xx = a.Xx + this.ResolveDelta(processor, vib + 0u);
                float yx = a.Yx + this.ResolveDelta(processor, vib + 1u);
                float xy = a.Xy + this.ResolveDelta(processor, vib + 2u);
                float yy = a.Yy + this.ResolveDelta(processor, vib + 3u);
                float dx = a.Dx + this.ResolveDelta(processor, vib + 4u);
                float dy = a.Dy + this.ResolveDelta(processor, vib + 5u);
                Matrix3x2 next = new(xx, yx, xy, yy, dx, dy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(pvt.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintTranslate t:
            {
                Matrix3x2 next = Matrix3x2.CreateTranslation(t.Dx, t.Dy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(t.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintVarTranslate vt:
            {
                float dx = vt.Dx + this.ResolveDelta(processor, vt.VarIndexBase + 0u);
                float dy = vt.Dy + this.ResolveDelta(processor, vt.VarIndexBase + 1u);
                Matrix3x2 next = Matrix3x2.CreateTranslation(dx, dy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(vt.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintScale s:
            {
                Matrix3x2 next = BuildScale(s.ScaleX, s.ScaleY, s.AroundCenter, s.CenterX, s.CenterY);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(s.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintVarScale vs:
            {
                uint vib = vs.VarIndexBase;
                float sx = vs.ScaleX + this.ResolveDelta(processor, vib + 0u);
                float sy = vs.Uniform ? sx : vs.ScaleY + this.ResolveDelta(processor, vib + 1u);
                int centerOffset = vs.Uniform ? 1 : 2;
                float cx = vs.AroundCenter ? vs.CenterX + this.ResolveDelta(processor, vib + (uint)centerOffset) : 0;
                float cy = vs.AroundCenter ? vs.CenterY + this.ResolveDelta(processor, vib + (uint)centerOffset + 1u) : 0;
                Matrix3x2 next = BuildScale(sx, sy, vs.AroundCenter, cx, cy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(vs.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintRotate r:
            {
                Matrix3x2 next = BuildRotate(r.Angle, r.AroundCenter, r.CenterX, r.CenterY);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(r.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintVarRotate vr:
            {
                uint vib = vr.VarIndexBase;
                float angle = vr.Angle + this.ResolveDelta(processor, vib + 0u);
                float cx = vr.AroundCenter ? vr.CenterX + this.ResolveDelta(processor, vib + 1u) : 0;
                float cy = vr.AroundCenter ? vr.CenterY + this.ResolveDelta(processor, vib + 2u) : 0;
                Matrix3x2 next = BuildRotate(angle, vr.AroundCenter, cx, cy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(vr.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintSkew k:
            {
                Matrix3x2 next = BuildSkew(k.XSkew, k.YSkew, k.AroundCenter, k.CenterX, k.CenterY);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(k.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintVarSkew vk:
            {
                uint vib = vk.VarIndexBase;
                float xSkew = vk.XSkew + this.ResolveDelta(processor, vib + 0u);
                float ySkew = vk.YSkew + this.ResolveDelta(processor, vib + 1u);
                float cx = vk.AroundCenter ? vk.CenterX + this.ResolveDelta(processor, vib + 2u) : 0;
                float cy = vk.AroundCenter ? vk.CenterY + this.ResolveDelta(processor, vib + 3u) : 0;
                Matrix3x2 next = BuildSkew(xSkew, ySkew, vk.AroundCenter, cx, cy);
                if (transformPaint)
                {
                    paintTransform *= next;
                }
                else
                {
                    glyphTransform *= next;
                }

                this.FlattenPaintToLayers(vk.Child, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            case PaintComposite comp:
            {
                compositeMode = MapCompositeMode(comp.CompositeMode);

                // Backdrop first, then Source. Both inherit the current glyph id.
                this.FlattenPaintToLayers(comp.Backdrop, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                this.FlattenPaintToLayers(comp.Source, currentGlyphId, glyphTransform, paintTransform, transformPaint, compositeMode, processor, outLayers);
                return;
            }

            // ---------------------------
            // Leaves: emit only if bound
            // ---------------------------
            case PaintSolid:
            case PaintVarSolid:
            case PaintLinearGradient:
            case PaintVarLinearGradient:
            case PaintRadialGradient:
            case PaintVarRadialGradient:
            case PaintSweepGradient:
            case PaintVarSweepGradient:
            {
                // Only emit if we have an active glyph id (i.e., we are inside a PaintGlyph/ColrGlyph branch).
                if (currentGlyphId.HasValue)
                {
                    _ = this.TryGetClipBox(currentGlyphId.Value, processor, out Bounds? clip);
                    outLayers.Add(new ResolvedGlyphLayer(currentGlyphId.Value, node, glyphTransform, paintTransform, compositeMode, clip));
                }

                return;
            }

            default:
            {
                // Unknown or unsupported node: do not emit and do not stop traversal.
                return;
            }
        }
    }

    /// <summary>
    /// Builds a scale matrix, optionally around a center.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildScale(float sx, float sy, bool aroundCenter, float cx, float cy)
    {
        if (!aroundCenter)
        {
            return Matrix3x2.CreateScale(sx, sy);
        }

        return Matrix3x2.CreateScale(sx, sy, new Vector2(cx, cy));
    }

    /// <summary>
    /// Builds a rotation matrix, optionally around a center.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildRotate(float angleColrUnits, bool aroundCenter, float cx, float cy)
    {
        // COLR: 1.0 == 180° => radians = angle * π
        float radians = angleColrUnits * MathF.PI;

        if (!aroundCenter)
        {
            return Matrix3x2.CreateRotation(radians);
        }

        return Matrix3x2.CreateRotation(radians, new Vector2(cx, cy));
    }

    /// <summary>
    /// Builds a skew matrix, optionally around a center.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildSkew(float xSkew, float ySkew, bool aroundCenter, float cx, float cy)
    {
        // COLR: 1.0 == 180° => radians = angle * π
        float rx = xSkew * MathF.PI;
        float ry = ySkew * MathF.PI;

        if (!aroundCenter)
        {
            return Matrix3x2.CreateSkew(rx, ry);
        }

        return Matrix3x2.CreateSkew(rx, ry, new Vector2(cx, cy));
    }

    /// <summary>
    /// Maps a COLR composite mode to the internal <see cref="CompositeMode"/>.
    /// <para>
    /// Returns <see cref="CompositeMode.SrcOver"/> when <paramref name="mode"/> is null
    /// or when the value is not recognized.
    /// </para>
    /// </summary>
    /// <param name="mode">The optional COLR composite mode.</param>
    /// <returns>The mapped <see cref="CompositeMode"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static CompositeMode MapCompositeMode(ColrCompositeMode? mode)
        => mode switch
        {
            // Porter–Duff
            ColrCompositeMode.Clear => CompositeMode.Clear,
            ColrCompositeMode.Src => CompositeMode.Src,
            ColrCompositeMode.Dst => CompositeMode.Dest,
            ColrCompositeMode.SrcOver => CompositeMode.SrcOver,
            ColrCompositeMode.DstOver => CompositeMode.DestOver,
            ColrCompositeMode.SrcIn => CompositeMode.SrcIn,
            ColrCompositeMode.DstIn => CompositeMode.DestIn,
            ColrCompositeMode.SrcOut => CompositeMode.SrcOut,
            ColrCompositeMode.DstOut => CompositeMode.DestOut,
            ColrCompositeMode.SrcAtop => CompositeMode.SrcAtop,
            ColrCompositeMode.DstAtop => CompositeMode.DestAtop,
            ColrCompositeMode.Xor => CompositeMode.Xor,
            ColrCompositeMode.Plus => CompositeMode.Plus,

            // Blend modes
            ColrCompositeMode.Screen => CompositeMode.Screen,
            ColrCompositeMode.Overlay => CompositeMode.Overlay,
            ColrCompositeMode.Darken => CompositeMode.Darken,
            ColrCompositeMode.Lighten => CompositeMode.Lighten,
            ColrCompositeMode.ColorDodge => CompositeMode.ColorDodge,
            ColrCompositeMode.ColorBurn => CompositeMode.ColorBurn,
            ColrCompositeMode.HardLight => CompositeMode.HardLight,
            ColrCompositeMode.SoftLight => CompositeMode.SoftLight,
            ColrCompositeMode.Difference => CompositeMode.Difference,
            ColrCompositeMode.Exclusion => CompositeMode.Exclusion,
            ColrCompositeMode.Multiply => CompositeMode.Multiply,
            ColrCompositeMode.Hue => CompositeMode.Hue,
            ColrCompositeMode.Saturation => CompositeMode.Saturation,
            ColrCompositeMode.Color => CompositeMode.Color,
            ColrCompositeMode.Luminosity => CompositeMode.Luminosity,
            _ => CompositeMode.SrcOver,
        };

    /// <summary>
    /// Attempts to retrieve the paint table offset associated with the specified glyph ID.
    /// </summary>
    /// <param name="glyphId">The glyph ID for which to look up the paint table offset.</param>
    /// <param name="paintOffset">
    /// When this method returns, contains the paint table offset for the specified glyph ID, if found; otherwise, zero.
    /// This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the paint table offset was found for the specified glyph ID; otherwise, <see langword="false"/>.
    /// </returns>
    private bool TryGetRootPaintOffset(ushort glyphId, out uint paintOffset)
    {
        if (this.baseGlyphList is null)
        {
            paintOffset = 0;
            return false;
        }

        ReadOnlySpan<BaseGlyphPaintRecord> recs = this.baseGlyphList.Records;
        int lo = 0, hi = recs.Length - 1;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            ushort gid = recs[mid].GlyphId;

            if (glyphId == gid)
            {
                paintOffset = recs[mid].PaintOffset;
                return true;
            }

            if (glyphId < gid)
            {
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        paintOffset = 0;
        return false;
    }

    /// <summary>
    /// Gets a span of paint offsets from the layer list starting at the specified index.
    /// </summary>
    /// <param name="first">The index of the first paint offset.</param>
    /// <param name="count">The number of paint offsets to retrieve.</param>
    /// <returns>A read-only span of paint offsets, or an empty span if the layer list is null or the range is invalid.</returns>
    private ReadOnlySpan<uint> GetLayerPaintOffsets(int first, int count)
    {
        if (this.layerList is null || count <= 0)
        {
            return [];
        }

        Span<uint> offsets = this.layerList.PaintOffsets.AsSpan();
        if ((uint)first >= (uint)offsets.Length)
        {
            return [];
        }

        int len = Math.Min(count, offsets.Length - first);
        return offsets.Slice(first, len);
    }

    /// <summary>
    /// Attempts to retrieve the clip box bounds for the specified glyph ID.
    /// </summary>
    /// <param name="glyphId">The glyph ID.</param>
    /// <param name="processor">The glyph variation processor, or <see langword="null"/> for non-variable fonts.</param>
    /// <param name="bounds">When this method returns, contains the clip bounds if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a clip box was found; otherwise, <see langword="false"/>.</returns>
    private bool TryGetClipBox(ushort glyphId, GlyphVariationProcessor? processor, out Bounds? bounds)
    {
        if (this.clipList is null)
        {
            bounds = default;
            return false;
        }

        return this.clipList.TryGetClipBox(glyphId, this, processor, out bounds);
    }

    /// <summary>
    /// Loads the COLR table from the specified binary reader.
    /// </summary>
    /// <param name="reader">The big-endian binary reader positioned at the start of the COLR table.</param>
    /// <returns>The loaded <see cref="ColrTable"/>.</returns>
    public static ColrTable Load(BigEndianBinaryReader reader)
    {
        // HEADER

        // Type      | Name                   | Description
        // ----------|------------------------|----------------------------------------------------------------------------------------------------
        // uint16    | version                | Table version number(starts at 0).
        // uint16    | numBaseGlyphRecords    | Number of Base Glyph Records.
        // Offset32  | baseGlyphRecordsOffset | Offset(from beginning of COLR table) to Base Glyph records.
        // Offset32  | layerRecordsOffset     | Offset(from beginning of COLR table) to Layer Records.
        // uint16    | numLayerRecords        | Number of Layer Records.
        ushort version = reader.ReadUInt16();
        ushort numBaseGlyphRecords = reader.ReadUInt16();
        uint baseGlyphRecordsOffset = reader.ReadOffset32();
        uint layerRecordsOffset = reader.ReadOffset32();
        ushort numLayerRecords = reader.ReadUInt16();

        uint baseGlyphListOffset = 0;
        uint layerListOffset = 0;
        uint clipListOffset = 0;
        uint varIndexMapOffset = 0;
        uint itemVariationStoreOffset = 0;

        if (version == 1)
        {
            // | Type     | Name                     | Description                                                                   |
            // |----------|--------------------------|-------------------------------------------------------------------------------|
            // | uint16   | version                  | Table version number—set to 1.                                                |
            // | uint16   | numBaseGlyphRecords      | Number of BaseGlyph records; may be 0 in a version 1 table.                   |
            // | Offset32 | baseGlyphRecordsOffset   | Offset to baseGlyphRecords array, from beginning of COLR table (may be NULL). |
            // | Offset32 | layerRecordsOffset       | Offset to layerRecords array, from beginning of COLR table (may be NULL).     |
            // | uint16   | numLayerRecords          | Number of Layer records; may be 0 in a version 1 table.                       |
            // | Offset32 | baseGlyphListOffset      | Offset to BaseGlyphList table, from beginning of COLR table.                  |
            // | Offset32 | layerListOffset          | Offset to LayerList table, from beginning of COLR table (may be NULL).        |
            // | Offset32 | clipListOffset           | Offset to ClipList table, from beginning of COLR table (may be NULL).         |
            // | Offset32 | varIndexMapOffset        | Offset to DeltaSetIndexMap table, from beginning of COLR table (may be NULL). |
            // | Offset32 | itemVariationStoreOffset | Offset to ItemVariationStore, from beginning of COLR table (may be NULL).     |
            baseGlyphListOffset = reader.ReadOffset32();
            layerListOffset = reader.ReadOffset32();
            clipListOffset = reader.ReadOffset32();
            varIndexMapOffset = reader.ReadOffset32();
            itemVariationStoreOffset = reader.ReadOffset32();
        }

        // v0: BaseGlyph and Layer records (optional in v1; may be zero)
        BaseGlyphRecord[] glyphs = [];
        if (numBaseGlyphRecords != 0 && baseGlyphRecordsOffset != 0)
        {
            glyphs = new BaseGlyphRecord[numBaseGlyphRecords];
            reader.Seek(baseGlyphRecordsOffset, SeekOrigin.Begin);

            for (int i = 0; i < numBaseGlyphRecords; i++)
            {
                ushort gi = reader.ReadUInt16();
                ushort idx = reader.ReadUInt16();
                ushort num = reader.ReadUInt16();
                glyphs[i] = new BaseGlyphRecord(gi, idx, num);
            }
        }

        LayerRecord[] layerRecs = [];
        if (numLayerRecords != 0 && layerRecordsOffset != 0)
        {
            layerRecs = new LayerRecord[numLayerRecords];
            reader.Seek(layerRecordsOffset, SeekOrigin.Begin);

            for (int i = 0; i < numLayerRecords; i++)
            {
                ushort gi = reader.ReadUInt16();
                ushort pi = reader.ReadUInt16();
                layerRecs[i] = new LayerRecord(gi, pi);
            }
        }

        // v1: BaseGlyphList, LayerList, ClipList (nullable if not present)
        BaseGlyphList? baseGlyphList = null;
        LayerList? layerList = null;
        ClipList? clipList = null;
        Dictionary<uint, Paint>? paintCache = null;

        if (version == 1)
        {
            baseGlyphList = BaseGlyphList.Load(reader, baseGlyphListOffset);
            layerList = LayerList.Load(reader, layerListOffset);
            clipList = ClipList.Load(reader, clipListOffset);

            paintCache = LoadPaintRoots(reader, baseGlyphList, layerList);
        }

        ItemVariationStore? itemVariationStore = itemVariationStoreOffset != 0
            ? ItemVariationStore.Load(reader, itemVariationStoreOffset)
            : null;

        DeltaSetIndexMap[]? deltaSetIndexMap = varIndexMapOffset != 0
            ? DeltaSetIndexMap.Load(reader, varIndexMapOffset)
            : null;

        return new ColrTable(glyphs, layerRecs, baseGlyphList, layerList, clipList, itemVariationStore, deltaSetIndexMap, paintCache, 1);
    }

    /// <summary>
    /// Eagerly loads and caches all paint objects referenced by the BaseGlyphList and LayerList.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="baseGlyphList">The base glyph list, or <see langword="null"/>.</param>
    /// <param name="layerList">The layer list, or <see langword="null"/>.</param>
    /// <returns>A dictionary mapping paint offsets to their resolved paint objects.</returns>
    private static Dictionary<uint, Paint> LoadPaintRoots(
        BigEndianBinaryReader reader,
        BaseGlyphList? baseGlyphList,
        LayerList? layerList)
    {
        PaintCaches caches = new();

        // 1) Root paints from BaseGlyphList
        if (baseGlyphList is not null)
        {
            foreach (BaseGlyphPaintRecord rec in baseGlyphList.Records)
            {
                if (rec.PaintOffset != 0)
                {
                    _ = LoadPaintAt(reader, rec.PaintOffset, layerList, caches);
                }
            }
        }

        // 2) All paints referenced by LayerList (PaintColrLayers points into these)
        if (layerList is not null)
        {
            foreach (uint offset in layerList.PaintOffsets)
            {
                if (offset != 0)
                {
                    _ = LoadPaintAt(reader, offset, layerList, caches);
                }
            }
        }

        return caches.PaintCache;
    }

    /// <summary>
    /// Loads a paint object from the specified offset, using the cache to avoid redundant reads.
    /// Recursively loads child paints as needed.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="paintOffset">The COLR-relative offset of the paint table.</param>
    /// <param name="layerList">The layer list for resolving PaintColrLayers references, or <see langword="null"/>.</param>
    /// <param name="caches">The shared caches for deduplicating loaded objects.</param>
    /// <returns>The loaded paint object.</returns>
    private static Paint LoadPaintAt(
        BigEndianBinaryReader reader,
        uint paintOffset,
        LayerList? layerList,
        PaintCaches caches)
    {
        if (caches.PaintCache.TryGetValue(paintOffset, out Paint? p))
        {
            return p;
        }

        long restore = reader.BaseStream.Position;
        reader.Seek(paintOffset, SeekOrigin.Begin);

        byte format = reader.ReadByte();
        Paint result;

        switch (format)
        {
            // 1: PaintColrLayers
            case 1:
            {
                byte numLayers = reader.ReadByte();
                uint firstLayerIndex = reader.ReadUInt32();

                result = new PaintColrLayers
                {
                    Format = format,
                    NumLayers = numLayers,
                    FirstLayerIndex = firstLayerIndex
                };

                // Walk children immediately:
                if (layerList is not null)
                {
                    for (uint i = 0; i < numLayers; i++)
                    {
                        int idx = (int)(firstLayerIndex + i);
                        uint layerPaintOff = layerList.PaintOffsets[idx];
                        if (layerPaintOff != 0)
                        {
                            _ = LoadPaintAt(reader, layerPaintOff, layerList, caches);
                        }
                    }
                }

                break;
            }

            // 2/3: PaintSolid / PaintVarSolid
            case 2:
            {
                ushort paletteIndex = reader.ReadUInt16();
                float alpha = reader.ReadF2Dot14();
                result = new PaintSolid { Format = format, PaletteIndex = paletteIndex, Alpha = alpha };
                break;
            }

            case 3:
            {
                ushort paletteIndex = reader.ReadUInt16();
                float alpha = reader.ReadF2Dot14();
                uint varBase = reader.ReadUInt32();
                result = new PaintVarSolid { Format = format, PaletteIndex = paletteIndex, Alpha = alpha, VarIndexBase = varBase };
                break;
            }

            // 4/5: PaintLinearGradient / PaintVarLinearGradient
            case 4:
            {
                uint colorLineOff = reader.ReadOffset24();
                ColorLine line = LoadColorLineAt(reader, paintOffset + colorLineOff, caches);
                short x0 = reader.ReadFWORD();
                short y0 = reader.ReadFWORD();
                short x1 = reader.ReadFWORD();
                short y1 = reader.ReadFWORD();
                short x2 = reader.ReadFWORD();
                short y2 = reader.ReadFWORD();
                result = new PaintLinearGradient { Format = format, ColorLine = line, X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 };
                break;
            }

            case 5:
            {
                uint colorLineOff = reader.ReadOffset24();
                VarColorLine line = LoadVarColorLineAt(reader, paintOffset + colorLineOff, caches);
                short x0 = reader.ReadFWORD();
                short y0 = reader.ReadFWORD();
                short x1 = reader.ReadFWORD();
                short y1 = reader.ReadFWORD();
                short x2 = reader.ReadFWORD();
                short y2 = reader.ReadFWORD();
                uint varBase = reader.ReadUInt32();
                result = new PaintVarLinearGradient
                {
                    Format = format,
                    ColorLine = line,
                    X0 = x0,
                    Y0 = y0,
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    VarIndexBase = varBase
                };
                break;
            }

            // 6/7: PaintRadialGradient / PaintVarRadialGradient
            case 6:
            {
                uint colorLineOff = reader.ReadOffset24();
                ColorLine line = LoadColorLineAt(reader, paintOffset + colorLineOff, caches);
                short x0 = reader.ReadFWORD();
                short y0 = reader.ReadFWORD();
                ushort r0 = reader.ReadUFWORD();
                short x1 = reader.ReadFWORD();
                short y1 = reader.ReadFWORD();
                ushort r1 = reader.ReadUFWORD();
                result = new PaintRadialGradient
                {
                    Format = format,
                    ColorLine = line,
                    X0 = x0,
                    Y0 = y0,
                    Radius0 = r0,
                    X1 = x1,
                    Y1 = y1,
                    Radius1 = r1
                };
                break;
            }

            case 7:
            {
                uint colorLineOff = reader.ReadOffset24();
                VarColorLine line = LoadVarColorLineAt(reader, paintOffset + colorLineOff, caches);
                short x0 = reader.ReadFWORD();
                short y0 = reader.ReadFWORD();
                ushort r0 = reader.ReadUFWORD();
                short x1 = reader.ReadFWORD();
                short y1 = reader.ReadFWORD();
                ushort r1 = reader.ReadUFWORD();
                uint varBase = reader.ReadUInt32();
                result = new PaintVarRadialGradient
                {
                    Format = format,
                    ColorLine = line,
                    X0 = x0,
                    Y0 = y0,
                    Radius0 = r0,
                    X1 = x1,
                    Y1 = y1,
                    Radius1 = r1,
                    VarIndexBase = varBase
                };
                break;
            }

            // 8/9: PaintSweepGradient / PaintVarSweepGradient
            case 8:
            {
                uint colorLineOff = reader.ReadOffset24();
                ColorLine line = LoadColorLineAt(reader, paintOffset + colorLineOff, caches);
                short cx = reader.ReadFWORD();
                short cy = reader.ReadFWORD();
                float start = reader.ReadF2Dot14();
                float end = reader.ReadF2Dot14();
                result = new PaintSweepGradient
                {
                    Format = format,
                    ColorLine = line,
                    CenterX = cx,
                    CenterY = cy,
                    StartAngle = start,
                    EndAngle = end
                };
                break;
            }

            case 9:
            {
                uint colorLineOff = reader.ReadOffset24();
                VarColorLine line = LoadVarColorLineAt(reader, paintOffset + colorLineOff, caches);
                short cx = reader.ReadFWORD();
                short cy = reader.ReadFWORD();
                float start = reader.ReadF2Dot14();
                float end = reader.ReadF2Dot14();
                uint varBase = reader.ReadUInt32();
                result = new PaintVarSweepGradient
                {
                    Format = format,
                    ColorLine = line,
                    CenterX = cx,
                    CenterY = cy,
                    StartAngle = start,
                    EndAngle = end,
                    VarIndexBase = varBase
                };
                break;
            }

            // 10: PaintGlyph
            case 10:
            {
                uint childOff = reader.ReadOffset24();
                ushort gid = reader.ReadUInt16();
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);
                result = new PaintGlyph { Format = format, Child = child, GlyphId = gid };
                break;
            }

            // 11: PaintColrGlyph
            case 11:
            {
                ushort gid = reader.ReadUInt16();
                result = new PaintColrGlyph { Format = format, GlyphId = gid };

                // Note: resolution of gid->root paint happens elsewhere when you interpret.
                break;
            }

            // 12/13: PaintTransform / PaintVarTransform
            case 12:
            {
                uint childOff = reader.ReadOffset24();
                uint transformOff = reader.ReadOffset24();

                Affine2x3 m = ReadAffine2x3At(reader, paintOffset + transformOff, caches);
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);
                result = new PaintTransform { Format = format, Child = child, Transform = m };
                break;
            }

            case 13:
            {
                uint childOff = reader.ReadOffset24();
                uint transformOff = reader.ReadOffset24();

                VarAffine2x3 vm = ReadVarAffine2x3At(reader, paintOffset + transformOff, caches);
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);
                result = new PaintVarTransform { Format = format, Child = child, Transform = vm };
                break;
            }

            // 14/15: PaintTranslate / PaintVarTranslate
            case 14:
            {
                uint childOff = reader.ReadOffset24();
                short dx = reader.ReadFWORD();
                short dy = reader.ReadFWORD();
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);
                result = new PaintTranslate { Format = format, Child = child, Dx = dx, Dy = dy };
                break;
            }

            case 15:
            {
                uint childOff = reader.ReadOffset24();
                short dx = reader.ReadFWORD();
                short dy = reader.ReadFWORD();
                uint varBase = reader.ReadUInt32();
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);
                result = new PaintVarTranslate { Format = format, Child = child, Dx = dx, Dy = dy, VarIndexBase = varBase };
                break;
            }

            // 16/17/18/19/20/21/22/23: Scale variants
            case 16: // PaintScale
            case 17: // PaintVarScale
            case 18: // PaintScaleAroundCenter
            case 19: // PaintVarScaleAroundCenter
            case 20: // PaintScaleUniform
            case 21: // PaintVarScaleUniform
            case 22: // PaintScaleUniformAroundCenter
            case 23: // PaintVarScaleUniformAroundCenter
            {
                bool aroundCenter = format is 18 or 19 or 22 or 23;
                bool uniform = format is 20 or 21 or 22 or 23;
                bool isVar = (format % 2) == 1;

                uint childOff = reader.ReadOffset24();
                float sx = reader.ReadF2Dot14();
                float sy = uniform ? sx : reader.ReadF2Dot14();

                short cx = 0, cy = 0;
                if (aroundCenter)
                {
                    cx = reader.ReadFWORD();
                    cy = reader.ReadFWORD();
                }

                uint varBase = isVar ? reader.ReadUInt32() : 0;
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);

                if (isVar)
                {
                    result = new PaintVarScale
                    {
                        Format = format,
                        Child = child,
                        ScaleX = sx,
                        ScaleY = sy,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter,
                        Uniform = uniform,
                        VarIndexBase = varBase
                    };
                }
                else
                {
                    result = new PaintScale
                    {
                        Format = format,
                        Child = child,
                        ScaleX = sx,
                        ScaleY = sy,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter,
                        Uniform = uniform
                    };
                }

                break;
            }

            // 24/25/26/27: Rotate variants
            case 24: // PaintRotate
            case 25: // PaintVarRotate
            case 26: // PaintRotateAroundCenter
            case 27: // PaintVarRotateAroundCenter
            {
                bool aroundCenter = format is 26 or 27;
                bool isVar = (format % 2) == 1;

                uint childOff = reader.ReadOffset24();
                float angle = reader.ReadF2Dot14();

                short cx = 0, cy = 0;
                if (aroundCenter)
                {
                    cx = reader.ReadFWORD();
                    cy = reader.ReadFWORD();
                }

                uint varBase = isVar ? reader.ReadUInt32() : 0;
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);

                if (isVar)
                {
                    result = new PaintVarRotate
                    {
                        Format = format,
                        Child = child,
                        Angle = angle,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter,
                        VarIndexBase = varBase
                    };
                }
                else
                {
                    result = new PaintRotate
                    {
                        Format = format,
                        Child = child,
                        Angle = angle,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter
                    };
                }

                break;
            }

            // 28/29/30/31: Skew variants
            case 28: // PaintSkew
            case 29: // PaintVarSkew
            case 30: // PaintSkewAroundCenter
            case 31: // PaintVarSkewAroundCenter
            {
                bool aroundCenter = format is 30 or 31;
                bool isVar = (format % 2) == 1;

                uint childOff = reader.ReadOffset24();
                float xskew = reader.ReadF2Dot14();
                float yskew = reader.ReadF2Dot14();

                short cx = 0, cy = 0;
                if (aroundCenter)
                {
                    cx = reader.ReadFWORD();
                    cy = reader.ReadFWORD();
                }

                uint varBase = isVar ? reader.ReadUInt32() : 0;
                Paint child = LoadPaintAt(reader, paintOffset + childOff, layerList, caches);

                if (isVar)
                {
                    result = new PaintVarSkew
                    {
                        Format = format,
                        Child = child,
                        XSkew = xskew,
                        YSkew = yskew,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter,
                        VarIndexBase = varBase
                    };
                }
                else
                {
                    result = new PaintSkew
                    {
                        Format = format,
                        Child = child,
                        XSkew = xskew,
                        YSkew = yskew,
                        CenterX = cx,
                        CenterY = cy,
                        AroundCenter = aroundCenter
                    };
                }

                break;
            }

            // 32: Composite
            case 32:
            {
                uint srcOff = reader.ReadOffset24();
                ColrCompositeMode mode = reader.ReadByte<ColrCompositeMode>();
                uint backOff = reader.ReadOffset24();

                Paint src = LoadPaintAt(reader, paintOffset + srcOff, layerList, caches);
                Paint back = LoadPaintAt(reader, paintOffset + backOff, layerList, caches);
                result = new PaintComposite { Format = format, CompositeMode = mode, Source = src, Backdrop = back };
                break;
            }

            default:
                // Unknown format -> treat as no-op solid (or throw). We'll store a stub.
                result = new PaintSolid { Format = format, PaletteIndex = 0, Alpha = 0 };
                break;
        }

        caches.PaintCache[paintOffset] = result;
        reader.BaseStream.Position = restore;
        return result;
    }

    /// <summary>
    /// Loads a <see cref="ColorLine"/> from the specified offset, using the cache to avoid redundant reads.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="offset">The COLR-relative offset of the color line.</param>
    /// <param name="caches">The shared caches.</param>
    /// <returns>The loaded color line.</returns>
    private static ColorLine LoadColorLineAt(BigEndianBinaryReader reader, uint offset, PaintCaches caches)
    {
        if (caches.ColorLineCache.TryGetValue(offset, out ColorLine? line))
        {
            return line;
        }

        long restore = reader.BaseStream.Position;
        reader.Seek(offset, SeekOrigin.Begin);

        line = ColorLine.Load(reader);
        caches.ColorLineCache[offset] = line;

        reader.BaseStream.Position = restore;

        return line;
    }

    /// <summary>
    /// Loads a <see cref="VarColorLine"/> from the specified offset, using the cache to avoid redundant reads.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="offset">The COLR-relative offset of the variable color line.</param>
    /// <param name="caches">The shared caches.</param>
    /// <returns>The loaded variable color line.</returns>
    private static VarColorLine LoadVarColorLineAt(BigEndianBinaryReader reader, uint offset, PaintCaches caches)
    {
        if (caches.VarColorLineCache.TryGetValue(offset, out VarColorLine? line))
        {
            return line;
        }

        long restore = reader.BaseStream.Position;
        reader.Seek(offset, SeekOrigin.Begin);

        line = VarColorLine.Load(reader);
        caches.VarColorLineCache[offset] = line;

        reader.BaseStream.Position = restore;
        return line;
    }

    /// <summary>
    /// Reads an <see cref="Affine2x3"/> matrix from the specified offset, using the cache to avoid redundant reads.
    /// Matrix values are stored as Fixed 16.16 numbers.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="offset">The COLR-relative offset of the affine matrix.</param>
    /// <param name="caches">The shared caches.</param>
    /// <returns>The loaded affine matrix.</returns>
    private static Affine2x3 ReadAffine2x3At(BigEndianBinaryReader reader, uint offset, PaintCaches caches)
    {
        if (caches.AffineCache.TryGetValue(offset, out Affine2x3 m))
        {
            return m;
        }

        long restore = reader.BaseStream.Position;
        reader.Seek(offset, SeekOrigin.Begin);

        float xx = reader.ReadFixed();
        float yx = reader.ReadFixed();
        float xy = reader.ReadFixed();
        float yy = reader.ReadFixed();
        float dx = reader.ReadFixed();
        float dy = reader.ReadFixed();

        m = new Affine2x3(xx, yx, xy, yy, dx, dy);
        caches.AffineCache[offset] = m;

        reader.BaseStream.Position = restore;
        return m;
    }

    /// <summary>
    /// Reads a <see cref="VarAffine2x3"/> matrix from the specified offset, using the cache to avoid redundant reads.
    /// Matrix values are stored as Fixed 16.16 numbers with an appended variation index base.
    /// </summary>
    /// <param name="reader">The binary reader.</param>
    /// <param name="offset">The COLR-relative offset of the variable affine matrix.</param>
    /// <param name="caches">The shared caches.</param>
    /// <returns>The loaded variable affine matrix.</returns>
    private static VarAffine2x3 ReadVarAffine2x3At(BigEndianBinaryReader reader, uint offset, PaintCaches caches)
    {
        if (caches.VarAffineCache.TryGetValue(offset, out VarAffine2x3 m))
        {
            return m;
        }

        long restore = reader.BaseStream.Position;
        reader.Seek(offset, SeekOrigin.Begin);

        float xx = reader.ReadFixed();
        float yx = reader.ReadFixed();
        float xy = reader.ReadFixed();
        float yy = reader.ReadFixed();
        float dx = reader.ReadFixed();
        float dy = reader.ReadFixed();
        uint varBase = reader.ReadUInt32();

        m = new VarAffine2x3(xx, yx, xy, yy, dx, dy, varBase);
        caches.VarAffineCache[offset] = m;

        reader.BaseStream.Position = restore;
        return m;
    }
}

/// <summary>
/// Holds per-load caches used during COLR table parsing to deduplicate paint objects,
/// color lines, and affine matrices that may be referenced from multiple offsets.
/// </summary>
internal sealed class PaintCaches
{
    /// <summary>
    /// Gets the cache of paint objects keyed by their COLR-relative offset.
    /// </summary>
    public Dictionary<uint, Paint> PaintCache { get; } = [];

    /// <summary>
    /// Gets the cache of color lines keyed by their COLR-relative offset.
    /// </summary>
    public Dictionary<uint, ColorLine> ColorLineCache { get; } = [];

    /// <summary>
    /// Gets the cache of variable color lines keyed by their COLR-relative offset.
    /// </summary>
    public Dictionary<uint, VarColorLine> VarColorLineCache { get; } = [];

    /// <summary>
    /// Gets the cache of affine matrices keyed by their COLR-relative offset.
    /// </summary>
    public Dictionary<uint, Affine2x3> AffineCache { get; } = [];

    /// <summary>
    /// Gets the cache of variable affine matrices keyed by their COLR-relative offset.
    /// </summary>
    public Dictionary<uint, VarAffine2x3> VarAffineCache { get; } = [];
}

/// <summary>
/// Represents a resolved COLR v1 glyph layer produced by flattening the paint DAG.
/// Associates a glyph ID with its paint node, geometry transform, paint transform, composite mode, and optional clip box.
/// </summary>
#pragma warning disable SA1201 // Elements should appear in the correct order
[DebuggerDisplay("Id: {GlyphId}")]
internal readonly struct ResolvedGlyphLayer
#pragma warning restore SA1201 // Elements should appear in the correct order
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolvedGlyphLayer"/> struct.
    /// </summary>
    /// <param name="id">The glyph ID whose outline this layer paints.</param>
    /// <param name="paint">The leaf paint node for this layer.</param>
    /// <param name="glyphTransform">The accumulated affine transform applied to glyph geometry.</param>
    /// <param name="paintTransform">The accumulated affine transform applied to the leaf paint.</param>
    /// <param name="mode">The composite mode to apply.</param>
    /// <param name="clipBox">The optional clip box bounds, or <see langword="null"/>.</param>
    public ResolvedGlyphLayer(ushort id, Paint paint, Matrix3x2 glyphTransform, Matrix3x2 paintTransform, CompositeMode mode, Bounds? clipBox)
    {
        this.GlyphId = id;
        this.Paint = paint;
        this.GlyphTransform = glyphTransform;
        this.PaintTransform = paintTransform;
        this.CompositeMode = mode;
        this.ClipBox = clipBox;
    }

    /// <summary>
    /// Gets the glyph ID whose outline this layer paints.
    /// </summary>
    public ushort GlyphId { get; }

    /// <summary>
    /// Gets the leaf paint node for this layer.
    /// </summary>
    public Paint Paint { get; }

    /// <summary>
    /// Gets the accumulated affine transform applied to glyph geometry.
    /// </summary>
    public Matrix3x2 GlyphTransform { get; }

    /// <summary>
    /// Gets the accumulated affine transform applied to the leaf paint.
    /// </summary>
    public Matrix3x2 PaintTransform { get; }

    /// <summary>
    /// Gets the composite mode to apply when rendering this layer.
    /// </summary>
    public CompositeMode CompositeMode { get; }

    /// <summary>
    /// Gets the optional clip box bounds for this layer, or <see langword="null"/> if no clip applies.
    /// </summary>
    public Bounds? ClipBox { get; }
}
