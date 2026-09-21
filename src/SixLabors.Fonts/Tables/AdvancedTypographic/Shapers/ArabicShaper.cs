// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// This is a shaper for Arabic, and other cursive scripts.
/// </summary>
/// <remarks>
/// The joining state machine and feature order follow <c>hb-ot-shaper-arabic.cc</c>.
/// </remarks>
internal sealed class ArabicShaper : DefaultShaper
{
    /// <summary>
    /// The canonical combining class for marks placed below a base.
    /// </summary>
    private const int BelowMarkOrder = 220;

    /// <summary>
    /// The canonical combining class for marks placed above a base.
    /// </summary>
    private const int AboveMarkOrder = 230;

    /// <summary>
    /// The temporary ordering class assigned to reordered marks below a base.
    /// </summary>
    private const int ReorderedBelowMarkOrder = 22;

    /// <summary>
    /// The temporary ordering class assigned to reordered marks above a base.
    /// </summary>
    private const int ReorderedAboveMarkOrder = 26;

    /// <summary>
    /// The 'mset' (mark positioning via substitution) feature tag.
    /// </summary>
    private static readonly Tag MsetTag = Tag.Parse("mset");

    /// <summary>
    /// The 'stch' (stretching glyph decomposition) feature tag.
    /// </summary>
    private static readonly Tag StchTag = Tag.Parse("stch");

    /// <summary>
    /// The 'fina' (terminal forms) feature tag.
    /// </summary>
    private static readonly Tag FinaTag = Tag.Parse("fina");

    /// <summary>
    /// The 'fin2' (terminal forms #2) feature tag.
    /// </summary>
    private static readonly Tag Fin2Tag = Tag.Parse("fin2");

    /// <summary>
    /// The 'fin3' (terminal forms #3) feature tag.
    /// </summary>
    private static readonly Tag Fin3Tag = Tag.Parse("fin3");

    /// <summary>
    /// The 'isol' (isolated forms) feature tag.
    /// </summary>
    private static readonly Tag IsolTag = Tag.Parse("isol");

    /// <summary>
    /// The 'init' (initial forms) feature tag.
    /// </summary>
    private static readonly Tag InitTag = Tag.Parse("init");

    /// <summary>
    /// The 'medi' (medial forms) feature tag.
    /// </summary>
    private static readonly Tag MediTag = Tag.Parse("medi");

    /// <summary>
    /// The 'med2' (medial forms #2) feature tag.
    /// </summary>
    private static readonly Tag Med2Tag = Tag.Parse("med2");

    /// <summary>
    /// No joining action.
    /// </summary>
    private const byte None = 0;

    /// <summary>
    /// Isolated form action.
    /// </summary>
    private const byte Isol = 1;

    /// <summary>
    /// Final form action.
    /// </summary>
    private const byte Fina = 2;

    /// <summary>
    /// Final form #2 action (for ALAPH).
    /// </summary>
    private const byte Fin2 = 3;

    /// <summary>
    /// Final form #3 action (for ALAPH after DALATH RISH).
    /// </summary>
    private const byte Fin3 = 4;

    /// <summary>
    /// Medial form action.
    /// </summary>
    private const byte Medi = 5;

    /// <summary>
    /// Medial form #2 action (for ALAPH).
    /// </summary>
    private const byte Med2 = 6;

    /// <summary>
    /// Initial form action.
    /// </summary>
    private const byte Init = 7;

    /// <summary>
    /// The pause action separating joining-form lookup stages.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> pauseAction;

    /// <summary>
    /// The action recording fixed and repeating pieces produced by stretch
    /// decomposition.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int> recordStretchPartsAction;

    /// <summary>
    /// The action applying presentation-form fallback after required ligatures.
    /// </summary>
    private readonly Action<ShapePlan, ShapingBuffer, int, int>? fallbackAction;

    /// <summary>
    /// Whether contextual alternates must complete before the common ligature
    /// features begin.
    /// </summary>
    private readonly bool pauseAfterContextualAlternates;

    /// <summary>
    /// Whether the selected script and language provide stretch decomposition.
    /// </summary>
    private readonly bool hasStretchFeature;

