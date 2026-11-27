// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Runtime.InteropServices;
using SixLabors.Fonts.Tables.General;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Aat;

/// <summary>
/// The classic and extended glyph metamorphosis tables substitute, rearrange,
/// and insert glyphs through ordered feature chains.
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6mort.html"/>
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6morx.html"/>
/// </summary>
internal abstract class MorphTable : Table
{
    private readonly Chain[] chains;
    private readonly AatFeatures features;

    /// <summary>
    /// Initializes a new instance of the <see cref="MorphTable"/> class.
    /// </summary>
    /// <param name="chains">The feature chains in application order.</param>
    /// <param name="features">The feature types and language tags in the font.</param>
    protected MorphTable(Chain[] chains, AatFeatures features)
    {
        this.chains = chains;
        this.features = features;
    }

    /// <summary>
    /// Applies AAT substitution to one run after the caller selects its table.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="run">The script, language, features, and orientation of the run.</param>
    /// <returns>The shape plan for positioning the resulting glyphs.</returns>
    public ShapePlan ApplySubstitution(FontMetrics fontMetrics, ShapingBuffer buffer, ScriptItemizer.ShapingRun run)
    {
        ShapePlan plan = buffer.GetOrCreatePlan(run.Script, default, fontMetrics, run.Culture, run.FeatureTags, true);
        int index = buffer.SpanStart;
        int count = buffer.SpanEnd - index;
        bool rtl = buffer[index].Direction == TextDirection.RightToLeft;
        plan.Shaper.Plan(fontMetrics, buffer, index, count, run.IsVertical);

        count = buffer.SpanEnd - index;
        if (count == 0)
        {
            return plan;
        }

        // Text preparation can remove records outside the AAT state stream.
        // Keep owned events so the state indices stay in order.
        AatStateTable.DiscardUnownedDeletions(buffer);
        int first = index + AatStateTable.CountDeletionsBefore(buffer, index, true);
        int end = this.ApplyChains(
            buffer,
            first,
            first + count,
            run.IsVertical,
            rtl,
            run.FeatureTags,
            run.Culture,
            buffer.MaxShapingGlyphCount);

        FinishAatSubstitution(buffer, first, end);

        int length = buffer.SpanEnd - index;
        if (length > 0)
        {
            ScriptItemizer.MarkCursiveTrackingRun(buffer, index, length, run.Script);
        }

        return plan;
    }

