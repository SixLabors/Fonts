// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using SixLabors.Fonts.Tables.AdvancedTypographic.GPos;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    internal static class AdvancedTypographicUtils
    {
        /// <summary>
        /// The maximum length of a context. Taken from HarfBuzz - hb-ot-layout-common.hh
        /// </summary>
        public const int MaxContextLength = 64;

        internal static bool MatchInputSequence(
            IGlyphShapingCollection collection,
            Tag feature,
            ushort index,
            ushort[] inputSequence,
            Span<int> matches)
        {
            int startIdx = index + 1;
            int i = 0;
            while (i < inputSequence.Length && i < MaxContextLength)
            {
                int collectionIdx = startIdx + i;
                if (collectionIdx == collection.Count)
                {
                    return false;
                }

                GlyphShapingData data = collection.GetGlyphShapingData(collectionIdx);
                if (!ContainsFeatureTag(data.Features, feature))
                {
                    return false;
                }

                ushort glyphId = data.GlyphIds[0];
                if (glyphId != inputSequence[i])
                {
                    return false;
                }

                matches[i++] = collectionIdx;
            }

            return i == inputSequence.Length;
        }

        internal static bool ContainsFeatureTag(List<TagEntry> featureList, Tag feature)
        {
            foreach (TagEntry tagEntry in featureList)
            {
                if (tagEntry.Tag == feature)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool MatchSequence(IGlyphShapingCollection collection, int glyphSequenceIndex, ushort[] sequenceToMatch)
        {
            int sequenceToMatchIdx = 0;
            int glyphCollectionIdx = glyphSequenceIndex + 1;

            while (sequenceToMatchIdx < sequenceToMatch.Length)
            {
                if (glyphCollectionIdx >= collection.Count)
                {
                    return false;
                }

                ReadOnlySpan<ushort> glyphIds = collection[glyphCollectionIdx++];
                ushort glyphId = glyphIds[0];
                if (glyphId != sequenceToMatch[sequenceToMatchIdx++])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool MatchClassSequence(
            IGlyphShapingCollection collection,
            int glyphSequenceIndex,
            ushort[] sequenceToMatch,
            ClassDefinitionTable classDefinitionTable)
        {
            int sequenceToMatchIdx = 0;
            int glyphCollectionIdx = glyphSequenceIndex + 1;

            while (sequenceToMatchIdx < sequenceToMatch.Length)
            {
                if (glyphCollectionIdx >= collection.Count)
                {
                    return false;
                }

                ReadOnlySpan<ushort> glyphIds = collection[glyphCollectionIdx++];
                if (!MatchClass(sequenceToMatchIdx++, sequenceToMatch, classDefinitionTable, glyphIds))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool ApplyChainedSequenceRule(IGlyphShapingCollection collection, Tag feature, ushort index, ChainedSequenceRuleTable rule)
        {
            if (rule.BacktrackSequence.Length > 0
                && !MatchSequence(collection, -rule.BacktrackSequence.Length, rule.BacktrackSequence))
            {
                return false;
            }

            if (rule.LookaheadSequence.Length > 0
                && !MatchSequence(collection, 1 + rule.InputSequence.Length, rule.LookaheadSequence))
            {
                return false;
            }

            Span<int> matches = stackalloc int[MaxContextLength];
            if (rule.InputSequence.Length > 0
                && !MatchInputSequence(collection, feature, index, rule.InputSequence, matches))
            {
                return false;
            }

            return true;
        }

        internal static bool ApplyChainedClassSequenceRule(
            IGlyphShapingCollection collection,
            ushort index,
            ChainedClassSequenceRuleTable rule,
            ClassDefinitionTable inputClassDefinitionTable,
            ClassDefinitionTable backtrackClassDefinitionTable,
            ClassDefinitionTable lookaheadClassDefinitionTable)
        {
            if (rule.BacktrackSequence.Length > 0
                && !MatchClassSequence(collection, -rule.BacktrackSequence.Length, rule.BacktrackSequence, backtrackClassDefinitionTable))
            {
                return false;
            }

            if (rule.InputSequence.Length > 0 &&
                !MatchClassSequence(collection, index, rule.InputSequence, inputClassDefinitionTable))
            {
                return false;
            }

            if (rule.LookaheadSequence.Length > 0
                && !MatchClassSequence(collection, 1 + rule.InputSequence.Length, rule.LookaheadSequence, lookaheadClassDefinitionTable))
            {
                return false;
            }

            return true;
        }

        internal static bool CheckAllCoverages(
            FontMetrics fontMetrics,
            LookupFlags lookupFlags,
            IGlyphShapingCollection collection,
            ushort index,
            int count,
            CoverageTable[] input,
            CoverageTable[] backtrack,
            CoverageTable[] lookahead)
        {
            // Check that there are enough context glyphs.
            if (index - backtrack.Length < 0 || input.Length + lookahead.Length > count)
            {
                return false;
            }

            // Check all coverages: if any of them does not match, abort update.
            SkippingGlyphIterator iterator = new(fontMetrics, collection, index, lookupFlags);
            if (!CheckCoverage(iterator, input, 0))
            {
                return false;
            }

            iterator.Reset(index, lookupFlags);
            if (!CheckCoverage(iterator, backtrack, -backtrack.Length))
            {
                return false;
            }

            iterator.Reset(index, lookupFlags);
            if (!CheckCoverage(iterator, lookahead, input.Length))
            {
                return false;
            }

            return true;
        }

        internal static void ApplyAnchor(
            FontMetrics fontMetrics,
            GlyphPositioningCollection collection,
            ushort index,
            AnchorTable baseAnchor,
            MarkRecord markRecord,
            ushort baseGlyphIndex,
            ushort baseGlyphId,
            ushort glyphId)
        {
            short baseX = baseAnchor.XCoordinate;
            short baseY = baseAnchor.YCoordinate;
            short markX = markRecord.MarkAnchorTable.XCoordinate;
            short markY = markRecord.MarkAnchorTable.YCoordinate;

            FontRectangle baseBounds = collection.GetAdvanceBounds(fontMetrics, baseGlyphIndex, baseGlyphId);
            Vector2 glyphOffset = collection.GetOffset(fontMetrics, index, glyphId);

            // Negate original offset to reset position to 0,0.
            short xo = (short)(glyphOffset.X * -1);
            short yo = (short)(glyphOffset.Y * -1);

            // Now offset to match the base position.
            // Advance bounds width/height already include the bounds min offset
            xo -= (short)baseBounds.Width;
            yo += (short)baseBounds.Y;

            // Now add new offset.
            xo += (short)(baseX - markX);
            yo += (short)(baseY - markY);

            // TODO: Consider vertical layout modes. TTB and BBT
            collection.Offset(fontMetrics, index, glyphId, xo, yo);
        }

        internal static bool IsMarkGlyph(FontMetrics fontMetrics, ushort glyphId, GlyphShapingData shapingData)
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

        internal static GlyphShapingClass GetGlyphShapingClass(FontMetrics fontMetrics, ushort glyphId, GlyphShapingData shapingData)
        {
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
                isMark = CodePoint.IsMark(shapingData.CodePoint);
                isBase = !isMark;
                isLigature = shapingData.LigatureComponentCount > 1;
            }

            return new GlyphShapingClass(isMark, isBase, isLigature, markAttachmentType);
        }

        private static bool CheckCoverage(
            SkippingGlyphIterator iterator,
            CoverageTable[] coverageTable,
            int offset)
        {
            IGlyphShapingCollection collection = iterator.Collection;
            offset = iterator.Increment(offset);
            for (int i = 0; i < coverageTable.Length && offset < collection.Count; i++)
            {
                ushort id = collection[offset][0];
                if (id == 0 || coverageTable[i].CoverageIndexOf(id) < 0)
                {
                    return false;
                }

                offset = iterator.Next();
            }

            return true;
        }

        private static bool MatchClass(int idx, ushort[] sequence, ClassDefinitionTable classDefinitionTable, ReadOnlySpan<ushort> glyphIds)
        {
            ushort glyphId = glyphIds[0];
            int glyphIdClass = classDefinitionTable.ClassIndexOf(glyphId);
            ushort sequenceEntryClassId = sequence[idx];
            return glyphIdClass == sequenceEntryClassId;
        }
    }
}
