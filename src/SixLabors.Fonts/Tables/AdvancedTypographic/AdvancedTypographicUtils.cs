// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Provides shared utility methods for advanced typographic layout processing in GPOS and GSUB tables.
/// </summary>
internal static class AdvancedTypographicUtils
{
    /// <summary>
    /// The maximum context length for sequence matching operations.
    /// Used to prevent excessive processing from maliciously crafted fonts.
    /// Based on HarfBuzz hb-buffer.hh.
    /// </summary>
    public const int MaxContextLength = 64;

    /// <summary>
    /// The maximum length factor multiplied by collection count to compute max allowable collection size.
    /// </summary>
    private const int MaxLengthFactor = 64;

    /// <summary>
    /// The minimum value for the max allowable collection size.
    /// </summary>
    private const int MaxLengthMinimum = 16384;

    /// <summary>
    /// The maximum operations factor multiplied by collection count to compute max allowable operations.
    /// </summary>
    private const int MaxOperationsFactor = 1024;

    /// <summary>
    /// The minimum value for the max allowable operations count.
    /// </summary>
    private const int MaxOperationsMinimum = 16384;

    /// <summary>
    /// The absolute maximum number of shaping characters, set to half of int.MaxValue.
    /// </summary>
    private const int MaxShapingCharsLength = 0x3FFFFFFF; // Half int max.

    /// <summary>
    /// Defines the direction for sequence matching operations.
    /// </summary>
    internal enum MatchDirection
    {
        /// <summary>
        /// Match in the forward direction.
        /// </summary>
        Forward,

        /// <summary>
        /// Match in the backward direction.
        /// </summary>
        Backward
    }

    /// <summary>
    /// Gets a value indicating whether the glyph represented by the codepoint should be interpreted vertically.
    /// </summary>
    /// <param name="codePoint">The codepoint represented by the glyph.</param>
    /// <param name="layoutMode">The layout mode.</param>
    /// <returns>The <see cref="bool"/>.</returns>
    public static bool IsVerticalGlyph(CodePoint codePoint, LayoutMode layoutMode)
    {
        if (layoutMode.IsVertical())
        {
            return true;
        }

        bool isVerticalLayout = layoutMode.IsVerticalMixed();
        return isVerticalLayout && CodePoint.GetVerticalOrientationType(codePoint) is VerticalOrientationType.Upright or VerticalOrientationType.TransformUpright;
    }

    /// <summary>
    /// Gets the maximum allowable shaping collection count for the given input length.
    /// </summary>
    /// <param name="length">The input collection length.</param>
    /// <returns>The maximum allowable count.</returns>
    public static int GetMaxAllowableShapingCollectionCount(int length)
        => (int)Math.Min(Math.Max((long)length * MaxLengthFactor, MaxLengthMinimum), MaxShapingCharsLength);

    /// <summary>
    /// Gets the maximum allowable shaping operations count for the given input length.
    /// </summary>
    /// <param name="length">The input collection length.</param>
    /// <returns>The maximum allowable operations count.</returns>
    public static int GetMaxAllowableShapingOperationsCount(int length)
        => (int)Math.Min(Math.Max((long)length * MaxOperationsFactor, MaxOperationsMinimum), MaxShapingCharsLength);

    /// <summary>
    /// Applies nested lookups from sequence lookup records for GSUB contextual/chaining lookups.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GSUB table.</param>
    /// <param name="feature">The feature tag being applied.</param>
    /// <param name="lookupFlags">The lookup flags for glyph filtering.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="records">The sequence lookup records specifying which lookups to apply at which positions.</param>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="index">The starting index in the collection.</param>
    /// <param name="count">The number of glyphs in the input sequence.</param>
    /// <returns><see langword="true"/> if the lookups were applied.</returns>
    public static bool ApplyLookupList(
        FontMetrics fontMetrics,
        GSubTable table,
        Tag feature,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        SequenceLookupRecord[] records,
        GlyphSubstitutionCollection collection,
        int index,
        int count)
    {
        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, lookupFlags, markFilteringSet);
        int currentCount = collection.Count;

        foreach (SequenceLookupRecord lookupRecord in records)
        {
            ushort sequenceIndex = lookupRecord.SequenceIndex;
            ushort lookupIndex = lookupRecord.LookupListIndex;
            iterator.Index = index;
            iterator.Increment(sequenceIndex);
            GSub.LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
            _ = lookup.TrySubstitution(fontMetrics, table, collection, feature, iterator.Index, count - (iterator.Index - index));

            // Account for substitutions changing the length of the collection.
            if (collection.Count != currentCount)
            {
                count -= currentCount - collection.Count;
                currentCount = collection.Count;
            }
        }