    /// <summary>
    /// The maximum number of fixed and repeated tiles emitted for one stretch
    /// decomposition.
    /// </summary>
    private const int MaximumStretchGlyphs = 256;

    /// <summary>
    /// The font-resolved fallback substitutions, created on first use.
    /// </summary>
    private ArabicFallbackSubstitutions? fallbackSubstitutions;

    /// <summary>
    /// Arabic joining state machine table. Each entry is [prevAction, curAction, nextState].
    /// Rows are states (0-6), columns are joining type categories.
    /// </summary>
    private static readonly byte[,][] StateTable =
    {
        // #           NonJoining,                    LeftJoining,                 RightJoining,                 DualJoining,                    ALAPH,                     DALATH RISH
        // State 0: prev was U,  not willing to join.
        { [None, None, 0], [None, Isol, 2], [None, Isol, 1], [None, Isol, 2], [None, Isol, 1], [None, Isol, 6] },

        // State 1: prev was R or ISOL/ALAPH,  not willing to join.
        { [None, None, 0], [None, Isol, 2], [None, Isol, 1], [None, Isol, 2], [None, Fin2, 5], [None, Isol, 6] },

        // State 2: prev was D/L in ISOL form,  willing to join.
        { [None, None, 0], [None, Isol, 2], [Init, Fina, 1], [Init, Fina, 3], [Init, Fina, 4], [Init, Fina, 6] },

        // State 3: prev was D in FINA form,  willing to join.
        { [None, None, 0], [None, Isol, 2], [Medi, Fina, 1], [Medi, Fina, 3], [Medi, Fina, 4], [Medi, Fina, 6] },

        // State 4: prev was FINA ALAPH,  not willing to join.
        { [None, None, 0], [None, Isol, 2], [Med2, Isol, 1], [Med2, Isol, 2], [Med2, Fin2, 5], [Med2, Isol, 6] },

        // State 5: prev was FIN2/FIN3 ALAPH,  not willing to join.
        { [None, None, 0], [None, Isol, 2], [Isol, Isol, 1], [Isol, Isol, 2], [Isol, Fin2, 5], [Isol, Isol, 6] },

        // State 6: prev was DALATH/RISH,  not willing to join.
        { [None, None, 0], [None, Isol, 2], [None, Isol, 1], [None, Isol, 2], [None, Fin3, 5], [None, Isol, 6] },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ArabicShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics used to resolve feature availability.</param>
    /// <param name="languageTags">The language system candidates used to resolve features.</param>
    public ArabicShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics, Tag[] languageTags)
        : base(script, MarkZeroingMode.PostGpos, textOptions)
    {
        this.pauseAction = Pause;
        this.recordStretchPartsAction = this.RecordStretchParts;
        this.fallbackAction = script == ScriptClass.Arabic ? this.ApplyFallback : null;

        // Arabic fonts without required contextual alternates finish 'calt'
        // before common ligatures. When 'rclt' exists, both features belong to
        // the same substitution stage. Feature availability is resolved for the
        // plan's selected script and language in either layout table.
        bool hasRequiredContextualAlternates = false;
        if (fontMetrics.TryGetGSubTable(out GSubTable? gsub))
        {
            this.hasStretchFeature = gsub.TryGetFeatureLookups(fontMetrics, in StchTag, script, languageTags, out _);
            hasRequiredContextualAlternates = gsub.TryGetFeatureLookups(fontMetrics, in RcltTag, script, languageTags, out _);
        }

        if (!hasRequiredContextualAlternates && fontMetrics.TryGetGPosTable(out GPosTable? gpos))
        {
            hasRequiredContextualAlternates = gpos.TryGetFeatureLookups(fontMetrics, in RcltTag, script, languageTags, out _);
        }

        this.pauseAfterContextualAlternates = script == ScriptClass.Arabic && !hasRequiredContextualAlternates;
    }

    /// <inheritdoc/>
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
        // Stretch decomposition must be recorded immediately after its substitution
        // stage; later multiple substitutions use the same component metadata.
        this.EnableFeature(buffer, index, count, StchTag, null, this.recordStretchPartsAction);

        // Canonical composition and localized forms complete before the joining-form
        // stages, so their output becomes the input to every contextual form lookup.
        this.EnableFeature(buffer, index, count, CcmpTag, ShapingFeatureFlags.ManualZwj);
        this.EnableFeature(buffer, index, count, LoclTag, ShapingFeatureFlags.ManualZwj, null, this.pauseAction);