    /// <summary>
    /// Loads and bounds each chain before reading its feature records and subtables.
    /// </summary>
    /// <param name="fontReader">The font reader.</param>
    /// <param name="tag">The tag of the substitution table.</param>
    /// <param name="extended">Whether the table uses the extended format.</param>
    /// <returns>The feature chains, or <see langword="null"/> if the table cannot be read.</returns>
    protected static Chain[]? LoadChains(FontReader fontReader, string tag, bool extended)
    {
        int glyphCount = fontReader.GetTable<MaximumProfileTable>().GlyphCount;
        if (!fontReader.TryGetBoundedReaderAtTablePosition(tag, out BigEndianBinaryReader? reader))
        {
            return null;
        }

        using (reader)
        {
            uint tableLength = (uint)reader.BaseStream.Length;

            // +---------+------------+-----------------------------------------+
            // | Type    | Name       | Description                             |
            // +---------+------------+-----------------------------------------+
            // | uint16  | version    | 1 for mort or 2/3 for morx.             |
            // | uint16  | unused     | Reserved.                               |
            // | uint32  | chainCount | Number of chains following this header. |
            // | Chain[] | chains     | Chains in application order.            |
            // +---------+------------+-----------------------------------------+
            if (tableLength < 8)
            {
                return null;
            }

            ushort version = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            uint chainCount = reader.ReadUInt32();
            int chainHeaderSize = extended ? 16 : 12;
            int subtableHeaderSize = extended ? 12 : 8;
            if ((extended ? version is not (2 or 3) : version != 1) ||
                !AatStateTable.Contains(8, (long)chainCount * chainHeaderSize, tableLength))
            {
                return null;
            }

            long offset = 8;
            List<Chain> chains = [];
            int totalSubtables = 0;
            for (int i = 0; i < chainCount; i++)
            {
                if (!AatStateTable.Contains(offset, chainHeaderSize, tableLength))
                {
                    return null;
                }

                // +---------------+---------------+---------------------------------------------+
                // | Type          | Name          | Description                                 |
                // +---------------+---------------+---------------------------------------------+
                // | uint32        | defaultFlags  | Initial mask of enabled subtables.          |
                // | uint32        | chainLength   | Chain byte length, including this header.   |
                // | uint16/uint32 | featureCount  | Number of feature records.                  |
                // | uint16/uint32 | subtableCount | Number of subtables following the features. |
                // | Feature[]     | features      | Feature selectors and their flag changes.   |
                // | Subtable[]    | subtables     | Subtables in application order.             |
                // +---------------+---------------+---------------------------------------------+
                // Counts are uint16 in mort and uint32 in morx.
                reader.Seek(offset, SeekOrigin.Begin);
                uint flags = reader.ReadUInt32();
                uint length = reader.ReadUInt32();
                uint featureCount = extended ? reader.ReadUInt32() : reader.ReadUInt16();
                uint subtableCount = extended ? reader.ReadUInt32() : reader.ReadUInt16();
                long end = offset + length;

                // Limit the number of subtables to bound the work for each run.
                if (!AatStateTable.Contains(offset, length, tableLength) ||
                    chainHeaderSize + (12L * featureCount) + (subtableHeaderSize * (long)subtableCount) > length ||
                    subtableCount > 1024 - totalSubtables)
                {
                    return null;
                }

                totalSubtables += (int)subtableCount;
                Feature[] features = new Feature[featureCount];

                // +--------+--------------+-------------------------------------------------+
                // | Type   | Name         | Description                                     |
                // +--------+--------------+-------------------------------------------------+
                // | uint16 | featureType  | Feature type from the Apple registry.           |
                // | uint16 | selector     | Setting of that feature type.                   |
                // | uint32 | enableFlags  | Bits to enable for this setting.                |
                // | uint32 | disableFlags | Complement of bits to disable for this setting. |
                // +--------+--------------+-------------------------------------------------+
                for (int j = 0; j < features.Length; j++)
                {
                    features[j] = new(reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt32(), reader.ReadUInt32());
                }

                long subtableOffset = offset + chainHeaderSize + (12L * featureCount);
                List<MorphSubTable> subtables = new((int)subtableCount);
                for (int j = 0; j < subtableCount; j++)
                {
                    if (!AatStateTable.Contains(subtableOffset, subtableHeaderSize, end))
                    {
                        return null;
                    }

                    // +---------------+-----------------+---------------------------------------------+
                    // | Type          | Name            | Description                                 |
                    // +---------------+-----------------+---------------------------------------------+
                    // | uint16/uint32 | length          | Byte length including this subtable header. |
                    // | uint16/uint32 | coverage        | Format in low byte, direction in high byte. |
                    // | uint32        | subFeatureFlags | Bits that activate this subtable.           |
                    // +---------------+-----------------+---------------------------------------------+
                    reader.Seek(subtableOffset, SeekOrigin.Begin);
                    uint size = extended ? reader.ReadUInt32() : reader.ReadUInt16();
                    uint coverage = extended ? reader.ReadUInt32() : reader.ReadUInt16();
                    uint subFeatureFlags = reader.ReadUInt32();
                    if (size < subtableHeaderSize || !AatStateTable.Contains(subtableOffset, size, end))
                    {
                        return null;
                    }

                    // The format is in the low byte. The direction flags are in the
                    // high byte of a 16-bit mort field or a 32-bit morx field.
                    int type = (byte)coverage;
                    byte direction = (byte)(coverage >> (extended ? 24 : 8));
                    long subtableEnd = subtableOffset + size;
                    MorphSubTable? subtable = MorphSubTable.Load(
                        reader,
                        subtableOffset + subtableHeaderSize,
                        subtableEnd,
                        extended,
                        glyphCount,
                        type,
                        direction,
                        subFeatureFlags);

                    if (subtable is not null)
                    {
                        subtables.Add(subtable);
                    }

                    subtableOffset = subtableEnd;
                }

                if (subtables.Count > 0)
                {
                    chains.Add(new(flags, features, [.. subtables]));
                }

                // Version 3 can include per-subtable glyph coverage after the last
                // subtable. The chain length skips it without changing shaping.
                offset = end;
            }

            // A valid table can have no chains to apply. It still takes part
            // in table selection, so retain it with an empty chain array.
            return [.. chains];
        }
    }