        return true;
    }

    /// <summary>
    /// Applies nested lookups from sequence lookup records for GPOS contextual/chaining lookups.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GPOS table.</param>
    /// <param name="feature">The feature tag being applied.</param>
    /// <param name="lookupFlags">The lookup flags for glyph filtering.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="records">The sequence lookup records specifying which lookups to apply at which positions.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The starting index in the collection.</param>
    /// <param name="count">The number of glyphs in the input sequence.</param>
    /// <returns><see langword="true"/> if the lookups were applied.</returns>
    public static bool ApplyLookupList(
        FontMetrics fontMetrics,
        GPosTable table,
        Tag feature,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        SequenceLookupRecord[] records,
        GlyphPositioningCollection collection,
        int index,
        int count)
    {
        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, lookupFlags, markFilteringSet);
        foreach (SequenceLookupRecord lookupRecord in records)
        {
            ushort sequenceIndex = lookupRecord.SequenceIndex;
            ushort lookupIndex = lookupRecord.LookupListIndex;
            iterator.Index = index;
            iterator.Increment(sequenceIndex);
            LookupTable lookup = table.LookupList.LookupTables[lookupIndex];
            _ = lookup.TryUpdatePosition(fontMetrics, table, collection, feature, iterator.Index, count - (iterator.Index - index));
        }

        return true;
    }

    /// <summary>
    /// Matches an input glyph sequence by glyph ID, verifying that each glyph has the specified feature enabled.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="feature">The feature tag that must be enabled on matched glyphs.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchInputSequence(SkippingGlyphIterator iterator, Tag feature, ushort increment, ushort[] sequence, Span<int> matches)
        => Match(
            increment,
            sequence,
            iterator,
            (component, data) =>
            {
                if (!ContainsFeatureTag(data.Features, feature))
                {
                    return false;
                }

                return component == data.GlyphId;
            },
            matches);

    /// <summary>
    /// Determines whether the feature list contains the specified feature tag in an enabled state.
    /// </summary>
    /// <param name="featureList">The list of tag entries to search.</param>
    /// <param name="feature">The feature tag to find.</param>
    /// <returns><see langword="true"/> if the feature is present and enabled; otherwise, <see langword="false"/>.</returns>
    private static bool ContainsFeatureTag(List<TagEntry> featureList, Tag feature)
    {
        foreach (TagEntry tagEntry in featureList)
        {
            if (tagEntry.Tag == feature && tagEntry.Enabled)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches a glyph sequence by glyph ID.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchSequence(SkippingGlyphIterator iterator, int increment, ushort[] sequence)
        => Match(
            increment,
            sequence,
            iterator,
            (component, data) => component == data.GlyphId,
            default);

    /// <summary>
    /// Matches a glyph sequence by class values using a class definition table.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of class values to match.</param>
    /// <param name="classDefinitionTable">The class definition table used to map glyph IDs to class values.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchClassSequence(
        SkippingGlyphIterator iterator,
        int increment,
        ushort[] sequence,
        ClassDefinitionTable classDefinitionTable)
        => Match(
            increment,
            sequence,
            iterator,
            (component, data) => component == classDefinitionTable.ClassIndexOf(data.GlyphId),
            default);

    /// <summary>
    /// Matches a forward glyph sequence using coverage tables.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="coverageTable">The array of coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the collection.</param>
    /// <param name="endExclusive">The exclusive end index in the collection.</param>
    /// <returns><see langword="true"/> if all coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] coverageTable,
        int startIndex,
        int endExclusive)
        => Match(
            iterator,
            startIndex,
            coverageTable,
            MatchDirection.Forward,
            endExclusive,
            (component, data) => component.CoverageIndexOf(data.GlyphId) >= 0,
            default);

    /// <summary>
    /// Matches a backward (backtrack) glyph sequence using coverage tables.
    /// Per the spec, backtrack[0] matches i-1, then i-2, and so on.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="backtrack">The array of backtrack coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the collection (the first backtrack position).</param>
    /// <param name="endExclusive">The exclusive end index in the collection.</param>
    /// <returns><see langword="true"/> if all backtrack coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchBacktrackCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] backtrack,
        int startIndex,
        int endExclusive)
        => Match(
            iterator,
            startIndex,
            backtrack,
            MatchDirection.Backward,
            endExclusive,
            (component, data) => component.CoverageIndexOf(data.GlyphId) >= 0,
            default);

    /// <summary>
    /// Applies a chained sequence rule by matching backtrack, input, and lookahead glyph ID sequences.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="rule">The chained sequence rule table to apply.</param>
    /// <returns><see langword="true"/> if all sequences matched; otherwise, <see langword="false"/>.</returns>
    public static bool ApplyChainedSequenceRule(SkippingGlyphIterator iterator, ChainedSequenceRuleTable rule)
    {
        if (rule.BacktrackSequence.Length > 0
            && !MatchSequence(iterator, -rule.BacktrackSequence.Length, rule.BacktrackSequence))
        {
            return false;
        }

        if (rule.InputSequence.Length > 0
            && !MatchSequence(iterator, 1, rule.InputSequence))
        {
            return false;
        }

        if (rule.LookaheadSequence.Length > 0
            && !MatchSequence(iterator, 1 + rule.InputSequence.Length, rule.LookaheadSequence))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies a chained class sequence rule by matching backtrack, input, and lookahead class sequences.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="rule">The chained class sequence rule table to apply.</param>
    /// <param name="inputClassDefinitionTable">The class definition table for the input sequence.</param>
    /// <param name="backtrackClassDefinitionTable">The class definition table for the backtrack sequence.</param>
    /// <param name="lookaheadClassDefinitionTable">The class definition table for the lookahead sequence.</param>
    /// <returns><see langword="true"/> if all sequences matched; otherwise, <see langword="false"/>.</returns>
    public static bool ApplyChainedClassSequenceRule(
        SkippingGlyphIterator iterator,
        ChainedClassSequenceRuleTable rule,
        ClassDefinitionTable inputClassDefinitionTable,
        ClassDefinitionTable backtrackClassDefinitionTable,
        ClassDefinitionTable lookaheadClassDefinitionTable)
    {
        if (rule.BacktrackSequence.Length > 0
            && !MatchClassSequence(iterator, -rule.BacktrackSequence.Length, rule.BacktrackSequence, backtrackClassDefinitionTable))
        {
            return false;
        }

        if (rule.InputSequence.Length > 0 &&
            !MatchClassSequence(iterator, 1, rule.InputSequence, inputClassDefinitionTable))
        {
            return false;
        }

        if (rule.LookaheadSequence.Length > 0
            && !MatchClassSequence(iterator, 1 + rule.InputSequence.Length, rule.LookaheadSequence, lookaheadClassDefinitionTable))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks all coverage tables (backtrack, input, and lookahead) for a chained context Format 3 match.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="lookupFlags">The lookup flags for glyph filtering.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="collection">The glyph shaping collection.</param>
    /// <param name="index">The starting index of the input sequence.</param>
    /// <param name="count">The number of glyphs available from the starting index.</param>
    /// <param name="input">The array of input coverage tables.</param>
    /// <param name="backtrack">The array of backtrack coverage tables.</param>
    /// <param name="lookahead">The array of lookahead coverage tables.</param>
    /// <returns><see langword="true"/> if all coverages matched; otherwise, <see langword="false"/>.</returns>
    public static bool CheckAllCoverages(
        FontMetrics fontMetrics,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        IGlyphShapingCollection collection,
        int index,
        int count,
        CoverageTable[] input,
        CoverageTable[] backtrack,
        CoverageTable[] lookahead)
    {
        int endExclusive = index + count;

        SkippingGlyphIterator iterator = new(fontMetrics, collection, index, lookupFlags, markFilteringSet);

        // Compute backtrack start using skippy prev(), not index-1.
        int backtrackStart = index;
        if (backtrack.Length > 0)
        {
            SkippingGlyphIterator backIt = iterator;
            backIt.Index = index;
            backtrackStart = backIt.Prev(); // first backtrack glyph (i-1 in skippy space)
        }

        if (!MatchBacktrackCoverageSequence(iterator, backtrack, backtrackStart, endExclusive))
        {
            return false;
        }

        // Input starts at the current glyph position.
        if (!MatchCoverageSequence(iterator, input, index, endExclusive))
        {
            return false;
        }

        // Compute lookahead start by advancing through the input sequence using skippy Next(),
        // not by raw index arithmetic.
        int lookaheadStart = index;
        if (lookahead.Length > 0)
        {
            SkippingGlyphIterator fwdIt = iterator;
            fwdIt.Index = index;
            fwdIt.Increment(input.Length); // advance input.Length steps in skippy space
            lookaheadStart = fwdIt.Index;
        }

        if (!MatchCoverageSequence(iterator, lookahead, lookaheadStart, endExclusive))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies anchor-based positioning for mark-to-base, mark-to-ligature, or mark-to-mark attachment.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The index of the mark glyph in the collection.</param>
    /// <param name="baseAnchor">The anchor table for the base glyph, or <see langword="null"/> if no anchor is defined.</param>
    /// <param name="markRecord">The mark record containing the mark anchor table and class.</param>
    /// <param name="baseGlyphIndex">The index of the base glyph in the collection.</param>
    /// <param name="feature">The feature tag being applied.</param>
    public static void ApplyAnchor(
        FontMetrics fontMetrics,
        GlyphPositioningCollection collection,
        int index,
        AnchorTable? baseAnchor,
        MarkRecord markRecord,
        int baseGlyphIndex,
        Tag feature)
    {
        // baseAnchor may be null because OpenType MarkToBase allows NULL anchor offsets
        // in BaseArray/BaseRecord. A NULL offset means "this base glyph has no anchor
        // for this mark class", and the lookup must be ignored for this mark–base pair.
        if (baseAnchor is null)
        {
            return;
        }

        GlyphShapingData baseData = collection[baseGlyphIndex];
        AnchorXY baseXY = baseAnchor.GetAnchor(fontMetrics, baseData, collection);

        GlyphShapingData markData = collection[index];
        AnchorXY markXY = markRecord.MarkAnchorTable.GetAnchor(fontMetrics, markData, collection);

        markData.Bounds.X = baseXY.XCoordinate - markXY.XCoordinate;
        markData.Bounds.Y = baseXY.YCoordinate - markXY.YCoordinate;
        markData.MarkAttachment = baseGlyphIndex;
        markData.AppliedFeatures.Add(feature);
    }

    /// <summary>
    /// Applies a value record's positioning adjustments to a glyph in the collection.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="collection">The glyph positioning collection.</param>
    /// <param name="index">The index of the glyph in the collection.</param>
    /// <param name="record">The value record containing positioning adjustments.</param>
    /// <param name="feature">The feature tag being applied.</param>
    public static void ApplyPosition(
        FontMetrics fontMetrics,
        GlyphPositioningCollection collection,
        int index,
        ValueRecord record,
        Tag feature)
    {
        GlyphShapingData current = collection[index];
        current.Bounds.Width += record.XAdvance;
        current.Bounds.Height += record.YAdvance;
        current.Bounds.X += record.XPlacement;
        current.Bounds.Y += record.YPlacement;

        // Apply variation deltas from VariationIndex tables (variable fonts).
        if (record.HasVariation)
        {
            current.Bounds.X += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.XPlacementVariation));
            current.Bounds.Y += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.YPlacementVariation));
            current.Bounds.Width += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.XAdvanceVariation));
            current.Bounds.Height += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.YAdvanceVariation));
        }

        current.AppliedFeatures.Add(feature);
    }

    /// <summary>
    /// Determines whether the specified glyph is a mark glyph based on GDEF class or Unicode properties.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="shapingData">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a mark; otherwise, <see langword="false"/>.</returns>
    public static bool IsMarkGlyph(FontMetrics fontMetrics, ushort glyphId, GlyphShapingData shapingData)
    {
        if (!fontMetrics.TryGetGlyphClass(glyphId, out GlyphClassDef? glyphClass) &&
            !CodePoint.IsMark(shapingData.CodePoint))
        {
            return false;
        }

        if (glyphClass != GlyphClassDef.MarkGlyph)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the glyph shaping class (mark, base, ligature, mark attachment type) for the specified glyph,
    /// using GDEF table data if available or falling back to Unicode properties.
    /// Results are cached on the <see cref="GlyphShapingData"/> instance.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="shapingData">The glyph shaping data, used for caching and Unicode fallback.</param>
    /// <returns>The <see cref="GlyphShapingClass"/>.</returns>
    public static GlyphShapingClass GetGlyphShapingClass(FontMetrics fontMetrics, ushort glyphId, GlyphShapingData shapingData)
    {
        // Cache the shaping class on the GlyphShapingData to avoid repeated GDEF lookups.
        // The cache key stores the glyph id; -1 means "not cached".
        if (shapingData.ShapingClassCacheKey == glyphId)
        {
            return shapingData.CachedShapingClass;
        }

        bool isMark;
        bool isBase;
        bool isLigature;
        ushort markAttachmentType = 0;
        if (fontMetrics.TryGetGlyphClass(glyphId, out GlyphClassDef? glyphClass))
        {
            isMark = glyphClass == GlyphClassDef.MarkGlyph;
            isBase = glyphClass == GlyphClassDef.BaseGlyph;
            isLigature = glyphClass == GlyphClassDef.LigatureGlyph;
            if (fontMetrics.TryGetMarkAttachmentClass(glyphId, out GlyphClassDef? markAttachmentClass))
            {
                markAttachmentType = (ushort)markAttachmentClass;
            }
        }
        else
        {
            // TODO: We may have to store each codepoint. FontKit checks all.
            isMark = CodePoint.IsMark(shapingData.CodePoint);
            isBase = !isMark;
            isLigature = shapingData.CodePointCount > 1;
        }

        GlyphShapingClass result = new(isMark, isBase, isLigature, markAttachmentType);
        shapingData.CachedShapingClass = result;
        shapingData.ShapingClassCacheKey = glyphId;
        return result;
    }

    /// <summary>
    /// Determines whether the specified glyph is in the given mark filtering set.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <returns><see langword="true"/> if the glyph is in the mark filtering set; otherwise, <see langword="false"/>.</returns>
    public static bool IsInMarkFilteringSet(FontMetrics fontMetrics, ushort markFilteringSet, ushort glyphId)
        => fontMetrics.IsInMarkFilteringSet(markFilteringSet, glyphId);

    /// <summary>
    /// Matches a sequence of elements against glyphs using an increment-based approach.
    /// </summary>
    /// <typeparam name="T">The type of sequence elements to match.</typeparam>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of elements to match.</param>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="condition">The condition function to test each element against glyph data.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <returns><see langword="true"/> if all elements in the sequence were matched; otherwise, <see langword="false"/>.</returns>
    private static bool Match<T>(
        int increment,
        T[] sequence,
        SkippingGlyphIterator iterator,
        Func<T, GlyphShapingData, bool> condition,
        Span<int> matches)
    {
        int position = iterator.Index;
        int offset = iterator.Increment(increment);
        IGlyphShapingCollection collection = iterator.Collection;

        if (offset < 0)
        {
            return false;
        }

        int i = 0;
        while (i < sequence.Length && i < MaxContextLength && offset < collection.Count)
        {
            if (!condition(sequence[i], collection[offset]))
            {
                break;
            }

            if (matches.Length == MaxContextLength)
            {
                matches[i] = iterator.Index;
            }

            i++;
            offset = iterator.Next();
        }

        iterator.Index = position;
        return i == sequence.Length;
    }

    /// <summary>
    /// Matches a sequence of elements against glyphs using a directional (forward/backward) approach.
    /// </summary>
    /// <typeparam name="T">The type of sequence elements to match.</typeparam>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="startIndex">The starting index in the collection.</param>
    /// <param name="sequence">The array of elements to match.</param>
    /// <param name="direction">The direction to iterate (forward or backward).</param>
    /// <param name="endExclusive">The exclusive end index in the collection.</param>
    /// <param name="condition">The condition function to test each element against glyph data.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <returns><see langword="true"/> if all elements in the sequence were matched; otherwise, <see langword="false"/>.</returns>
    private static bool Match<T>(
        SkippingGlyphIterator iterator,
        int startIndex,
        T[] sequence,
        MatchDirection direction,
        int endExclusive,
        Func<T, GlyphShapingData, bool> condition,
        Span<int> matches)
    {
        if (sequence.Length == 0)
        {
            return true;
        }

        int saved = iterator.Index;
        iterator.Index = startIndex;

        IGlyphShapingCollection collection = iterator.Collection;
        int limit = Math.Min(endExclusive, collection.Count);

        for (int i = 0; i < sequence.Length && i < MaxContextLength; i++)
        {
            if (iterator.Index < 0 || iterator.Index >= limit)
            {
                iterator.Index = saved;
                return false;
            }

            GlyphShapingData data = collection[iterator.Index];
            if (!condition(sequence[i], data))
            {
                iterator.Index = saved;
                return false;
            }

            if (matches.Length == MaxContextLength)
            {
                matches[i] = iterator.Index;
            }

            if (i + 1 < sequence.Length)
            {
                iterator.Index = direction == MatchDirection.Forward
                    ? iterator.Next()
                    : iterator.Prev();
            }
        }

        iterator.Index = saved;
        return true;
    }
}