        this.AddFeature(buffer, index, count, IsolTag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, FinaTag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, Fin2Tag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, Fin3Tag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, MediTag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, Med2Tag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);
        this.AddFeature(buffer, index, count, InitTag, ShapingFeatureFlags.ManualZwj, false, null, this.pauseAction);

        // The ligature trio and the required composition and ligature features
        // match the joiners themselves for this script's shaping model.
        this.EnableFeature(buffer, index, count, RligTag, ShapingFeatureFlags.ManualZwj, null, this.fallbackAction);
        this.EnableFeature(buffer, index, count, CaltTag, ShapingFeatureFlags.ManualZwj, null, this.pauseAfterContextualAlternates ? this.pauseAction : null);
        this.Features.AddFlags(LigaTag, ShapingFeatureFlags.ManualZwj);
        this.Features.AddFlags(CligTag, ShapingFeatureFlags.ManualZwj);
    }

    /// <inheritdoc/>
    protected override void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count)
    {
        base.PlanPostprocessingFeatures(buffer, index, count);

        this.EnableFeature(buffer, index, count, MsetTag, ShapingFeatureFlags.ManualZwj);
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(ShapingBuffer buffer, int index, int count)
    {
        base.AssignFeatures(buffer, index, count);

        ArabicJoining.Apply(buffer, index, count, this.ScriptClass, this.Features);
    }

    /// <inheritdoc/>
    public override void ReorderNormalizedMarks(ShapingBuffer buffer, int start, int end)
    {
        // Only the ordinary below and above classes receive the Arabic modifier
        // treatment. Canonical ordering has already grouped equal classes stably.
        int scan = start;
        for (int order = BelowMarkOrder; order <= AboveMarkOrder; order += AboveMarkOrder - BelowMarkOrder)
        {
            while (scan < end && buffer[scan].MarkOrderingClass < order)
            {
                scan++;
            }

            if (scan == end)
            {
                break;
            }

            if (buffer[scan].MarkOrderingClass > order)
            {
                continue;
            }

            int modifierEnd = scan;
            while (modifierEnd < end
                && buffer[modifierEnd].MarkOrderingClass == order
                && UnicodeData.IsArabicModifierCombiningMark((uint)buffer[modifierEnd].CodePoint.Value))
            {
                modifierEnd++;
            }

            if (modifierEnd == scan)
            {
                // The modifier must lead its equal-class block. A later modifier is
                // ordinary input here and canonical order remains unchanged.
                continue;
            }

            // The leading modifier block at this class belongs before every earlier
            // mark in the run. Moving each record in turn preserves both blocks'
            // internal order without allocating temporary storage.
            int movedCount = modifierEnd - scan;
            buffer.CombineInputStarts(start, modifierEnd);
            for (int i = 0; i < movedCount; i++)
            {
                buffer.MoveGlyph(scan + i, start + i);
            }

            // The moved marks must still sort before the ordinary Arabic classes
            // during joiner handling and composition. Fallback positioning later
            // folds these two temporary orders back to below and above geometry.
            int reorderedOrder = order == BelowMarkOrder ? ReorderedBelowMarkOrder : ReorderedAboveMarkOrder;
            int reorderedEnd = start + movedCount;
            while (start < reorderedEnd)
            {
                buffer[start].MarkOrderOverride = reorderedOrder;
                start++;
            }

            scan = modifierEnd;
        }
    }

    /// <inheritdoc/>
    public override void PostprocessGlyphs(ShapingBuffer buffer, int index, int count)
    {
        int segmentEnd = index + count;
        bool hasStretch = false;

        // Most runs do not use stretch decomposition. Avoid reversing or scanning
        // their positioned records a second time.
        for (int i = index; i < segmentEnd; i++)
        {
            hasStretch |= buffer[i].IsFixedStretch || buffer[i].IsRepeatingStretch;
        }

        if (!hasStretch)
        {
            return;
        }

        bool rightToLeft = buffer[index].Direction == TextDirection.RightToLeft;

        // Stretch geometry is computed in displayed order. The library keeps its
        // shared shaping result in logical order, so reverse only this segment for
        // the calculation and restore logical order when expansion is complete.
        buffer.ReverseRange(index, segmentEnd);

        int addedGlyphs = 0;
        int scan = segmentEnd;
        while (scan > index)
        {
            scan--;
            if (!IsStretch(buffer[scan]))
            {
                continue;
            }

            // One decomposition is a contiguous alternating run of fixed and
            // repeating tiles. Its original advances determine how much of the
            // covered word can be filled without copying.
            int stretchEnd = scan + 1;
            int fixedWidth = 0;
            int repeatingWidth = 0;
            int fixedCount = 0;
            int repeatingCount = 0;
            while (scan >= index && IsStretch(buffer[scan]))
            {
                int width = buffer.MetricsAt(scan).Metrics.AdvanceWidth;
                if (buffer[scan].IsFixedStretch)
                {
                    fixedWidth += width;
                    fixedCount++;
                }
                else
                {
                    repeatingWidth += width;
                    repeatingCount++;
                }

                scan--;
            }

            // The stretch covers the adjacent word preceding the tile run in
            // displayed order. Punctuation and separators terminate the context;
            // positioning adjustments are included in the available width.
            int stretchStart = scan + 1;
            int context = stretchStart;
            int availableWidth = 0;
            while (context > index && !IsStretch(buffer[context - 1]) && IsStretchContext(buffer[context - 1]))
            {
                context--;
                ref ShapingBuffer.GlyphMetricsEntry contextMetrics = ref buffer.MetricsAt(context);
                ref GlyphShapingPosition contextPosition = ref buffer.PositionAt(context);
                availableWidth += contextMetrics.GetAdvanceWidth(in contextPosition);
            }

            // Fixed tiles are emitted once. Fill the remaining width with complete
            // copies of the repeating pattern, leaving any residual width to center
            // the result over the covered word.
            int remainingWidth = availableWidth - fixedWidth;
            int repeatCopies = 0;
            if (remainingWidth > repeatingWidth && repeatingWidth > 0)
            {
                repeatCopies = (remainingWidth / repeatingWidth) - 1;
            }

            // When another complete pattern would overrun the target, permit the
            // repeated tiles to overlap evenly. This produces a closer fit than
            // leaving an uncovered shortfall.
            int repeatOverlap = 0;
            int shortfall = remainingWidth - (repeatingWidth * (repeatCopies + 1));
            if (shortfall > 0 && repeatingCount > 0)
            {
                repeatCopies++;
                int excess = ((repeatCopies + 1) * repeatingWidth) - remainingWidth;
                if (excess > 0)
                {
                    repeatOverlap = excess / (repeatCopies * repeatingCount);
                    remainingWidth = 0;
                }
            }

            // Malformed fonts must not expand one decomposition without bound. The
            // limit includes both original tiles and every inserted repeat.
            int baseGlyphs = fixedCount + repeatingCount;
            int maximumCopies = repeatingCount > 0 && baseGlyphs < MaximumStretchGlyphs
                ? (MaximumStretchGlyphs - baseGlyphs) / repeatingCount
                : 0;
            repeatCopies = Math.Min(repeatCopies, maximumCopies);

            // Half of any uncovered width is left on each side. Every tile becomes
            // zero-advance and is placed explicitly within that centered span.
            int xOffset = remainingWidth / 2;
            for (int tile = stretchEnd - 1; tile >= stretchStart; tile--)
            {
                ref GlyphShapingData tileData = ref buffer[tile];
                ShapingBuffer.GlyphMetricsEntry tileMetrics = buffer.MetricsAt(tile);
                GlyphShapingPosition tilePosition = buffer.PositionAt(tile);
                int width = tileMetrics.Metrics.AdvanceWidth;
                int repetitions = tileData.IsRepeatingStretch ? repeatCopies + 1 : 1;

                // Copies are inserted at the same index. Each later insertion
                // therefore lands before the preceding copy, matching the reverse
                // write order used to preserve the tile sequence.
                for (int repetition = 0; repetition < repetitions; repetition++)
                {
                    if (rightToLeft)
                    {
                        xOffset -= width;
                        if (repetition > 0)
                        {
                            xOffset += repeatOverlap;
                        }
                    }

                    tilePosition.Bounds.X = xOffset;
                    tilePosition.Bounds.Width = 0;
                    if (repetition == 0)
                    {
                        // Reuse the original record for the first copy so its text
                        // identity and resolved metrics remain authoritative.
                        buffer.PositionAt(tile) = tilePosition;
                    }
                    else
                    {
                        buffer.InsertPositioned(tile, in tileData, in tileMetrics, in tilePosition);
                        addedGlyphs++;
                    }

                    if (!rightToLeft)
                    {
                        xOffset += width;
                        if (repetition > 0)
                        {
                            xOffset -= repeatOverlap;
                        }
                    }
                }
            }
        }

        buffer.ReverseRange(index, segmentEnd + addedGlyphs);
    }

    /// <summary>
    /// Records the alternating fixed and repeating pieces emitted by the stretch
    /// feature. Only substitution creates these pieces; the same stage action is
    /// also visible to positioning because both tables share stage boundaries.
    /// </summary>
    /// <param name="plan">The shaping plan.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    private void RecordStretchParts(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (!this.hasStretchFeature || buffer.Role != ShapingBufferRole.Substitution)
        {
            return;
        }

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            ref GlyphShapingData data = ref buffer[i];
            if (!data.IsDecomposed)
            {
                continue;
            }

            // Multiple substitution numbers its output components from zero. Even
            // components are fixed end pieces; odd components form the repeatable
            // interior pattern.
            bool repeating = (data.LigatureComponent & 1) != 0;
            data.IsFixedStretch = !repeating;
            data.IsRepeatingStretch = repeating;
        }
    }

    /// <summary>
    /// Determines whether a record is one of the tiles produced by stretch
    /// decomposition.
    /// </summary>
    /// <param name="data">The shaping record.</param>
    /// <returns><see langword="true"/> for a fixed or repeating tile.</returns>
    private static bool IsStretch(GlyphShapingData data) => data.IsFixedStretch || data.IsRepeatingStretch;

    /// <summary>
    /// Determines whether a record contributes advance to the word covered by a
    /// stretch decomposition.
    /// </summary>
    /// <param name="data">The shaping record.</param>
    /// <returns><see langword="true"/> when the record belongs to the covered word.</returns>
    private static bool IsStretchContext(GlyphShapingData data)
    {
        if (data.IsDefaultIgnorable)
        {
            return true;
        }

        // This deliberately matches the word categories used by stretch
        // justification. In particular, Arabic and Syriac letters are OtherLetter;
        // broadening this to every Unicode letter category would cross context that
        // the feature does not cover.
        UnicodeCategory category = CodePoint.GetGeneralCategory(data.CodePoint);
        return category is UnicodeCategory.OtherNotAssigned
            or UnicodeCategory.PrivateUse
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark
            or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.OtherNumber
            or UnicodeCategory.CurrencySymbol
            or UnicodeCategory.ModifierSymbol
            or UnicodeCategory.MathSymbol
            or UnicodeCategory.OtherSymbol;
    }

    /// <summary>
    /// Separates joining-form features into distinct substitution stages.
    /// </summary>
    /// <param name="plan">The shaping plan.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    private static void Pause(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
    }

    /// <summary>
    /// Applies presentation-form fallback when all four Arabic joining-form features are absent.
    /// </summary>
    /// <param name="plan">The shaping plan.</param>
    /// <param name="buffer">The shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records in the segment.</param>
    private void ApplyFallback(ShapePlan plan, ShapingBuffer buffer, int index, int count)
    {
        if (this.ScriptClass != ScriptClass.Arabic
            || plan.TryGetGSubFeatureLookups(in IsolTag, out _)
            || plan.TryGetGSubFeatureLookups(in FinaTag, out _)
            || plan.TryGetGSubFeatureLookups(in MediTag, out _)
            || plan.TryGetGSubFeatureLookups(in InitTag, out _))
        {
            return;
        }

        this.fallbackSubstitutions ??= ArabicFallbackSubstitutions.Create(plan.FontMetrics);
        this.fallbackSubstitutions.Apply(plan, buffer, index, count, InitTag, MediTag, FinaTag, IsolTag, RligTag);
    }
}
