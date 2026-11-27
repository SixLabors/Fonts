// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.AdvancedTypographic.GSub;

/// <summary>
/// The headers of the GSUB and GPOS tables contain offsets to Lookup List tables (LookupList) for
/// glyph substitution (GSUB table) and glyph positioning (GPOS table). The LookupList table contains
/// an array of offsets to Lookup tables (lookupOffsets).
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-list-table"/>
/// </summary>
internal sealed class LookupListTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupListTable"/> class.
    /// </summary>
    /// <param name="lookupCount">The number of lookups in this table.</param>
    /// <param name="lookupTables">The array of lookup tables.</param>
    private LookupListTable(ushort lookupCount, LookupTable[] lookupTables)
    {
        this.LookupCount = lookupCount;
        this.LookupTables = lookupTables;
    }

    /// <summary>
    /// Gets the number of lookups in this table.
    /// </summary>
    public ushort LookupCount { get; }

    /// <summary>
    /// Gets the array of lookup tables.
    /// </summary>
    public LookupTable[] LookupTables { get; }

    /// <summary>
    /// Loads the <see cref="LookupListTable"/> from the binary reader at the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the lookup list table.</param>
    /// <returns>The loaded <see cref="LookupListTable"/>.</returns>
    public static LookupListTable Load(BigEndianBinaryReader reader, long offset)
    {
        // +----------+----------------------------+---------------------------------------------------------------+
        // | Type     | Name                       | Description                                                   |
        // +==========+============================+===============================================================+
        // | uint16   | lookupCount                | Number of lookups in this table                               |
        // +----------+----------------------------+---------------------------------------------------------------+
        // | Offset16 | lookupOffsets[lookupCount] | Array of offsets to Lookup tables, from beginning             |
        // |          |                            | of LookupList — zero based (first lookup is Lookup index = 0) |
        // +----------+----------------------------+---------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort lookupCount = reader.ReadUInt16();
        using Buffer<ushort> lookupOffsetsBuffer = new(lookupCount);
        Span<ushort> lookupOffsets = lookupOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(lookupOffsets);

        LookupTable[] lookupTables = new LookupTable[lookupCount];

        for (int i = 0; i < lookupTables.Length; i++)
        {
            lookupTables[i] = LookupTable.Load(reader, offset + lookupOffsets[i]);
        }

        return new LookupListTable(lookupCount, lookupTables);
    }
}

