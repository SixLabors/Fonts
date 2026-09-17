// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
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
    /// The maximum depth of nested lookup application. Contextual lookups may
    /// recurse into one another, so a font can describe unbounded nesting;
    /// application stops at this depth instead.
    /// </summary>
    public const int MaxNestingLevel = 64;

    /// <summary>
    /// The maximum length factor multiplied by buffer count to compute max allowable buffer size.
    /// </summary>
    private const int MaxLengthFactor = 64;

    /// <summary>
    /// The minimum value for the max allowable buffer size.
    /// </summary>
    private const int MaxLengthMinimum = 16384;

    /// <summary>
    /// The maximum operations factor multiplied by buffer count to compute max allowable operations.
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
    /// Supplies a compile-time glyph comparison to the shared sequence walks.
    /// </summary>
    /// <typeparam name="T">The type of sequence element to compare.</typeparam>
    private interface ISequenceMatcher<T>
    {
        /// <summary>
        /// Determines whether one sequence element matches the current glyph record.
        /// </summary>
        /// <param name="element">The sequence element.</param>
        /// <param name="data">The glyph record.</param>
        /// <returns><see langword="true"/> when the element matches; otherwise, <see langword="false"/>.</returns>
        static abstract bool Matches(T element, ref GlyphShapingData data);
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
    /// Gets the maximum allowable shaping buffer count for the given input length.
    /// </summary>
    /// <param name="length">The input buffer length.</param>
    /// <returns>The maximum allowable count.</returns>
    public static int GetMaxAllowableShapingCollectionCount(int length)
        => (int)Math.Min(Math.Max((long)length * MaxLengthFactor, MaxLengthMinimum), MaxShapingCharsLength);

    /// <summary>
    /// Gets the maximum allowable shaping operations count for the given input length.
    /// </summary>
    /// <param name="length">The input buffer length.</param>
    /// <returns>The maximum allowable operations count.</returns>
    public static int GetMaxAllowableShapingOperationsCount(int length)
        => (int)Math.Min(Math.Max((long)length * MaxOperationsFactor, MaxOperationsMinimum), MaxShapingCharsLength);

    /// <summary>
    /// Applies nested lookups from sequence lookup records for GSUB contextual/chaining lookups.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GSUB table.</param>
    /// <param name="feature">The feature tag being applied.</param>
    /// <param name="lookupMask">The applying lookup's combined mask, inherited by the nested lookups.</param>
    /// <param name="records">The sequence lookup records specifying which lookups to apply at which positions.</param>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="matchPositions">The buffer positions the input sequence matched at, reconciled as nested lookups change the buffer's length.</param>
    /// <param name="matchCount">The number of matched positions.</param>
    /// <param name="matchEnd">The position one past the final matched input record.</param>
    /// <returns><see langword="true"/> if the lookups were applied.</returns>
    public static bool ApplyLookupList(
        FontMetrics fontMetrics,
        GSubTable table,
        Tag feature,
        uint lookupMask,
        SequenceLookupRecord[] records,
        ShapingBuffer buffer,
        Span<int> matchPositions,
        int matchCount,
        int matchEnd)
    {
        if (buffer.NestingLimitReached)
        {
            return false;
        }

        // Matching walked the records still to be read; applying works against
        // the pass position, where the produced side has its own length. A
        // sequence index can name a record a nested lookup produced, which no
        // input position describes, so the matched positions move into that
        // frame once and stay in it.
        int shift = buffer.PassBacktrackLength - buffer.ReadIndex;
        int end = buffer.PassBacktrackLength + (matchEnd - buffer.ReadIndex);

        // The two sides sit level until a lookup in this pass changes a length,
        // and level means the frames coincide.
        if (shift != 0)
        {
            for (int i = 0; i < matchCount; i++)
            {
                matchPositions[i] += shift;
            }
        }

        buffer.PushNestedApplication();

        foreach (SequenceLookupRecord lookupRecord in records)
        {
            int sequenceIndex = lookupRecord.SequenceIndex;
            if (sequenceIndex >= matchCount)
            {
                continue;
            }

            int total = buffer.PassBacktrackLength + buffer.PassLookaheadLength;
            int position = matchPositions[sequenceIndex];

            // An earlier nested lookup can consume enough records to strand a
            // later sequence position past everything that remains.
            if (position >= total)
            {
                continue;
            }

            // The nested lookup applies at the cursor, so the cursor goes to the
            // record: forward over what it passes, back over what an earlier
            // nested lookup already produced.
            buffer.MoveTo(position);

            GSub.LookupTable lookup = table.LookupList.LookupTables[lookupRecord.LookupListIndex];
            _ = lookup.TrySubstitution(fontMetrics, table, buffer, feature, lookupMask, buffer.ReadIndex, buffer.PassLookaheadLength);

            int delta = buffer.PassBacktrackLength + buffer.PassLookaheadLength - total;
            if (delta == 0)
            {
                continue;
            }

            end += delta;
            if (end < position)
            {
                // A nested lookup that consumed a great deal can pull the end
                // behind the record it applied at; nothing before that record
                // can have been consumed, so the end stops there.
                delta += position - end;
                end = position;
            }

            matchCount = FixupMatchPositions(matchPositions, matchCount, sequenceIndex, delta);
        }

        buffer.PopNestedApplication();

        // Everything the rule matched is now behind the pass position.
        buffer.MoveTo(end);
        return true;
    }

    /// <summary>
    /// Reconciles the matched positions after a nested lookup changed the
    /// buffer's length. A growth is taken as records inserted directly after the
    /// applying position, and a shrink as the positions following it having been
    /// consumed; the trailing positions then shift by the same delta.
    /// </summary>
    /// <param name="matchPositions">The matched positions to reconcile.</param>
    /// <param name="matchCount">The number of matched positions.</param>
    /// <param name="sequenceIndex">The position the nested lookup applied at.</param>
    /// <param name="delta">The buffer's length change.</param>
    /// <returns>The new number of matched positions.</returns>
    private static int FixupMatchPositions(Span<int> matchPositions, int matchCount, int sequenceIndex, int delta)
    {
        int next = sequenceIndex + 1;
        if (delta > 0)
        {
            if (delta + matchCount > MaxContextLength)
            {
                return matchCount;
            }
        }
        else
        {
            // A shrink can never consume more positions than the match holds.
            delta = Math.Max(delta, next - matchCount);
            next -= delta;
        }

        int tail = matchCount - next;
        if (tail > 0)
        {
            matchPositions.Slice(next, tail).CopyTo(matchPositions[(next + delta)..]);
        }

        next += delta;
        matchCount += delta;

        for (int j = sequenceIndex + 1; j < next; j++)
        {
            matchPositions[j] = matchPositions[j - 1] + 1;
        }

        for (; next < matchCount; next++)
        {
            matchPositions[next] += delta;
        }

        return matchCount;
    }

    /// <summary>
    /// Applies nested lookups from sequence lookup records for GPOS contextual/chaining lookups.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="table">The GPOS table.</param>
    /// <param name="feature">The feature tag being applied.</param>
    /// <param name="records">The sequence lookup records specifying which lookups to apply at which positions.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="matchPositions">The buffer positions the input sequence matched at.</param>
    /// <param name="matchCount">The number of matched positions.</param>
    /// <param name="count">The number of glyphs in the input sequence.</param>
    /// <param name="matchEnd">The position one past the final matched input record.</param>
    /// <returns><see langword="true"/> if the lookups were applied.</returns>
    public static bool ApplyLookupList(
        FontMetrics fontMetrics,
        GPosTable table,
        Tag feature,
        SequenceLookupRecord[] records,
        ShapingBuffer buffer,
        ReadOnlySpan<int> matchPositions,
        int matchCount,
        int count,
        int matchEnd)
    {
        if (buffer.NestingLimitReached)
        {
            return false;
        }

        // Positioning never changes the buffer's length, so the matched
        // positions stand for the whole record list.
        int startIndex = matchPositions[0];
        int end = buffer.PassBacktrackLength + (matchEnd - buffer.ReadIndex);
        buffer.PushNestedApplication();

        foreach (SequenceLookupRecord lookupRecord in records)
        {
            int sequenceIndex = lookupRecord.SequenceIndex;
            if (sequenceIndex >= matchCount)
            {
                continue;
            }

            int position = matchPositions[sequenceIndex];
            LookupTable lookup = table.LookupList.LookupTables[lookupRecord.LookupListIndex];
            _ = lookup.TryUpdatePosition(fontMetrics, table, buffer, feature, position, count - (position - startIndex));
        }

        buffer.PopNestedApplication();

        // Everything the rule matched is now behind the cursor.
        buffer.MoveTo(end);
        return true;
    }

    /// <summary>
    /// Matches an input glyph sequence by glyph ID, verifying that each glyph has the applying lookup enabled.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="featureMask">The applying lookup's combined mask; matched glyphs must have it enabled.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchInputSequence(SkippingGlyphIterator iterator, uint featureMask, ushort increment, ushort[] sequence, Span<int> matches)
    {
        iterator.SetMatchContext(featureMask, false);
        return Match<ushort, GlyphIdSequenceMatcher>(increment, sequence, iterator, matches);
    }

    /// <summary>
    /// Matches a glyph sequence by glyph ID under the given matcher context.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchSequence(SkippingGlyphIterator iterator, int increment, ushort[] sequence, uint mask, bool contextMatch)
        => MatchSequence(iterator, increment, sequence, mask, contextMatch, out _);

    /// <summary>
    /// Matches a glyph sequence by glyph ID under the given matcher context,
    /// reporting the position one past the final matched element.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchSequence(SkippingGlyphIterator iterator, int increment, ushort[] sequence, uint mask, bool contextMatch, out int matchEnd)
        => MatchSequence(iterator, increment, sequence, mask, contextMatch, default, out matchEnd);

    /// <summary>
    /// Matches a glyph sequence by glyph ID under the given matcher context,
    /// recording where each element matched so nested lookups apply to the
    /// records the match actually consumed rather than to a re-derived walk.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of glyph IDs to match.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <param name="matches">The span receiving the matched positions, or default when they are not needed.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchSequence(SkippingGlyphIterator iterator, int increment, ushort[] sequence, uint mask, bool contextMatch, Span<int> matches, out int matchEnd)
    {
        iterator.SetMatchContext(mask, contextMatch);
        return Match<ushort, GlyphIdSequenceMatcher>(increment, sequence, iterator, matches, out matchEnd);
    }

    /// <summary>
    /// Matches a glyph sequence by class values using a class definition table
    /// under the given matcher context.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of class values to match.</param>
    /// <param name="classDefinitionTable">The class definition table used to map glyph IDs to class values.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchClassSequence(
        SkippingGlyphIterator iterator,
        int increment,
        ushort[] sequence,
        ClassDefinitionTable classDefinitionTable,
        uint mask,
        bool contextMatch)
        => MatchClassSequence(iterator, increment, sequence, classDefinitionTable, mask, contextMatch, out _);

    /// <summary>
    /// Matches a glyph sequence by class values under the given matcher context,
    /// reporting the position one past the final matched element.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of class values to match.</param>
    /// <param name="classDefinitionTable">The class definition table used to map glyph IDs to class values.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchClassSequence(
        SkippingGlyphIterator iterator,
        int increment,
        ushort[] sequence,
        ClassDefinitionTable classDefinitionTable,
        uint mask,
        bool contextMatch,
        out int matchEnd)
        => MatchClassSequence(iterator, increment, sequence, classDefinitionTable, mask, contextMatch, default, out matchEnd);

    /// <summary>
    /// Matches a glyph sequence by class values under the given matcher context,
    /// recording where each element matched so nested lookups apply to the
    /// records the match actually consumed.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of class values to match.</param>
    /// <param name="classDefinitionTable">The class definition table used to map glyph IDs to class values.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches backtrack or lookahead context.</param>
    /// <param name="matches">The span receiving the matched positions, or default when they are not needed.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchClassSequence(
        SkippingGlyphIterator iterator,
        int increment,
        ushort[] sequence,
        ClassDefinitionTable classDefinitionTable,
        uint mask,
        bool contextMatch,
        Span<int> matches,
        out int matchEnd)
    {
        iterator.SetMatchContext(mask, contextMatch);
        int position = iterator.Index;
        ShapingBuffer buffer = iterator.Collection;
        matchEnd = position + 1;
        int i = 0;

        // Class-context matching is one of the hottest complex-script paths.
        // Keep its class lookup directly in the loop so every matched element
        // avoids a delegate dispatch while retaining the same iterator gates.
        if (!iterator.MatchTransparencyActive)
        {
            int solidOffset = iterator.Increment(increment);
            if (solidOffset < 0)
            {
                return false;
            }

            while (i < sequence.Length && i < MaxContextLength && solidOffset < buffer.Count)
            {
                ref GlyphShapingData solidData = ref buffer[solidOffset];
                if (!iterator.MayMatch(ref solidData) || sequence[i] != classDefinitionTable.ClassIndexOf(solidData.GlyphId))
                {
                    break;
                }

                if (!matches.IsEmpty)
                {
                    matches[i] = solidOffset;
                }

                i++;
                matchEnd = solidOffset + 1;
                solidOffset = iterator.Next();
            }

            iterator.Index = position;
            return i == sequence.Length;
        }

        // A single forward step must test the immediately following transparent
        // record against the first class before deciding whether to skip it.
        int offset;
        if (increment == 1)
        {
            offset = position + 1;
            iterator.Index = offset;
        }
        else
        {
            offset = iterator.Increment(increment);
        }

        if (offset < 0)
        {
            return false;
        }

        // Property-skipped records never participate. A transparent record can
        // match its named class; otherwise it is stepped over, while a solid
        // class mismatch refuses the rule.
        while (i < sequence.Length && i < MaxContextLength && offset < buffer.Count)
        {
            if (iterator.IsPropertySkipped(offset))
            {
                offset = ++iterator.Index;
                continue;
            }

            ref GlyphShapingData data = ref buffer[offset];
            if (iterator.MayMatch(ref data) && sequence[i] == classDefinitionTable.ClassIndexOf(data.GlyphId))
            {
                if (!matches.IsEmpty)
                {
                    matches[i] = offset;
                }

                i++;
                matchEnd = offset + 1;
                offset = ++iterator.Index;
                continue;
            }

            if (!iterator.IsTransparent(ref data))
            {
                break;
            }

            offset = ++iterator.Index;
        }

        iterator.Index = position;
        return i == sequence.Length;
    }

    /// <summary>
    /// Matches a forward glyph sequence using coverage tables under the given
    /// matcher context.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="coverageTable">The array of coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the buffer.</param>
    /// <param name="endExclusive">The exclusive end index in the buffer.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches lookahead context.</param>
    /// <returns><see langword="true"/> if all coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] coverageTable,
        int startIndex,
        int endExclusive,
        uint mask,
        bool contextMatch)
        => MatchCoverageSequence(iterator, coverageTable, startIndex, endExclusive, mask, contextMatch, out _);

    /// <summary>
    /// Matches a forward glyph sequence using coverage tables under the given
    /// matcher context, reporting the position one past the final matched element.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="coverageTable">The array of coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the buffer.</param>
    /// <param name="endExclusive">The exclusive end index in the buffer.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches lookahead context.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if all coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] coverageTable,
        int startIndex,
        int endExclusive,
        uint mask,
        bool contextMatch,
        out int matchEnd)
        => MatchCoverageSequence(iterator, coverageTable, startIndex, endExclusive, mask, contextMatch, default, out matchEnd);

    /// <summary>
    /// Matches a forward glyph sequence using coverage tables under the given
    /// matcher context, recording where each element matched so nested lookups
    /// apply to the records the match actually consumed.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="coverageTable">The array of coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the buffer.</param>
    /// <param name="endExclusive">The exclusive end index in the buffer.</param>
    /// <param name="mask">The applying lookup's mask; ignored during context matching.</param>
    /// <param name="contextMatch">Whether this matches lookahead context.</param>
    /// <param name="matches">The span receiving the matched positions, or default when they are not needed.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if all coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] coverageTable,
        int startIndex,
        int endExclusive,
        uint mask,
        bool contextMatch,
        Span<int> matches,
        out int matchEnd)
    {
        iterator.SetMatchContext(mask, contextMatch);
        return Match<CoverageTable, CoverageSequenceMatcher>(iterator, startIndex, coverageTable, MatchDirection.Forward, endExclusive, matches, out matchEnd);
    }

    /// <summary>
    /// Matches a backward (backtrack) glyph sequence using coverage tables.
    /// Per the spec, backtrack[0] matches i-1, then i-2, and so on. Backtrack is
    /// always context, so joiners are transparent to it.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator, already stamped at the applying glyph for backtrack matching.</param>
    /// <param name="backtrack">The array of backtrack coverage tables to match against.</param>
    /// <param name="startIndex">The starting index in the buffer (the first backtrack position).</param>
    /// <param name="endExclusive">The exclusive end index in the buffer.</param>
    /// <returns><see langword="true"/> if all backtrack coverage tables matched; otherwise, <see langword="false"/>.</returns>
    public static bool MatchBacktrackCoverageSequence(
        SkippingGlyphIterator iterator,
        CoverageTable[] backtrack,
        int startIndex,
        int endExclusive)
    {
        return Match<CoverageTable, CoverageSequenceMatcher>(iterator, startIndex, backtrack, MatchDirection.Backward, endExclusive, default, out _);
    }

    /// <summary>
    /// Matches a backtrack sequence by class value over the records the pass has
    /// produced. The class table travels as match state so the lambda stays
    /// static: a capturing lambda here would allocate on every rule attempt.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator, already pointed at the produced side.</param>
    /// <param name="startIndex">The first backtrack position.</param>
    /// <param name="sequence">The array of class values to match.</param>
    /// <param name="classDefinitionTable">The class definition table for the backtrack sequence.</param>
    /// <returns><see langword="true"/> if the entire sequence was matched; otherwise, <see langword="false"/>.</returns>
    private static bool MatchBacktrackClassSequence(
        SkippingGlyphIterator iterator,
        int startIndex,
        ushort[] sequence,
        ClassDefinitionTable classDefinitionTable)
    {
        if (sequence.Length == 0)
        {
            return true;
        }

        int offset = startIndex;
        int limit = iterator.RecordCount;
        int i = 0;
        while (i < sequence.Length && i < MaxContextLength)
        {
            if (offset < 0 || offset >= limit)
            {
                return false;
            }

            if (iterator.IsPropertySkipped(offset))
            {
                offset--;
                continue;
            }

            ref GlyphShapingData data = ref iterator.RecordAt(offset);
            if (iterator.MayMatch(ref data) && sequence[i] == classDefinitionTable.ClassIndexOf(data.GlyphId))
            {
                i++;
                offset--;
                continue;
            }

            if (!iterator.IsTransparent(ref data))
            {
                return false;
            }

            offset--;
        }

        return true;
    }

    /// <summary>
    /// Applies a chained sequence rule by matching backtrack, input, and lookahead glyph ID sequences.
    /// The input matches under the applying lookup's mask and joiner handling;
    /// backtrack and lookahead match as context, transparent to joiners.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="rule">The chained sequence rule table to apply.</param>
    /// <param name="mask">The applying lookup's mask.</param>
    /// <param name="matches">The span receiving the matched input positions, offset by one for the coverage-matched first glyph.</param>
    /// <param name="matchEnd">The position one past the final matched input record.</param>
    /// <returns><see langword="true"/> if all sequences matched; otherwise, <see langword="false"/>.</returns>
    public static bool ApplyChainedSequenceRule(SkippingGlyphIterator iterator, ChainedSequenceRuleTable rule, uint mask, Span<int> matches, out int matchEnd)
    {
        matchEnd = iterator.Index + 1;
        if (rule.InputSequence.Length > 0
            && !MatchSequence(iterator, 1, rule.InputSequence, mask, false, matches, out matchEnd))
        {
            return false;
        }

        if (rule.LookaheadSequence.Length > 0)
        {
            // Lookahead starts exactly one past the final matched input element,
            // not a stepped jump over the input count.
            SkippingGlyphIterator lookahead = iterator;
            lookahead.Index = matchEnd - 1;
            if (!MatchSequence(lookahead, 1, rule.LookaheadSequence, 0, true))
            {
                return false;
            }
        }

        if (rule.BacktrackSequence.Length > 0)
        {
            SkippingGlyphIterator backIt = iterator;
            int backtrackStart = backIt.StartBacktrack();
            if (!Match<ushort, GlyphIdSequenceMatcher>(backIt, backtrackStart, rule.BacktrackSequence, MatchDirection.Backward, int.MaxValue, default, out _))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Applies a chained class sequence rule by matching backtrack, input, and lookahead class sequences.
    /// The input matches under the applying lookup's mask and joiner handling;
    /// backtrack and lookahead match as context, transparent to joiners.
    /// </summary>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="rule">The chained class sequence rule table to apply.</param>
    /// <param name="inputClassDefinitionTable">The class definition table for the input sequence.</param>
    /// <param name="backtrackClassDefinitionTable">The class definition table for the backtrack sequence.</param>
    /// <param name="lookaheadClassDefinitionTable">The class definition table for the lookahead sequence.</param>
    /// <param name="mask">The applying lookup's mask.</param>
    /// <param name="matches">The span receiving the matched input positions, offset by one for the coverage-matched first glyph.</param>
    /// <param name="matchEnd">The position one past the final matched input record.</param>
    /// <returns><see langword="true"/> if all sequences matched; otherwise, <see langword="false"/>.</returns>
    public static bool ApplyChainedClassSequenceRule(
        SkippingGlyphIterator iterator,
        ChainedClassSequenceRuleTable rule,
        ClassDefinitionTable inputClassDefinitionTable,
        ClassDefinitionTable backtrackClassDefinitionTable,
        ClassDefinitionTable lookaheadClassDefinitionTable,
        uint mask,
        Span<int> matches,
        out int matchEnd)
    {
        matchEnd = iterator.Index + 1;
        if (rule.InputSequence.Length > 0 &&
            !MatchClassSequence(iterator, 1, rule.InputSequence, inputClassDefinitionTable, mask, false, matches, out matchEnd))
        {
            return false;
        }

        if (rule.LookaheadSequence.Length > 0)
        {
            // Lookahead starts exactly one past the final matched input element,
            // not a stepped jump over the input count.
            SkippingGlyphIterator lookahead = iterator;
            lookahead.Index = matchEnd - 1;
            if (!MatchClassSequence(lookahead, 1, rule.LookaheadSequence, lookaheadClassDefinitionTable, 0, true))
            {
                return false;
            }
        }

        if (rule.BacktrackSequence.Length > 0)
        {
            SkippingGlyphIterator backIt = iterator;
            int backtrackStart = backIt.StartBacktrack();
            if (!MatchBacktrackClassSequence(backIt, backtrackStart, rule.BacktrackSequence, backtrackClassDefinitionTable))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks all coverage tables (backtrack, input, and lookahead) for a chained context Format 3 match.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="lookupFlags">The lookup flags for glyph filtering.</param>
    /// <param name="markFilteringSet">The mark filtering set index.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The starting index of the input sequence.</param>
    /// <param name="count">The number of glyphs available from the starting index.</param>
    /// <param name="input">The array of input coverage tables.</param>
    /// <param name="backtrack">The array of backtrack coverage tables.</param>
    /// <param name="lookahead">The array of lookahead coverage tables.</param>
    /// <param name="mask">The applying lookup's mask; the input matches under it.</param>
    /// <param name="matches">The span receiving the matched input positions; the input coverage array covers the whole input including its first glyph.</param>
    /// <param name="matchEnd">The position one past the final matched input record.</param>
    /// <returns><see langword="true"/> if all coverages matched; otherwise, <see langword="false"/>.</returns>
    public static bool CheckAllCoverages(
        FontMetrics fontMetrics,
        LookupFlags lookupFlags,
        ushort markFilteringSet,
        ShapingBuffer buffer,
        int index,
        int count,
        CoverageTable[] input,
        CoverageTable[] backtrack,
        CoverageTable[] lookahead,
        uint mask,
        Span<int> matches,
        out int matchEnd)
    {
        int endExclusive = index + count;

        SkippingGlyphIterator iterator = new(fontMetrics, buffer, index, lookupFlags, markFilteringSet);

        // Backtrack steps back through the records the pass produced, skipping
        // as context so joiners are transparent to it.
        matchEnd = index;
        if (backtrack.Length > 0)
        {
            SkippingGlyphIterator backIt = iterator;
            int backtrackStart = backIt.StartBacktrack();
            if (!MatchBacktrackCoverageSequence(backIt, backtrack, backtrackStart, int.MaxValue))
            {
                return false;
            }
        }

        // Input starts at the current glyph position; lookahead starts exactly
        // one past the final matched input element.
        if (!MatchCoverageSequence(iterator, input, index, endExclusive, mask, false, matches, out matchEnd))
        {
            return false;
        }

        if (!MatchCoverageSequence(iterator, lookahead, matchEnd, endExclusive, 0, true))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies anchor-based positioning for mark-to-base, mark-to-ligature, or mark-to-mark attachment.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="index">The index of the mark glyph in the buffer.</param>
    /// <param name="baseAnchor">The anchor table for the base glyph, or <see langword="null"/> if no anchor is defined.</param>
    /// <param name="markRecord">The mark record containing the mark anchor table and class.</param>
    /// <param name="baseGlyphIndex">The index of the base glyph in the buffer.</param>
    /// <param name="feature">The feature tag being applied.</param>
    public static void ApplyAnchor(
        FontMetrics fontMetrics,
        ShapingBuffer buffer,
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

        ref GlyphShapingData baseData = ref buffer[baseGlyphIndex];
        AnchorXY baseXY = baseAnchor.GetAnchor(fontMetrics, ref baseData, buffer);

        ref GlyphShapingData markData = ref buffer[index];
        AnchorXY markXY = markRecord.MarkAnchorTable.GetAnchor(fontMetrics, ref markData, buffer);

        ref GlyphShapingPosition markPosition = ref buffer.PositionAt(index);
        markPosition.Bounds.X = baseXY.XCoordinate - markXY.XCoordinate;
        markPosition.Bounds.Y = baseXY.YCoordinate - markXY.YCoordinate;
        markPosition.MarkAttachment = baseGlyphIndex;
        markData.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
    }

    /// <summary>
    /// Applies a value record's positioning adjustments to a glyph in the buffer.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="index">The index of the glyph in the buffer.</param>
    /// <param name="record">The value record containing positioning adjustments.</param>
    /// <param name="feature">The feature tag being applied.</param>
    public static void ApplyPosition(
        FontMetrics fontMetrics,
        ShapingBuffer buffer,
        int index,
        ValueRecord record,
        Tag feature)
    {
        ref GlyphShapingPosition position = ref buffer.PositionAt(index);
        position.Bounds.Width += record.XAdvance;
        position.Bounds.Height += record.YAdvance;
        position.Bounds.X += record.XPlacement;
        position.Bounds.Y += record.YPlacement;

        // Apply variation deltas from VariationIndex tables (variable fonts).
        if (record.HasVariation)
        {
            position.Bounds.X += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.XPlacementVariation));
            position.Bounds.Y += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.YPlacementVariation));
            position.Bounds.Width += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.XAdvanceVariation));
            position.Bounds.Height += (short)MathF.Round(fontMetrics.GetGDefVariationDelta(record.YAdvanceVariation));
        }

        ref GlyphShapingData current = ref buffer[index];
        current.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
    }

    /// <summary>
    /// Replaces a matched glyph sequence with one ligature while preserving component attachment bookkeeping.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The index of the first matched glyph.</param>
    /// <param name="matches">The indices of the remaining matched glyphs.</param>
    /// <param name="glyphId">The ligature glyph identifier.</param>
    /// <param name="feature">The feature applying the substitution.</param>
    /// <param name="count">The number of glyphs in the current shaping segment.</param>
    public static void ApplyLigatureSubstitution(FontMetrics fontMetrics, ShapingBuffer buffer, int index, ReadOnlySpan<int> matches, ushort glyphId, Tag feature, int count)
    {
        ref GlyphShapingData data = ref buffer[index];
        GlyphShapingClass shapingClass = GetGlyphShapingClass(fontMetrics, buffer, data.GlyphId, ref data);
        bool isBaseLigature = shapingClass.IsBase;
        bool isMarkLigature = shapingClass.IsMark;

        // A base followed only by marks remains a base, and marks combined only
        // with marks remain attached to their existing base. Any non-mark
        // component turns the result into a new ordinary ligature.
        for (int i = 0; i < matches.Length; i++)
        {
            ref GlyphShapingData match = ref buffer[matches[i]];
            if (!IsMarkGlyph(fontMetrics, match.GlyphId, ref match))
            {
                isBaseLigature = false;
                isMarkLigature = false;
                break;
            }
        }

        bool isLigature = !isBaseLigature && !isMarkLigature;

        // An ordinary ligature needs a fresh identity so later positioning can
        // distinguish its components. A mark ligature keeps the identity and
        // component of the mark stack it already belongs to.
        int ligatureId = isLigature ? buffer.LigatureId++ : data.LigatureId;
        int ligatureComponent = isLigature ? -1 : data.LigatureComponent;
        int lastLigatureId = data.LigatureId;
        int lastComponentCount = data.LigatureComponentCount;
        int currentComponentCount = lastComponentCount;
        int nextIndex = index + 1;

        // Matching can skip marks between two components. Move those marks to
        // the new ligature identity while translating their old component
        // numbers past every component already consumed.
        foreach (int matchIndex in matches)
        {
            if (isLigature)
            {
                while (nextIndex < matchIndex)
                {
                    ref GlyphShapingData current = ref buffer[nextIndex];
                    int currentComponent = current.LigatureComponent == -1 ? lastComponentCount : current.LigatureComponent;
                    current.LigatureId = ligatureId;
                    current.LigatureComponent = currentComponentCount - lastComponentCount + Math.Min(currentComponent, lastComponentCount);
                    nextIndex++;
                }
            }
            else
            {
                nextIndex = matchIndex;
            }

            ref GlyphShapingData last = ref buffer[nextIndex];
            lastLigatureId = last.LigatureId;
            lastComponentCount = last.LigatureComponentCount;
            currentComponentCount += lastComponentCount;
            nextIndex++;
        }

        // A matched component can itself be a ligature whose attached marks
        // follow the final matched glyph. Translate only its contiguous component
        // records: a shared identity without a component marks the end of that
        // attachment tail just as surely as a different identity does.
        if (!isMarkLigature && lastLigatureId > 0)
        {
            int end = index + count;
            for (int i = nextIndex; i < end; i++)
            {
                ref GlyphShapingData current = ref buffer[i];
                if (current.LigatureId != lastLigatureId)
                {
                    break;
                }

                int currentComponent = current.LigatureComponent;
                if (currentComponent < 0)
                {
                    break;
                }

                current.LigatureId = ligatureId;
                current.LigatureComponent = currentComponentCount - lastComponentCount + Math.Min(currentComponent, lastComponentCount);
            }
        }

        // The matched component total is distinct from the text span represented by
        // the glyph because marks ignored by the lookup remain separate records.
        if (isLigature)
        {
            data.LigatureComponentCount = currentComponentCount;
        }

        buffer.Replace(index, matches, glyphId, ligatureId, ligatureComponent, feature);
    }

    /// <summary>
    /// Determines whether the specified glyph is a mark glyph based on its font-defined class or the fallback Unicode classification.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="shapingData">The glyph shaping data.</param>
    /// <returns><see langword="true"/> if the glyph is a mark; otherwise, <see langword="false"/>.</returns>
    public static bool IsMarkGlyph(FontMetrics fontMetrics, ushort glyphId, ref GlyphShapingData shapingData)
    {
        if (fontMetrics.TryGetGlyphClass(glyphId, out GlyphClassDef? glyphClass))
        {
            return glyphClass == GlyphClassDef.MarkGlyph;
        }

        return IsFallbackMark(ref shapingData);
    }

    /// <summary>
    /// Gets the glyph shaping class (mark, base, ligature, mark attachment type) for the specified glyph,
    /// using GDEF table data if available or falling back to Unicode properties.
    /// Results are cached on the <see cref="GlyphShapingData"/> instance, with
    /// table-derived classes additionally cached on the buffer so re-classification
    /// after a substitution changes a glyph id skips the table walks.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph shaping buffer carrying the class cache.</param>
    /// <param name="glyphId">The glyph identifier.</param>
    /// <param name="shapingData">The glyph shaping data, used for caching and Unicode fallback.</param>
    /// <returns>The <see cref="GlyphShapingClass"/>.</returns>
    public static GlyphShapingClass GetGlyphShapingClass(FontMetrics fontMetrics, ShapingBuffer buffer, ushort glyphId, ref GlyphShapingData shapingData)
    {
        // Cache the shaping class on the GlyphShapingData to avoid repeated GDEF lookups.
        // The cache key stores the glyph id; -1 means "not cached".
        if (shapingData.ShapingClassCacheKey == glyphId)
        {
            return shapingData.CachedShapingClass;
        }

        if (buffer.TryGetShapingClass(fontMetrics, glyphId, out GlyphShapingClass cached))
        {
            shapingData.CachedShapingClass = cached;
            shapingData.ShapingClassCacheKey = glyphId;
            return cached;
        }

        bool isMark;
        bool isBase;
        bool isLigature;
        bool tableDerived = false;
        ushort markAttachmentType = 0;
        if (fontMetrics.TryGetGlyphClass(glyphId, out GlyphClassDef? glyphClass))
        {
            isMark = glyphClass == GlyphClassDef.MarkGlyph;
            isBase = glyphClass == GlyphClassDef.BaseGlyph;
            isLigature = glyphClass == GlyphClassDef.LigatureGlyph;
            tableDerived = true;
            if (fontMetrics.TryGetMarkAttachmentClass(glyphId, out GlyphClassDef? markAttachmentClass))
            {
                markAttachmentType = (ushort)markAttachmentClass;
            }
        }
        else
        {
            // TODO: We may have to store each codepoint. FontKit checks all.
            isMark = IsFallbackMark(ref shapingData);
            isBase = !isMark;
            isLigature = shapingData.CodePointCount > 1;
        }

        GlyphShapingClass result = new(isMark, isBase, isLigature, markAttachmentType);
        if (tableDerived)
        {
            buffer.SetShapingClass(glyphId, result);
        }

        shapingData.CachedShapingClass = result;
        shapingData.ShapingClassCacheKey = glyphId;
        return result;
    }

    /// <summary>
    /// Determines whether a glyph without a font-defined class receives the fallback mark class.
    /// </summary>
    /// <param name="shapingData">The Unicode properties carried by the glyph.</param>
    /// <returns><see langword="true"/> when the glyph receives the mark class; otherwise, <see langword="false"/>.</returns>
    private static bool IsFallbackMark(ref GlyphShapingData shapingData)
    {
        // Spacing and enclosing marks retain their advances when a font supplies no
        // glyph classes. Default-ignorables are also bases so they remain visible to
        // lookup matching instead of being skipped by mark-specific lookup flags.
        return CodePoint.GetGeneralCategory(shapingData.CodePoint) == UnicodeCategory.NonSpacingMark
            && !shapingData.IsDefaultIgnorable;
    }

    /// <summary>
    /// Zeros the advances of mark glyphs in a positioned segment.
    /// </summary>
    /// <param name="fontMetrics">The font metrics supplying glyph classes.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the segment.</param>
    /// <param name="count">The number of glyphs in the segment.</param>
    /// <param name="adjustOffsets">Whether to move each mark back by its original advance before zeroing it.</param>
    public static void ZeroMarkAdvances(FontMetrics fontMetrics, ShapingBuffer buffer, int index, int count, bool adjustOffsets)
    {
        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (!IsMarkGlyph(fontMetrics, data.GlyphId, ref data))
            {
                continue;
            }

            ref GlyphShapingPosition position = ref buffer.PositionAt(i);
            if (adjustOffsets
                && (data.Direction == TextDirection.LeftToRight || buffer.TextOptions.LayoutMode.IsVertical()))
            {
                // With no positioning table, a forward-flowing mark hangs over the
                // preceding glyph after its advance is removed. Browsers normalize
                // bottom-to-top runs to forward top-to-bottom shaping, so pure
                // vertical layout follows the same offset adjustment.
                position.Bounds.X -= position.Bounds.Width;
                position.Bounds.Y -= position.Bounds.Height;
            }

            position.Bounds.Width = 0;
            position.Bounds.Height = 0;
        }
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
    /// <typeparam name="TMatcher">The compile-time comparison used for each sequence element.</typeparam>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of elements to match.</param>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <returns><see langword="true"/> if all elements in the sequence were matched; otherwise, <see langword="false"/>.</returns>
    private static bool Match<T, TMatcher>(int increment, T[] sequence, SkippingGlyphIterator iterator, Span<int> matches)
        where TMatcher : ISequenceMatcher<T>
        => Match<T, TMatcher>(increment, sequence, iterator, matches, out _);

    /// <summary>
    /// Matches a sequence of elements against glyphs using an increment-based
    /// approach, reporting the position one past the final matched element so
    /// lookahead can start exactly where the input ended.
    /// </summary>
    /// <typeparam name="T">The type of sequence elements to match.</typeparam>
    /// <typeparam name="TMatcher">The compile-time comparison used for each sequence element.</typeparam>
    /// <param name="increment">The initial increment from the iterator's current position.</param>
    /// <param name="sequence">The array of elements to match.</param>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <param name="matchEnd">The position one past the final matched element.</param>
    /// <returns><see langword="true"/> if all elements in the sequence were matched; otherwise, <see langword="false"/>.</returns>
    private static bool Match<T, TMatcher>(int increment, T[] sequence, SkippingGlyphIterator iterator, Span<int> matches, out int matchEnd)
        where TMatcher : ISequenceMatcher<T>
    {
        int position = iterator.Index;
        ShapingBuffer buffer = iterator.Collection;
        matchEnd = position + 1;
        int i = 0;

        // A buffer without a transparent record in reach keeps the solid-glyph
        // walk: every stepped-to record must match or the rule is refused.
        if (!iterator.MatchTransparencyActive)
        {
            int solidOffset = iterator.Increment(increment);
            if (solidOffset < 0)
            {
                return false;
            }

            while (i < sequence.Length && i < MaxContextLength && solidOffset < buffer.Count)
            {
                ref GlyphShapingData solidData = ref buffer[solidOffset];
                if (!iterator.MayMatch(ref solidData) || !TMatcher.Matches(sequence[i], ref solidData))
                {
                    break;
                }

                if (matches.Length == MaxContextLength)
                {
                    matches[i] = solidOffset;
                }

                i++;
                matchEnd = solidOffset + 1;
                solidOffset = iterator.Next();
            }

            iterator.Index = position;
            return i == sequence.Length;
        }

        // A single forward step enters the walk directly so a transparent record
        // at the next position still gets its chance to match the first sequence
        // element; larger jumps step over positions other matchers consumed.
        int offset;
        if (increment == 1)
        {
            offset = position + 1;
            iterator.Index = offset;
        }
        else
        {
            offset = iterator.Increment(increment);
        }

        if (offset < 0)
        {
            return false;
        }

        // A transparent record is stepped over unless it matches the sequence
        // position itself; a solid record that fails the shape test refuses the
        // whole match.
        while (i < sequence.Length && i < MaxContextLength && offset < buffer.Count)
        {
            if (iterator.IsPropertySkipped(offset))
            {
                offset = ++iterator.Index;
                continue;
            }

            ref GlyphShapingData data = ref buffer[offset];
            if (iterator.MayMatch(ref data) && TMatcher.Matches(sequence[i], ref data))
            {
                if (matches.Length == MaxContextLength)
                {
                    matches[i] = offset;
                }

                i++;
                matchEnd = offset + 1;
                offset = ++iterator.Index;
                continue;
            }

            if (!iterator.IsTransparent(ref data))
            {
                break;
            }

            offset = ++iterator.Index;
        }

        iterator.Index = position;
        return i == sequence.Length;
    }

    /// <summary>
    /// Matches a sequence of elements against glyphs using a directional (forward/backward) approach.
    /// </summary>
    /// <typeparam name="T">The type of sequence elements to match.</typeparam>
    /// <typeparam name="TMatcher">The compile-time comparison used for each sequence element.</typeparam>
    /// <param name="iterator">The skipping glyph iterator.</param>
    /// <param name="startIndex">The starting index in the buffer.</param>
    /// <param name="sequence">The array of elements to match.</param>
    /// <param name="direction">The direction to iterate (forward or backward).</param>
    /// <param name="endExclusive">The exclusive end index in the buffer.</param>
    /// <param name="matches">A span to store matched glyph indices, or default if not needed.</param>
    /// <param name="matchEnd">The position one past the final matched element; meaningful for forward matching.</param>
    /// <returns><see langword="true"/> if all elements in the sequence were matched; otherwise, <see langword="false"/>.</returns>
    private static bool Match<T, TMatcher>(SkippingGlyphIterator iterator, int startIndex, T[] sequence, MatchDirection direction, int endExclusive, Span<int> matches, out int matchEnd)
        where TMatcher : ISequenceMatcher<T>
    {
        matchEnd = startIndex;
        if (sequence.Length == 0)
        {
            return true;
        }

        int saved = iterator.Index;
        int offset = startIndex;
        int step = direction == MatchDirection.Forward ? 1 : -1;

        // Backtrack reads the records the pass produced, so that side bounds it.
        int limit = Math.Min(endExclusive, iterator.RecordCount);

        // A transparent record is stepped over unless it matches the sequence
        // position itself; a solid record that fails the shape test refuses the
        // whole match.
        int i = 0;
        while (i < sequence.Length && i < MaxContextLength)
        {
            if (offset < 0 || offset >= limit)
            {
                iterator.Index = saved;
                return false;
            }

            if (iterator.IsPropertySkipped(offset))
            {
                offset += step;
                continue;
            }

            ref GlyphShapingData data = ref iterator.RecordAt(offset);
            if (iterator.MayMatch(ref data) && TMatcher.Matches(sequence[i], ref data))
            {
                if (matches.Length == MaxContextLength)
                {
                    matches[i] = offset;
                }

                i++;
                matchEnd = offset + 1;
                offset += step;
                continue;
            }

            if (!iterator.IsTransparent(ref data))
            {
                iterator.Index = saved;
                return false;
            }

            offset += step;
        }

        iterator.Index = saved;
        return true;
    }

    /// <summary>
    /// Compares a sequence glyph identifier with the current glyph record.
    /// </summary>
    private readonly struct GlyphIdSequenceMatcher : ISequenceMatcher<ushort>
    {
        /// <inheritdoc/>
        public static bool Matches(ushort element, ref GlyphShapingData data) => element == data.GlyphId;
    }

    /// <summary>
    /// Tests the current glyph record against a sequence coverage table.
    /// </summary>
    private readonly struct CoverageSequenceMatcher : ISequenceMatcher<CoverageTable>
    {
        /// <inheritdoc/>
        public static bool Matches(CoverageTable element, ref GlyphShapingData data) => element.CoverageIndexOf(data.GlyphId) >= 0;
    }
}
