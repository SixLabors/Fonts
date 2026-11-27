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
    /// The maximum number of glyph slots in a context match. Matches HarfBuzz's hb-limits.hh.
    /// </summary>
    public const int MaxContextLength = 64;

    /// <summary>
    /// The maximum depth of nested lookup application. Contextual lookups can call
    /// one another without an end condition. The depth limit stops that recursion.
    /// </summary>
    public const int MaxNestingLevel = 64;

    /// <summary>
    /// The owner value of a deletion outside a state table stream.
    /// </summary>
    public const int NoDeletionOwner = int.MaxValue;

    // These shaping limits match hb-limits.hh in the pinned HarfBuzz source.
    // They bound glyph growth and lookup work. Table loaders check byte ranges separately.

    /// <summary>
    /// The factor that sets the glyph count limit from the input length.
    /// </summary>
    private const int MaxLengthFactor = 512;

    /// <summary>
    /// The smallest glyph count limit. Short input can produce many glyphs.
    /// </summary>
    private const int MaxLengthMinimum = 65536;

    /// <summary>
    /// The factor that sets the operation limit from the input length.
    /// </summary>
    private const int MaxOperationsFactor = 65536;

    /// <summary>
    /// The smallest operation limit. Short input can require many table passes.
    /// </summary>
    private const int MaxOperationsMinimum = 524288;

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
    /// Gets whether the glyph uses vertical metrics by default in the specified layout mode.
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

        return layoutMode.IsVerticalMixed() && IsVerticalGlyph(CodePoint.GetVerticalOrientationType(codePoint));
    }

    /// <summary>
    /// Gets whether the default glyph is upright in vertical text.
    /// </summary>
    /// <param name="orientation">The Unicode orientation of the character.</param>
    /// <returns>Whether the default glyph is upright.</returns>
    public static bool IsVerticalGlyph(VerticalOrientationType orientation)
        => orientation is VerticalOrientationType.Upright or VerticalOrientationType.TransformUpright;

    /// <summary>
    /// Counts the deletion events before a live glyph or state table index.
    /// </summary>
    /// <param name="events">The ordered deletion events.</param>
    /// <param name="index">The exclusive index to count up to.</param>
    /// <param name="liveBoundary">Whether the index is in the live glyph buffer.</param>
    /// <returns>The number of events before the index.</returns>
    public static int CountDeletionsBefore(ReadOnlySpan<(int Index, int Owner, GlyphShapingData Glyph)> events, int index, bool liveBoundary)
    {
        int low = 0;
        int high = events.Length;
        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            int eventIndex = liveBoundary ? events[mid].Owner : events[mid].Index;
            if (eventIndex < index)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
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

        int end = matchEnd;
        buffer.PushNestedApplication();

        foreach (SequenceLookupRecord lookupRecord in records)
        {
            int sequenceIndex = lookupRecord.SequenceIndex;
            if (sequenceIndex >= matchCount)
            {
                continue;
            }

            int total = buffer.SpanEnd;
            int position = matchPositions[sequenceIndex];

            // An earlier nested lookup can consume enough records to strand a
            // later sequence position past everything that remains.
            if (position >= total)
            {
                continue;
            }

            // An earlier nested lookup can move the cursor past this position.
            // Use the live index that tracks the matched glyph.
            buffer.MoveTo(position);

            GSub.LookupTable lookup = table.LookupList.LookupTables[lookupRecord.LookupListIndex];
            _ = lookup.TrySubstitution(fontMetrics, table, buffer, feature, lookupMask, buffer.ReadIndex, buffer.PassLookaheadLength);

            int delta = buffer.SpanEnd - total;
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
        buffer.MoveTo(matchEnd);
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

            while (i < sequence.Length && i < MaxContextLength && solidOffset < buffer.SpanEnd)
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
        while (i < sequence.Length && i < MaxContextLength && offset < buffer.SpanEnd)
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
        int first = iterator.Collection.SpanStart;
        int i = 0;
        while (i < sequence.Length && i < MaxContextLength)
        {
            if (offset < first || offset >= limit)
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

        // Round the difference once so fractional variation deltas from the
        // two anchors combine before the result enters integer positioning.
        markPosition.Bounds.X = (int)MathF.Round(baseXY.XCoordinate - markXY.XCoordinate, MidpointRounding.AwayFromZero);
        markPosition.Bounds.Y = (int)MathF.Round(baseXY.YCoordinate - markXY.YCoordinate, MidpointRounding.AwayFromZero);
        markPosition.MarkAttachment = baseGlyphIndex;
        markData.AppliedFeatureMask |= ShapePlanFeatures.GetVerticalMask(feature);
    }

    /// <summary>
    /// Clears the advances and the offset along the layout axis for unchanged default ignorable glyphs.
    /// </summary>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="index">The starting index.</param>
    /// <param name="count">The number of glyphs to process.</param>
    public static void ZeroDefaultIgnorableAdvances(ShapingBuffer buffer, int index, int count)
    {
        if (!buffer.HasDefaultIgnorables)
        {
            return;
        }

        bool isVertical = buffer.TextOptions.LayoutMode.IsVertical();
        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (!data.IsDefaultIgnorable || data.IsSubstituted)
            {
                continue;
            }

            // Attachment resolution sums the advances between the base and the mark.
            // Clear these advances before the sum. Keep the offset across the layout axis.
            ref GlyphShapingPosition position = ref buffer.PositionAt(i);
            position.Bounds.Width = 0;
            position.Bounds.Height = 0;
            if (isVertical)
            {
                position.Bounds.Y = 0;
            }
            else
            {
                position.Bounds.X = 0;
            }
        }
    }

    /// <summary>
    /// Resolves the mark links set by the selected positioning subtable.
    /// </summary>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="start">The first glyph index in the run.</param>
    /// <param name="end">The glyph index after the run.</param>
    public static void ResolveMarkAttachments(ShapingBuffer buffer, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            if (buffer.PositionAt(i).MarkAttachment != -1)
            {
                FixMarkAttachment(buffer, start, end, i, MaxNestingLevel);
            }
        }
    }

    /// <summary>
    /// Converts out-of-range advances after positioning. Attachment resolution
    /// uses the original pen steps before this conversion preserves their geometry.
    /// </summary>
    /// <param name="buffer">The shaped glyph buffer.</param>
    public static void NormalizeAdvances(ShapingBuffer buffer)
    {
        int start = buffer.SpanStart;
        int end = buffer.SpanEnd;
        bool verticalLayout = buffer.TextOptions.LayoutMode.IsVertical();
        while (start < end)
        {
            int grapheme = buffer[start].GraphemeIndex;
            int next = start;
            bool normalizeWidth = false;
            bool normalizeHeight = false;
            do
            {
                ref GlyphShapingBounds bounds = ref buffer.PositionAt(next).Bounds;
                normalizeWidth |= (uint)bounds.Width > ushort.MaxValue;
                normalizeHeight |= (uint)bounds.Height > ushort.MaxValue;
                next++;
            }
            while (next < end && buffer[next].GraphemeIndex == grapheme);

            bool backwards = !verticalLayout && buffer[start].Direction == TextDirection.RightToLeft;
            int first = backwards ? next - 1 : start;
            int stop = backwards ? start - 1 : next;
            int step = backwards ? -1 : 1;
            if (normalizeWidth)
            {
                NormalizeGraphemeAdvance(buffer, first, stop, step, false);
            }

            if (normalizeHeight)
            {
                NormalizeGraphemeAdvance(buffer, first, stop, step, true);
            }

            start = next;
        }
    }

    /// <summary>
    /// Moves an advance adjustment within one grapheme and preserves the glyph origins.
    /// </summary>
    /// <param name="buffer">The shaped glyph buffer.</param>
    /// <param name="first">The first glyph in visual order.</param>
    /// <param name="stop">The index after the last glyph in visual order.</param>
    /// <param name="step">The index increment in visual order.</param>
    /// <param name="vertical">Whether to adjust heights and Y offsets.</param>
    private static void NormalizeGraphemeAdvance(ShapingBuffer buffer, int first, int stop, int step, bool vertical)
    {
        long carry = 0;
        long advanceTotal = 0;
        for (int i = first; i != stop; i += step)
        {
            ref GlyphShapingBounds bounds = ref buffer.PositionAt(i).Bounds;
            int advance = vertical ? bounds.Height : bounds.Width;
            long requested = carry + advance;
            int reduced = (int)Math.Clamp(requested, 0, ushort.MaxValue);

            // The earlier pen step changed by the carried amount. Move the
            // current origin to compensate and carry any remaining spacing forward.
            if (vertical)
            {
                bounds.Y -= (int)carry;
                bounds.Height = reduced;
            }
            else
            {
                bounds.X += (int)carry;
                bounds.Width = reduced;
            }

            carry = requested - reduced;
            advanceTotal += reduced;
        }

        // At the end of a grapheme, absorb any remainder into earlier pen
        // steps. Keep the spacing inside the grapheme so wrapping keeps it
        // with its glyphs. The available range is the sum of unsigned advances.
        long capacity = (long)Math.Abs(stop - first) * ushort.MaxValue;
        long remaining = Math.Clamp(carry, -advanceTotal, capacity - advanceTotal);
        for (int i = stop - step; remaining != 0; i -= step)
        {
            ref GlyphShapingBounds bounds = ref buffer.PositionAt(i).Bounds;
            int advance = vertical ? bounds.Height : bounds.Width;
            int change = (int)Math.Clamp(remaining, -advance, ushort.MaxValue - advance);
            remaining -= change;

            // Earlier steps will absorb the remaining change. Their new
            // pen positions must leave this glyph at its original origin.
            if (vertical)
            {
                bounds.Height += change;
                bounds.Y += (int)remaining;
            }
            else
            {
                bounds.Width += change;
                bounds.X -= (int)remaining;
            }
        }
    }

    /// <summary>
    /// Resolves a mark's offset using its parent's position, vertical origin, and the intervening pen steps.
    /// </summary>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="start">The first glyph index in the range.</param>
    /// <param name="end">The glyph index after the range.</param>
    /// <param name="currentIndex">The glyph whose attachment needs resolution.</param>
    /// <param name="remainingDepth">The number of parent links that may still be followed.</param>
    private static void FixMarkAttachment(ShapingBuffer buffer, int start, int end, int currentIndex, int remainingDepth)
    {
        ref GlyphShapingPosition position = ref buffer.PositionAt(currentIndex);
        int baseIndex = position.MarkAttachment;

        // Consume each link once. Clearing it before following the parent also ends cycles.
        position.MarkAttachment = -1;
        if (remainingDepth == 0 || baseIndex < start || baseIndex >= end)
        {
            return;
        }

        // A link can point to a glyph that the buffer scan will reach later.
        if (buffer.PositionAt(baseIndex).MarkAttachment != -1)
        {
            FixMarkAttachment(buffer, start, end, baseIndex, remainingDepth - 1);
        }

        bool isVertical = IsVerticalGlyph(buffer[currentIndex].CodePoint, buffer.TextOptions.LayoutMode);
        if (isVertical)
        {
            GetVerticalOrigin(buffer.MetricsAt(currentIndex).Metrics, buffer.UseShapingVerticalOrigin, out int markOriginX, out int markOriginY);
            GetVerticalOrigin(buffer.MetricsAt(baseIndex).Metrics, buffer.UseShapingVerticalOrigin, out int baseOriginX, out int baseOriginY);

            // The anchor delta is relative to the parent. The caller applies
            // each glyph's origin later. Add the mark origin and subtract the
            // parent origin so the anchors meet after the caller applies them.
            position.Bounds.X += markOriginX - baseOriginX;
            position.Bounds.Y += markOriginY - baseOriginY;
        }

        position.Bounds.X += buffer.PositionAt(baseIndex).Bounds.X;
        position.Bounds.Y += buffer.PositionAt(baseIndex).Bounds.Y;

        bool rightToLeft = buffer[currentIndex].Direction == TextDirection.RightToLeft;
        int firstAdvance = Math.Min(baseIndex, currentIndex);
        int endAdvance = Math.Max(baseIndex, currentIndex);
        bool subtractAdvance = baseIndex < currentIndex;
        bool reverseGraphemes = rightToLeft && buffer.TextOptions.LayoutMode.IsVertical()
            && buffer[currentIndex].GraphemeIndex != buffer[baseIndex].GraphemeIndex;

        if (rightToLeft && !isVertical)
        {
            subtractAdvance = !subtractAdvance;
        }

        int width = 0;
        int height = 0;
        if (reverseGraphemes)
        {
            // Bottom-to-top reverses whole graphemes and preserves their inner order.
            subtractAdvance = !subtractAdvance;
            SumBottomToTopAdvances(buffer, start, end, firstAdvance, endAdvance, out width, out height);
        }
        else
        {
            // Horizontal RTL includes the child's pen step instead of the parent's.
            if (rightToLeft && !isVertical)
            {
                firstAdvance++;
                endAdvance++;
            }

            for (int i = firstAdvance; i < endAdvance; i++)
            {
                ref GlyphShapingBounds advance = ref buffer.PositionAt(i).Bounds;
                width += advance.Width;
                height += advance.Height;
            }
        }

        // Vertical pen steps advance down the page. Offset Y is positive up.
        position.Bounds.X += subtractAdvance ? -width : width;
        position.Bounds.Y += subtractAdvance == isVertical ? height : -height;
    }

    /// <summary>
    /// Sums the pen steps between two glyphs in a bottom-to-top run.
    /// </summary>
    /// <param name="buffer">The shaped glyph buffer in logical order.</param>
    /// <param name="start">The first index in the range.</param>
    /// <param name="end">The index after the range.</param>
    /// <param name="lower">The smaller logical index, which comes later visually.</param>
    /// <param name="higher">The larger logical index, which comes earlier visually.</param>
    /// <param name="width">The total width.</param>
    /// <param name="height">The total height.</param>
    private static void SumBottomToTopAdvances(ShapingBuffer buffer, int start, int end, int lower, int higher, out int width, out int height)
    {
        int lowerGrapheme = buffer[lower].GraphemeIndex;
        int higherGrapheme = buffer[higher].GraphemeIndex;
        width = 0;
        height = 0;

        // Visual order keeps the suffix of the first grapheme, every
        // whole grapheme between them, and the prefix of the last grapheme.
        // The glyph order inside each grapheme stays the same.
        for (int i = higher; i < end && buffer[i].GraphemeIndex == higherGrapheme; i++)
        {
            ref GlyphShapingBounds advance = ref buffer.PositionAt(i).Bounds;
            width += advance.Width;
            height += advance.Height;
        }

        for (int i = higher - 1; i > lower; i--)
        {
            int grapheme = buffer[i].GraphemeIndex;
            if (grapheme == lowerGrapheme || grapheme == higherGrapheme)
            {
                continue;
            }

            ref GlyphShapingBounds advance = ref buffer.PositionAt(i).Bounds;
            width += advance.Width;
            height += advance.Height;
        }

        for (int i = lower - 1; i >= start && buffer[i].GraphemeIndex == lowerGrapheme; i--)
        {
            ref GlyphShapingBounds advance = ref buffer.PositionAt(i).Bounds;
            width += advance.Width;
            height += advance.Height;
        }
    }

    /// <summary>
    /// Gets the vertical origin for a glyph in font design units.
    /// </summary>
    /// <param name="metrics">The glyph metrics.</param>
    /// <param name="useShapingVerticalOrigin">Whether to use the shaping fallback when the font has no vertical metrics.</param>
    /// <param name="x">The horizontal origin.</param>
    /// <param name="y">The vertical origin.</param>
    public static void GetVerticalOrigin(FontGlyphMetrics metrics, bool useShapingVerticalOrigin, out int x, out int y)
    {
        // OpenType vertical origins are horizontally centered on the glyph's
        // nominal horizontal advance, using integer division in font units.
        x = metrics.AdvanceWidth / 2;

        VerticalMetrics verticalMetrics = metrics.FontMetrics.VerticalMetrics;
        if (verticalMetrics.Synthesized && useShapingVerticalOrigin)
        {
            // Public shaping follows the shaping-engine fallback by centering each
            // glyph's extents within the font's ascender-to-descender height.
            float fontAdvance = verticalMetrics.Ascender - verticalMetrics.Descender;
            y = (int)(metrics.Bounds.Max.Y + MathF.Floor((fontAdvance - metrics.Height) * .5F));
            return;
        }

        // Authored origins are shared by both contracts. When tables are absent,
        // synthesized TopSideBearing resolves to the font-ascent origin used by
        // browsers for layout and painting.
        y = (int)(metrics.Bounds.Max.Y + metrics.TopSideBearing);
    }

    /// <summary>
    /// Adjusts the advance of a glyph that belongs to the positioning font.
    /// </summary>
    /// <param name="fontMetrics">The font that supplies the adjustment.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="index">The glyph index.</param>
    /// <param name="glyphId">The glyph ID that the adjustment applies to.</param>
    /// <param name="dx">The horizontal advance adjustment in font design units.</param>
    /// <param name="dy">The vertical advance adjustment in upward-positive font design units.</param>
    public static void ApplyAdvance(FontMetrics fontMetrics, ShapingBuffer buffer, int index, ushort glyphId, int dx, int dy)
    {
        FontGlyphMetrics m = buffer.MetricsAt(index).Metrics;
        if (m.GlyphId != glyphId || fontMetrics != m.FontMetrics)
        {
            return;
        }

        bool isVertical = IsVerticalGlyph(m.CodePoint, buffer.TextOptions.LayoutMode)
            || (buffer[index].AppliedFeatureMask & ShapePlanFeatures.VerticalFeatureMask) != 0;

        ref GlyphShapingBounds bounds = ref buffer.PositionAt(index).Bounds;

        // Advance heights grow downward but font-space grows upward, hence the negation.
        bounds.Width += dx;
        if (isVertical)
        {
            bounds.Height -= dy;
        }

        if (bounds.HasOutOfRangeAdvance)
        {
            buffer.HasOutOfRangeAdvances = true;
        }
    }

    /// <summary>
    /// Applies the positioning adjustment for a kerning pair.
    /// </summary>
    /// <param name="fontMetrics">The font that owns the pair.</param>
    /// <param name="buffer">The glyph positioning buffer.</param>
    /// <param name="left">The index of the first glyph in the pair.</param>
    /// <param name="right">The index of the second glyph in the pair.</param>
    /// <param name="dx">The horizontal adjustment in font design units.</param>
    /// <param name="dy">The vertical adjustment in font design units.</param>
    /// <param name="yDown">Whether a positive vertical adjustment moves down.</param>
    public static void ApplyPairKerning(FontMetrics fontMetrics, ShapingBuffer buffer, int left, int right, int dx, int dy, bool yDown)
    {
        // Give half of the adjustment to the first glyph's advance. The
        // signed shift rounds down, so the second advance gets the remainder.
        // Together the two advances move the pen after the pair by the full adjustment.
        int firstX = dx >> 1;
        int firstY = dy >> 1;
        int secondX = dx - firstX;
        int secondY = dy - firstY;

        // Split in the table's coordinate system so that odd values give
        // the same remainder before conversion to the font's Y axis.
        if (yDown)
        {
            firstY = -firstY;
            secondY = -secondY;
        }

        ApplyAdvance(fontMetrics, buffer, left, buffer[left].GlyphId, firstX, firstY);
        ApplyAdvance(fontMetrics, buffer, right, buffer[right].GlyphId, secondX, secondY);

        // The first advance moves the pen before the second glyph is drawn.
        // The offset supplies the rest of the adjustment to that glyph's origin.
        ref GlyphShapingBounds bounds = ref buffer.PositionAt(right).Bounds;
        bounds.X += secondX;
        bounds.Y += secondY;
    }

    /// <summary>
    /// Reads a VariationIndex from a device offset. The caller restores the stream position if needed.
    /// </summary>
    /// <param name="reader">The positioning table reader.</param>
    /// <param name="parentBase">The absolute position of the parent table.</param>
    /// <param name="deviceOffset">The offset of the device table from the parent table.</param>
    /// <returns>The one-based packed VariationIndex, or zero if there is no variation.</returns>
    public static uint ReadVariationIndex(BigEndianBinaryReader reader, long parentBase, ushort deviceOffset)
    {
        long offset = parentBase + deviceOffset;
        if (deviceOffset == 0 || offset > reader.BaseStream.Length - (3 * sizeof(ushort)))
        {
            return 0;
        }

        reader.BaseStream.Position = offset;
        ushort outerIndex = reader.ReadUInt16();
        ushort innerIndex = reader.ReadUInt16();
        ushort format = reader.ReadUInt16();

        // This format identifies a VariationIndex. Device tables for
        // hinting adjustments use the same header with a different format.
        const ushort variationIndexFormat = 0x8000;
        if (format != variationIndexFormat)
        {
            return 0;
        }

        // Put the outer index in the high 16 bits and the inner index in the low
        // 16 bits. Zero selects the first delta set. Store the index plus one so
        // zero in a value record means no variation. Adding one to an all-ones index wraps to zero.
        uint packedIndex = ((uint)outerIndex << 16) | innerIndex;
        return packedIndex == uint.MaxValue ? 0 : packedIndex + 1;
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

        if (position.Bounds.HasOutOfRangeAdvance)
        {
            buffer.HasOutOfRangeAdvances = true;
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

            while (i < sequence.Length && i < MaxContextLength && solidOffset < buffer.SpanEnd)
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
        while (i < sequence.Length && i < MaxContextLength && offset < buffer.SpanEnd)
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
        int first = iterator.Collection.SpanStart;

        // A transparent record is stepped over unless it matches the sequence
        // position itself; a solid record that fails the shape test refuses the
        // whole match.
        int i = 0;
        while (i < sequence.Length && i < MaxContextLength)
        {
            if (offset < first || offset >= limit)
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