/// <summary>
/// A Lookup table (Lookup) defines the specific conditions, type, and results of a substitution
/// or positioning action that is used to implement a feature. For example, a substitution
/// operation requires a list of target glyph indices to be replaced, a list of replacement glyph
/// indices, and a description of the type of substitution action.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#lookup-table"/>
/// </summary>
internal sealed class LookupTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupTable"/> class.
    /// </summary>
    /// <param name="lookupType">The lookup type identifying the kind of substitution.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <param name="lookupSubTables">The array of lookup subtables.</param>
    private LookupTable(
        ushort lookupType,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        LookupSubTable[] lookupSubTables)
    {
        this.LookupType = lookupType;
        this.LookupFlags = lookupFlags;
        this.MarkFilteringSet = markFilteringSet;
        this.LookupSubTables = lookupSubTables;
        this.IsReverse = lookupType == 8 || (lookupType == 7 && lookupSubTables.Length > 0 && lookupSubTables[0].IsReverse);

        // The union of every subtable's gating coverage: a glyph outside the digest
        // cannot be affected by any subtable of this lookup, so application skips it
        // without touching the subtables. See GlyphSetDigest for the accuracy contract.
        GlyphSetDigest digest = default;
        for (int i = 0; i < lookupSubTables.Length; i++)
        {
            // Each subtable also carries its own digest so application can skip
            // subtables whose gating coverage cannot contain the current glyph
            // without paying the virtual probe. Contextual formats that expose no
            // leading coverage flood their digest and therefore always pass.
            GlyphSetDigest subTableDigest = default;
            lookupSubTables[i].CollectDigest(ref subTableDigest);
            lookupSubTables[i].Digest = subTableDigest;
            lookupSubTables[i].CollectDigest(ref digest);
        }

        this.Digest = digest;
    }

    /// <summary>
    /// Gets the lookup type, which determines the kind of substitution performed.
    /// </summary>
    public ushort LookupType { get; }

    /// <summary>
    /// Gets the lookup qualifiers flags that control filtering of glyphs during lookup.
    /// </summary>
    public LookupFlags LookupFlags { get; }

    /// <summary>
    /// Gets the index (base 0) into the GDEF mark glyph sets structure, used when the
    /// <see cref="AdvancedTypographic.LookupFlags.UseMarkFilteringSet"/> flag is set.
    /// </summary>
    public ushort MarkFilteringSet { get; }

    /// <summary>
    /// Gets the array of lookup subtables for this lookup.
    /// </summary>
    public LookupSubTable[] LookupSubTables { get; }

    /// <summary>
    /// Gets the approximate membership filter for the glyphs this lookup can affect.
    /// </summary>
    public GlyphSetDigest Digest { get; }

    /// <summary>
    /// Gets a value indicating whether this lookup applies from the end of the glyph sequence to the beginning.
    /// </summary>
    public bool IsReverse { get; }

    /// <summary>
    /// Loads the <see cref="LookupTable"/> from the binary reader at the given offset.
    /// </summary>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the lookup table.</param>
    /// <returns>The loaded <see cref="LookupTable"/>.</returns>
    public static LookupTable Load(BigEndianBinaryReader reader, long offset)
    {
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | Type     | Name                           | Description                                                 |
        // +==========+================================+=============================================================+
        // | uint16   | lookupType                     | Different enumerations for GSUB and GPOS                    |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | lookupFlag                     | Lookup qualifiers                                           |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | subTableCount                  | Number of subtables for this lookup                         |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | Offset16 | subtableOffsets[subTableCount] | Array of offsets to lookup subtables, from beginning of     |
        // |          |                                | Lookup table                                                |
        // +----------+--------------------------------+-------------------------------------------------------------+
        // | uint16   | markFilteringSet               | Index (base 0) into GDEF mark glyph sets structure.         |
        // |          |                                | This field is only present if the USE\_MARK\_FILTERING\_SET |
        // |          |                                | lookup flag is set.                                         |
        // +----------+--------------------------------+-------------------------------------------------------------+
        reader.Seek(offset, SeekOrigin.Begin);

        ushort lookupType = reader.ReadUInt16();
        LookupFlags lookupFlags = reader.ReadUInt16<LookupFlags>();
        ushort subTableCount = reader.ReadUInt16();

        // An unknown lookup type has no subtables that GSUB can interpret.
        // Keep its slot so that feature lookup indices stay valid.
        if (lookupType is < 1 or > 8)
        {
            return new LookupTable(lookupType, lookupFlags, 0, []);
        }

        bool hasMarkFilteringSet = (lookupFlags & LookupFlags.UseMarkFilteringSet) != 0;
        long tableEnd = reader.BaseStream.Length;

        // Validate the offset array and the optional mark filtering field as
        // a unit before allocating storage or reading beyond GSUB.
        int offsetBytes = sizeof(ushort) * (subTableCount + (hasMarkFilteringSet ? 1 : 0));
        if (offsetBytes > tableEnd - reader.BaseStream.Position)
        {
            return new LookupTable(lookupType, lookupFlags, 0, []);
        }

        using Buffer<ushort> subTableOffsetsBuffer = new(subTableCount);
        Span<ushort> subTableOffsets = subTableOffsetsBuffer.GetSpan();
        reader.ReadUInt16Array(subTableOffsets);

        // The fifth bit indicates the presence of a MarkFilteringSet field in the Lookup table.
        ushort markFilteringSet = hasMarkFilteringSet
            ? reader.ReadUInt16()
            : (ushort)0;

        LookupSubTable[] lookupSubTables = new LookupSubTable[subTableCount];

        for (int i = 0; i < lookupSubTables.Length; i++)
        {
            // Every supported subtable begins with a 16-bit format field.
            long subtableOffset = offset + subTableOffsets[i];
            lookupSubTables[i] = subtableOffset <= tableEnd - reader.StartOfStream - sizeof(ushort)
                ? LoadLookupSubTable(lookupType, lookupFlags, markFilteringSet, reader, subtableOffset)
                : new NotImplementedSubTable();
        }

        return new LookupTable(lookupType, lookupFlags, markFilteringSet, lookupSubTables);
    }

    /// <summary>
    /// Attempts to perform a glyph substitution at the specified index in the buffer.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GSUB table.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="feature">The feature tag to apply.</param>
    /// <param name="lookupMask">The applying lookup's combined mask.</param>
    /// <param name="index">The index in the buffer at which to attempt substitution.</param>
    /// <param name="count">The number of glyphs in the input sequence to consider.</param>
    /// <returns><see langword="true"/> if a substitution was performed; otherwise, <see langword="false"/>.</returns>
    public bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        ShapingBuffer buffer,
        Tag feature,
        uint lookupMask,
        int index,
        int count)
    {
        ushort glyphId = buffer[index].GlyphId;
        foreach (LookupSubTable subTable in this.LookupSubTables)
        {
            // A glyph outside the subtable's digest cannot match its coverage, so the
            // probe (a virtual call, coverage search, or full context-match attempt)
            // is skipped entirely.
            if (!subTable.Digest.MightContain(glyphId))
            {
                continue;
            }

            // Nested applications consume through the pass cursor exactly as
            // top-level ones do; the contextual match brings the cursor to each
            // record before recursing.
            buffer.DirectConsume = subTable.ConsumesDirectly;
            if (subTable.TrySubstitution(fontMetrics, table, buffer, feature, lookupMask, index, count))
            {
                // A lookup is finished for a glyph after the client locates the target
                // glyph or glyph context and performs a substitution, if specified.
                buffer.DirectConsume = false;
                return true;
            }
        }

        buffer.DirectConsume = false;
        return false;
    }

    /// <summary>
    /// Tests whether this lookup would trigger a substitution on the given glyph
    /// sequence without applying it. Shapers use this to probe a feature's effect
    /// while classifying glyphs, before any real substitution runs.
    /// </summary>
    /// <param name="glyphs">The glyph id sequence to query.</param>
    /// <param name="zeroContext">Whether matching against context outside the sequence is disallowed.</param>
    /// <returns><see langword="true"/> if a substitution would be triggered.</returns>
    public bool WouldApply(ReadOnlySpan<ushort> glyphs, bool zeroContext)
    {
        if (glyphs.Length == 0 || !this.Digest.MightContain(glyphs[0]))
        {
            return false;
        }

        foreach (LookupSubTable subTable in this.LookupSubTables)
        {
            if (!subTable.Digest.MightContain(glyphs[0]))
            {
                continue;
            }

            if (subTable.WouldApply(glyphs, zeroContext))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads a lookup subtable based on the lookup type.
    /// </summary>
    /// <param name="lookupType">The lookup type identifying the kind of substitution.</param>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    /// <param name="reader">The big-endian binary reader.</param>
    /// <param name="offset">The offset to the beginning of the subtable.</param>
    /// <returns>The loaded subtable, or an inactive subtable if its data is invalid.</returns>
    private static LookupSubTable LoadLookupSubTable(
        ushort lookupType,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        BigEndianBinaryReader reader,
        long offset)
    {
        try
        {
            return lookupType switch
            {
                1 => LookupType1SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                2 => LookupType2SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                3 => LookupType3SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                4 => LookupType4SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                5 => LookupType5SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                6 => LookupType6SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                7 => LookupType7SubTable.Load(reader, offset, lookupFlags, markFilteringSet, LoadLookupSubTable),
                8 => LookupType8SubTable.Load(reader, offset, lookupFlags, markFilteringSet),
                _ => new NotImplementedSubTable(),
            };
        }
        catch (Exception exception) when (exception is EndOfStreamException or InvalidFontFileException)
        {
            // A nested offset can lead to invalid data. Each sibling
            // subtable starts at its own offset and can still be loaded.
            return new NotImplementedSubTable();
        }
    }
}

/// <summary>
/// Base class for all GSUB lookup subtables. Each subtable implements a specific
/// type of glyph substitution logic.
/// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub"/>
/// </summary>
internal abstract class LookupSubTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LookupSubTable"/> class.
    /// </summary>
    /// <param name="lookupFlags">The lookup qualifiers flags.</param>
    /// <param name="markFilteringSet">The index into the GDEF mark glyph sets structure.</param>
    protected LookupSubTable(LookupFlags lookupFlags, ushort markFilteringSet)
    {
        this.LookupFlags = lookupFlags;
        this.MarkFilteringSet = markFilteringSet;
    }

    /// <summary>
    /// Gets the lookup qualifiers flags that control filtering of glyphs during lookup.
    /// </summary>
    public LookupFlags LookupFlags { get; }

    /// <summary>
    /// Gets the index (base 0) into the GDEF mark glyph sets structure.
    /// </summary>
    public ushort MarkFilteringSet { get; }

    /// <summary>
    /// Gets a value indicating whether the subtable's replacements consume exactly
    /// the records they match, so top-level application may produce them through
    /// the pass cursor. Contextual and reverse subtables leave this false: their
    /// replacements run in place and the driver alone advances the cursor.
    /// </summary>
    public virtual bool ConsumesDirectly => false;

    /// <summary>
    /// Gets a value indicating whether the owning lookup applies from the end of the glyph sequence to the beginning.
    /// </summary>
    public virtual bool IsReverse => false;

    /// <summary>
    /// Gets or sets the approximate membership filter for the glyphs this subtable can
    /// affect. Assigned once by the owning <see cref="LookupTable"/> during construction.
    /// </summary>
    public GlyphSetDigest Digest { get; internal set; }

    /// <summary>
    /// Adds the coverage that gates this subtable's applicability to the digest.
    /// The default adds every glyph so the lookup is always attempted, the correct
    /// conservative behavior for subtables whose gating coverage is unknown.
    /// </summary>
    /// <param name="digest">The digest to add to.</param>
    public virtual void CollectDigest(ref GlyphSetDigest digest) => digest.AddAll();

    /// <summary>
    /// Attempts to perform a glyph substitution at the specified index in the buffer.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GSUB table.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="feature">The feature tag to apply.</param>
    /// <param name="lookupMask">The applying lookup's combined mask.</param>
    /// <param name="index">The index in the buffer at which to attempt substitution.</param>
    /// <param name="count">The number of glyphs in the input sequence to consider.</param>
    /// <returns><see langword="true"/> if a substitution was performed; otherwise, <see langword="false"/>.</returns>
    public abstract bool TrySubstitution(
        FontMetrics fontMetrics,
        GSubTable table,
        ShapingBuffer buffer,
        Tag feature,
        uint lookupMask,
        int index,
        int count);

    /// <summary>
    /// Tests whether this subtable would trigger a substitution on the given glyph
    /// sequence without applying it. The first glyph anchors the match and the
    /// sequence length must account for every matched glyph; context outside the
    /// sequence is consulted only when <paramref name="zeroContext"/> permits it.
    /// </summary>
    /// <param name="glyphs">The glyph id sequence to query.</param>
    /// <param name="zeroContext">Whether matching against context outside the sequence is disallowed.</param>
    /// <returns><see langword="true"/> if a substitution would be triggered.</returns>
    public abstract bool WouldApply(ReadOnlySpan<ushort> glyphs, bool zeroContext);
}