    /// <summary>
    /// Applies the enabled chains to a run in its shaping order.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="start">The first state table index in the run.</param>
    /// <param name="end">The state table index after the run.</param>
    /// <param name="vertical">Whether the run uses vertical features.</param>
    /// <param name="rtl">Whether the input direction is right-to-left or bottom-to-top.</param>
    /// <param name="featureTags">The OpenType feature tags that the run requests.</param>
    /// <param name="culture">The culture of the run.</param>
    /// <param name="maxCount">The maximum number of glyphs that the buffer can hold during shaping.</param>
    /// <returns>The state table index after the substituted run.</returns>
    private int ApplyChains(ShapingBuffer buffer, int start, int end, bool vertical, bool rtl, IReadOnlyList<Tag> featureTags, CultureInfo culture, int maxCount)
    {
        bool bottomToTop = rtl && buffer.TextOptions.LayoutMode.IsVertical();
        bool rightToLeft = rtl && !vertical;

        // Subtables share the input grapheme order. An insertion updates the
        // indices, but merging clusters does not change the next read position.
        int[]? order = bottomToTop ? AatStateTable.Traversal.GetBottomToTopIndices(buffer, start, end) : null;
        AatStateTable.Traversal traversal = new(start, end, false, order);
        try
        {
            string language = culture.IetfLanguageTag;
            int featureCost = 1 + Math.Max(featureTags.Count, language.Length);

            foreach (Chain chain in this.chains)
            {
                // Selector resolution can compare the run's language or walk its
                // requested feature tags for each record in the chain.
                long selectionCost = (long)chain.Features.Length * featureCost;
                if (buffer.RemainingOperations == 0 || !buffer.CountOperations(selectionCost))
                {
                    break;
                }

                uint flags = chain.DefaultFlags;
                foreach (Feature feature in chain.Features)
                {
                    if (this.features.IsEnabled(feature.Type, feature.Selector, featureTags, language, vertical))
                    {
                        // DisableFlags is already complemented in the font.
                        flags = (flags & feature.DisableFlags) | feature.EnableFlags;
                    }
                }

                foreach (MorphSubTable subtable in chain.SubTables)
                {
                    if (buffer.RemainingOperations == 0)
                    {
                        break;
                    }

                    if (!subtable.AppliesTo(flags, vertical))
                    {
                        continue;
                    }

                    traversal.ResetDirection(subtable.Reverses(rightToLeft));

                    // Reuse spare slots while this subtable edits the glyphs.
                    // Pack the array before a later subtable can reorder a range.
                    buffer.BeginOutputPass(buffer.SpanStart);
                    subtable.Apply(buffer, ref traversal, maxCount);
                    buffer.EndOutputPass();
                }
            }

            return traversal.EndIndex;
        }
        finally
        {
            traversal.ReleaseIndices();
        }
    }

    /// <summary>
    /// Associates deletion events with the live range for positioning.
    /// </summary>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="start">The first state table index in the run.</param>
    /// <param name="end">The state table index after the run.</param>
    private static void FinishAatSubstitution(ShapingBuffer buffer, int start, int end)
    {
        int first = AatStateTable.CountDeletionsBefore(buffer, start);
        int last = AatStateTable.CountDeletionsBefore(buffer, end);
        if (first == last)
        {
            return;
        }

        int liveStart = start - first;
        if (liveStart == end - last)
        {
            // Positioning has no run to visit when substitution removes every glyph.
            buffer.DeletedGlyphs.RemoveRange(first, last - first);
            return;
        }

        Span<(int Index, int Owner, GlyphShapingData Glyph)> deletions = CollectionsMarshal.AsSpan(buffer.DeletedGlyphs);
        for (int i = first; i < last; i++)
        {
            // Subtract earlier deletions to find the gap in the live buffer.
            // Assign the event to the glyph before the gap, or the first glyph.
            deletions[i].Owner = Math.Max(liveStart, deletions[i].Index - i - 1);
        }
    }

    /// <summary>
    /// The mask and tables that make up one independently configured chain.
    /// </summary>
    protected readonly struct Chain
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chain"/> struct.
        /// </summary>
        /// <param name="defaultFlags">The initial feature flags for the chain.</param>
        /// <param name="features">The feature records in application order.</param>
        /// <param name="subTables">The substitution subtables in application order.</param>
        public Chain(uint defaultFlags, Feature[] features, MorphSubTable[] subTables)
        {
            this.DefaultFlags = defaultFlags;
            this.Features = features;
            this.SubTables = subTables;
        }

        /// <summary>
        /// Gets the initial mask of enabled subtables.
        /// </summary>
        public uint DefaultFlags { get; }

        /// <summary>
        /// Gets the feature records that modify the mask for each run.
        /// </summary>
        public Feature[] Features { get; }

        /// <summary>
        /// Gets the subtables in application order.
        /// </summary>
        public MorphSubTable[] SubTables { get; }
    }

    /// <summary>
    /// A setting that changes a chain's enabled-subtable mask.
    /// </summary>
    protected readonly struct Feature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Feature"/> struct.
        /// </summary>
        /// <param name="type">The AAT feature type.</param>
        /// <param name="selector">The AAT feature selector.</param>
        /// <param name="enableFlags">The flags to set when this selector applies.</param>
        /// <param name="disableFlags">The mask that retains flags when this selector applies.</param>
        public Feature(ushort type, ushort selector, uint enableFlags, uint disableFlags)
        {
            this.Type = type;
            this.Selector = selector;
            this.EnableFlags = enableFlags;
            this.DisableFlags = disableFlags;
        }

        /// <summary>
        /// Gets the Apple feature type.
        /// </summary>
        public ushort Type { get; }

        /// <summary>
        /// Gets the selector of the feature.
        /// </summary>
        public ushort Selector { get; }

        /// <summary>
        /// Gets the bits to enable.
        /// </summary>
        public uint EnableFlags { get; }

        /// <summary>
        /// Gets the bits to retain before enabling new ones.
        /// </summary>
        public uint DisableFlags { get; }
    }
}
