// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;

namespace SixLabors.Fonts.Tables.General.Colr;

internal class ColrTable : Table
{
    internal const string TableName = "COLR";

    // v0
    private readonly BaseGlyphRecord[] glyphRecords;
    private readonly LayerRecord[] layers;

    // v1 (nullable if not present)
    private readonly BaseGlyphList? baseGlyphList;
    private readonly LayerList? layerList;
    private readonly ClipList? clipList;

    // Caches (offset -> resolved object)
    private readonly Dictionary<uint, Paint>? paintCache;

    public ColrTable(
        BaseGlyphRecord[] glyphRecords,
        LayerRecord[] layers)
        : this(glyphRecords, layers, null, null, null, null, 0)
    {
    }

    public ColrTable(
        BaseGlyphRecord[] glyphRecords,
        LayerRecord[] layers,
        BaseGlyphList? baseGlyphList,
        LayerList? layerList,
        ClipList? clipList,
        Dictionary<uint, Paint>? paintCache = null,
        int version = 1)
    {
        this.glyphRecords = glyphRecords;
        this.layers = layers;
        this.baseGlyphList = baseGlyphList;
        this.layerList = layerList;
        this.clipList = clipList;
        this.paintCache = paintCache;
        this.Version = version;
    }

    public int Version { get; }

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
    /// <param name="layers">
    /// When this method returns, contains a list of resolved glyph layers if the operation succeeds; otherwise,
    /// <see langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns>
    /// <see langword="true"/> if the color glyph layers were successfully resolved; otherwise, <see langword="false"/>.
    /// </returns>
    internal bool TryGetColrV1Layers(ushort glyphId, [NotNullWhen(true)] out List<ResolvedGlyphLayer>? layers)
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
        this.FlattenPaintToLayers(root, null, Matrix3x2.Identity, CompositeMode.SrcOver, acc);

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
    /// <param name="outLayers">Accumulator for resolved layers.</param>
    private void FlattenPaintToLayers(
        Paint node,
        ushort? currentGlyphId,
        Matrix3x2 transform,
        CompositeMode compositeMode,
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
                        this.FlattenPaintToLayers(child, currentGlyphId, transform, compositeMode, outLayers);
                    }
                }

                return;
            }

            case PaintColrGlyph pcg:
            {
                // Resolve the referenced glyph's root paint and recurse under that glyph id.
                if (this.TryGetRootPaintOffset(pcg.GlyphId, out uint off) && off != 0
                    && this.paintCache!.TryGetValue(off, out Paint? colrRoot) && colrRoot is not null)
                {
                    this.FlattenPaintToLayers(colrRoot, pcg.GlyphId, transform, compositeMode, outLayers);
                }

                return;
            }

            case PaintGlyph pg:
            {
                // Bind geometry to the specified glyph id and recurse into its child paint.
                this.FlattenPaintToLayers(pg.Child, pg.GlyphId, transform, compositeMode, outLayers);
                return;
            }

            // ---------------------------
            // Wrappers: forward glyph id
            // ---------------------------
            case PaintTransform pt:
            {
                transform *= ToMatrix(pt.Transform);
                this.FlattenPaintToLayers(pt.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintVarTransform pvt:
            {
                transform *= ToMatrix(pvt.Transform);
                this.FlattenPaintToLayers(pvt.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintTranslate t:
            {
                transform *= Matrix3x2.CreateTranslation(t.Dx, t.Dy);
                this.FlattenPaintToLayers(t.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintVarTranslate vt:
            {
                transform *= Matrix3x2.CreateTranslation(vt.Dx, vt.Dy);
                this.FlattenPaintToLayers(vt.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintScale s:
            {
                transform *= BuildScale(s.ScaleX, s.ScaleY, s.AroundCenter, s.CenterX, s.CenterY);
                this.FlattenPaintToLayers(s.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintVarScale vs:
            {
                transform *= BuildScale(vs.ScaleX, vs.ScaleY, vs.AroundCenter, vs.CenterX, vs.CenterY);
                this.FlattenPaintToLayers(vs.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintRotate r:
            {
                transform *= BuildRotate(r.Angle, r.AroundCenter, r.CenterX, r.CenterY);
                this.FlattenPaintToLayers(r.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintVarRotate vr:
            {
                transform *= BuildRotate(vr.Angle, vr.AroundCenter, vr.CenterX, vr.CenterY);
                this.FlattenPaintToLayers(vr.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintSkew k:
            {
                transform *= BuildSkew(k.XSkew, k.YSkew, k.AroundCenter, k.CenterX, k.CenterY);
                this.FlattenPaintToLayers(k.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintVarSkew vk:
            {
                transform *= BuildSkew(vk.XSkew, vk.YSkew, vk.AroundCenter, vk.CenterX, vk.CenterY);
                this.FlattenPaintToLayers(vk.Child, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            case PaintComposite comp:
            {
                compositeMode = MapCompositeMode(comp.CompositeMode);

                // Backdrop first, then Source. Both inherit the current glyph id.
                this.FlattenPaintToLayers(comp.Backdrop, currentGlyphId, transform, compositeMode, outLayers);
                this.FlattenPaintToLayers(comp.Source, currentGlyphId, transform, compositeMode, outLayers);
                return;
            }

            // ---------------------------
            // Leaves: emit only if bound
            // ---------------------------
            case PaintSolid:
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
                    _ = this.TryGetClipBox(currentGlyphId.Value, out Bounds? clip);
                    outLayers.Add(new ResolvedGlyphLayer(currentGlyphId.Value, node, transform, compositeMode, clip));
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
    /// Maps an optional fixed 2×3 affine to <see cref="Matrix3x2"/>.
    /// Layout:
    ///   [ xx  xy  dx ]
    ///   [ yx  yy  dy ]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 ToMatrix(Affine2x3? affine)
    {
        if (affine.HasValue)
        {
            Affine2x3 a = affine.Value;
            return new Matrix3x2(a.Xx, a.Yx, a.Xy, a.Yy, a.Dx, a.Dy); // (M11, M12, M21, M22, M31, M32)
        }

        return Matrix3x2.Identity;
    }

    /// <summary>
    /// Maps an optional variable 2×3 affine to <see cref="Matrix3x2"/>.
    /// Layout:
    ///   [ xx  xy  dx ]
    ///   [ yx  yy  dy ]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 ToMatrix(VarAffine2x3? varAffine)
    {
        if (varAffine.HasValue)
        {
            VarAffine2x3 v = varAffine.Value;
            return new Matrix3x2(v.Xx, v.Yx, v.Xy, v.Yy, v.Dx, v.Dy);
        }

        return Matrix3x2.Identity;
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

        // T(c) * S * T(-c)
        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 s = Matrix3x2.CreateScale(sx, sy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * s * t1;
    }

    /// <summary>
    /// Builds a rotation matrix in radians-degrees as provided, optionally around a center.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildRotate(float angleRadiansOrDegrees, bool aroundCenter, float cx, float cy)
    {
        // Input is already bias-adjusted per your loader; assume radians if that’s how you parsed it.
        Matrix3x2 r = Matrix3x2.CreateRotation(angleRadiansOrDegrees);
        if (!aroundCenter)
        {
            return r;
        }

        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * r * t1;
    }

    /// <summary>
    /// Builds a skew matrix, optionally around a center. Angles are in radians.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Matrix3x2 BuildSkew(float xSkew, float ySkew, bool aroundCenter, float cx, float cy)
    {
        float tx = MathF.Tan(xSkew);
        float ty = MathF.Tan(ySkew);

        // Skew X then Y: Ky * Kx
        Matrix3x2 kx = new(1, 0, tx, 1, 0, 0); // x' = x + y*tx
        Matrix3x2 ky = new(1, ty, 0, 1, 0, 0); // y' = y + x*ty
        Matrix3x2 k = kx * ky;

        if (!aroundCenter)
        {
            return k;
        }

        Matrix3x2 t0 = Matrix3x2.CreateTranslation(-cx, -cy);
        Matrix3x2 t1 = Matrix3x2.CreateTranslation(cx, cy);
        return t0 * k * t1;
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
            ColrCompositeMode.Dst => CompositeMode.Dst,
            ColrCompositeMode.SrcOver => CompositeMode.SrcOver,
            ColrCompositeMode.DstOver => CompositeMode.DstOver,
            ColrCompositeMode.SrcIn => CompositeMode.SrcIn,
            ColrCompositeMode.DstIn => CompositeMode.DstIn,
            ColrCompositeMode.SrcOut => CompositeMode.SrcOut,
            ColrCompositeMode.DstOut => CompositeMode.DstOut,
            ColrCompositeMode.SrcAtop => CompositeMode.SrcAtop,
            ColrCompositeMode.DstAtop => CompositeMode.DstAtop,
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

    private bool TryGetClipBox(ushort glyphId, out Bounds? bounds)
    {
        if (this.clipList is null)
        {
            bounds = default;
            return false;
        }

        // TODO: support variation resolver
        return this.clipList.TryGetClipBox(glyphId, null, out bounds);
    }

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

            // varIndexMapOffset / itemVariationStoreOffset are parsed elsewhere if/when needed.
            _ = varIndexMapOffset;
            _ = itemVariationStoreOffset;

            paintCache = LoadPaintRoots(reader, baseGlyphList, layerList);
        }

        return new ColrTable(glyphs, layerRecs, baseGlyphList, layerList, clipList, paintCache, 1);
    }

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
            case 16: // ScaleAroundCenter
            case 17: // VarScaleAroundCenter
            case 18: // Scale
            case 19: // VarScale
            case 20: // ScaleUniformAroundCenter
            case 21: // VarScaleUniformAroundCenter
            case 22: // ScaleUniform
            case 23: // VarScaleUniform
            {
                bool aroundCenter = format is 16 or 17 or 20 or 21;
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
            case 24: // RotateAroundCenter
            case 25: // VarRotateAroundCenter
            case 26: // Rotate
            case 27: // VarRotate
            {
                bool aroundCenter = format is 24 or 25;
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
            case 28: // SkewAroundCenter
            case 29: // VarSkewAroundCenter
            case 30: // Skew
            case 31: // VarSkew
            {
                bool aroundCenter = format is 28 or 29;
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

    // Eager ColorLine loaders (offset-based)
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

    // Affine readers (Fixed 16.16)
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

internal sealed class PaintCaches
{
    public Dictionary<uint, Paint> PaintCache { get; } = [];

    public Dictionary<uint, ColorLine> ColorLineCache { get; } = [];

    public Dictionary<uint, VarColorLine> VarColorLineCache { get; } = [];

    public Dictionary<uint, Affine2x3> AffineCache { get; } = [];

    public Dictionary<uint, VarAffine2x3> VarAffineCache { get; } = [];
}

#pragma warning disable SA1201 // Elements should appear in the correct order
[DebuggerDisplay("Id: {GlyphId}")]
internal readonly struct ResolvedGlyphLayer
#pragma warning restore SA1201 // Elements should appear in the correct order
{
    public ResolvedGlyphLayer(ushort id, Paint paint, Matrix3x2 transform, CompositeMode mode, Bounds? clipBox)
    {
        this.GlyphId = id;
        this.Paint = paint;
        this.Transform = transform;
        this.CompositeMode = mode;
        this.ClipBox = clipBox;
    }

    public ushort GlyphId { get; }

    public Paint Paint { get; }

    public Matrix3x2 Transform { get; }

    public CompositeMode CompositeMode { get; }

    public Bounds? ClipBox { get; }
}
