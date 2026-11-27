// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Splits a shaping buffer into runs of a single script and language and plans a
/// shaper for each run.
/// <para>
/// Planning is what prepares a run's text and registers its features, so it
/// belongs to every font rather than to the fonts that carry a substitution
/// table. A font without one still needs the characters a script cannot be read
/// without, and positioning reuses the runs planning records.
/// </para>
/// </summary>
internal static class ScriptItemizer
{
    /// <summary>
    /// Reads the run of one script that begins at <paramref name="index"/>,
    /// leaving <paramref name="index"/> on the run's last record.
    /// <para>
    /// Feature lookups are assigned to runs rather than to the text as a whole so
    /// that the shapers of two languages cannot interfere with one another.
    /// Characters belonging to no script of their own join the run around them.
    /// </para>
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the run's first record, left on its last.</param>
    /// <param name="maxCount">The largest record count a run may reach.</param>
    /// <param name="count">When this method returns, contains the number of records in the run.</param>
    /// <returns>The script and culture shared by the run.</returns>
    public static ShapingRun ReadRun(ShapingBuffer buffer, ref int index, int maxCount, out int count)
    {
        ShapingRun run = new(buffer, index);
        count = 1;

        while (index < buffer.Count - 1)
        {
            if (!run.TryInclude(buffer, index + 1))
            {
                break;
            }

            index++;
            count++;

            if (index >= maxCount)
            {
                break;
            }
        }

        return run;
    }

    /// <summary>
    /// Gets whether shaping uses the vertical axis for this character.
    /// </summary>
    /// <param name="codePoint">The character to shape.</param>
    /// <param name="layoutMode">The layout mode for the text.</param>
    /// <returns>Whether to apply vertical shaping features.</returns>
    public static bool UsesVerticalShaping(CodePoint codePoint, LayoutMode layoutMode)
    {
        // Characters with orientation Tr also need vertical shaping so the
        // font can supply a vertical form. Layout rotates the glyph only if
        // shaping does not substitute that form.
        return layoutMode.IsVertical()
            || (layoutMode.IsVerticalMixed() && CodePoint.GetVerticalOrientationType(codePoint) != VerticalOrientationType.Rotate);
    }

    /// <summary>
    /// Resolves the script that applies to one shaping record.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The explicit run script, the whole-text script, or the script inferred from the character, in that order.</returns>
    public static ScriptClass ResolveScript(ShapingBuffer buffer, int index)
    {
        ref GlyphShapingData data = ref buffer[index];
        ScriptClass? script = buffer.TextRuns[data.TextRunIndex].Script ?? buffer.TextOptions.Script;

        // Explicit metadata belongs to the whole declared run, including its
        // punctuation and any characters whose Unicode Script value differs.
        return script ?? CodePoint.GetScriptClass(data.CodePoint);
    }

    /// <summary>
    /// Resolves the culture that applies to one shaping record.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The explicit run culture, the whole-text culture, or the current culture, in that order.</returns>
    public static CultureInfo ResolveCulture(ShapingBuffer buffer, int index)
    {
        ref GlyphShapingData data = ref buffer[index];
        return buffer.TextRuns[data.TextRunIndex].Culture ?? buffer.TextOptions.Culture ?? CultureInfo.CurrentCulture;
    }

    /// <summary>
    /// Resolves the additional feature tags that apply to one shaping record.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The run-specific tags, or the whole-text tags when the run inherits them.</returns>
    public static IReadOnlyList<Tag> ResolveFeatureTags(ShapingBuffer buffer, int index)
    {
        ref GlyphShapingData data = ref buffer[index];

        // Null means inheritance; an explicitly empty run list must remain empty so
        // it can disable whole-text features over only that run.
        return buffer.TextRuns[data.TextRunIndex].FeatureTags ?? buffer.TextOptions.FeatureTags;
    }

    /// <summary>
    /// Plans a shaper over every run of the buffer and records the runs for the
    /// positioning pass. Used for a font that carries no substitution table,
    /// where there are no lookups to apply but the text still has to be prepared.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    public static void PlanRuns(FontMetrics fontMetrics, ShapingBuffer buffer)
    {
        // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
        int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(buffer.Count);

        for (int i = 0; i < buffer.Count; i++)
        {
            int index = i;
            ShapingRun run = ReadRun(buffer, ref i, maxCount, out int count);

            // With no substitution table the font offers no script of its own, so
            // the run is planned against the default design.
            ShapePlan shapePlan = buffer.GetOrCreatePlan(run.Script, default, fontMetrics, run.Culture, run.FeatureTags);

            // Preparing the text can insert records, so the run grows with it.
            int collectionCount = buffer.Count;

            shapePlan.Shaper.Plan(fontMetrics, buffer, index, count);

            int delta = buffer.Count - collectionCount;
            i += delta;
            count += delta;

            // A substitution table with no lookups still walks every stage and
            // invokes its actions. Preserve that path for fonts with no table,
            // accounting for actions that insert or remove records.
            List<ShapePlanStageGroup<GSub.LookupTable>> groups = shapePlan.GetOrBuildGSubStageGroups();
            List<ShapingStage> stages = shapePlan.Stages;
            for (int g = 0; g < groups.Count; g++)
            {
                ShapePlanStageGroup<GSub.LookupTable> group = groups[g];

                collectionCount = buffer.Count;
                stages[group.Start].PreProcessFeature(shapePlan, buffer, index, count);

                delta = buffer.Count - collectionCount;
                i += delta;
                count += delta;

                collectionCount = buffer.Count;
                stages[group.End - 1].PostProcessFeature(shapePlan, buffer, index, count);

                delta = buffer.Count - collectionCount;
                i += delta;
                count += delta;
            }

            MarkCursiveTrackingRun(buffer, index, count, run.Script);
            buffer.SegmentPlans.Add((index, count, run.Script, shapePlan));
        }
    }

    /// <summary>
    /// Marks a resolved cursive-script run so layout can preserve its joins when
    /// applying tracking. The pass is skipped entirely when tracking is disabled.
    /// </summary>
    /// <param name="buffer">The shaped glyph buffer.</param>
    /// <param name="index">The zero-based index of the first glyph in the run.</param>
    /// <param name="count">The number of glyphs in the run.</param>
    /// <param name="script">The resolved script shared by the run.</param>
    public static void MarkCursiveTrackingRun(ShapingBuffer buffer, int index, int count, ScriptClass script)
    {
        // Layout only needs this metadata when it will add tracking. Avoid touching
        // every output record in the common zero-tracking case.
        if (buffer.TextOptions.Tracking == 0
            || script is not (ScriptClass.Arabic
                or ScriptClass.HanifiRohingya
                or ScriptClass.Mandaic
                or ScriptClass.Mongolian
                or ScriptClass.Nko
                or ScriptClass.PhagsPa
                or ScriptClass.Syriac))
        {
            return;
        }

        // Browsers implement CSS Text §8.2.1 by suppressing letter spacing inside
        // cursively joined runs:
        // https://www.w3.org/TR/css-text-4/#cursive-tracking
        // Store the resolved classification on each post-GSUB record so layout
        // does not re-itemize the shaped glyph stream.
        int end = index + count;
        for (int i = index; i < end; i++)
        {
            buffer[i].IsCursiveScript = true;
        }
    }

    /// <summary>
    /// Tracks the script and language shared by one shaping run.
    /// </summary>
    public struct ShapingRun
    {
        private int textRunIndex;
        private readonly TextDirection direction;
        private readonly bool isVertical;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShapingRun"/> struct from the first shaping record in a run.
        /// </summary>
        /// <param name="buffer">The glyph shaping buffer.</param>
        /// <param name="index">The zero-based index of the first record.</param>
        public ShapingRun(ShapingBuffer buffer, int index)
        {
            this.Script = ResolveScript(buffer, index);
            this.Culture = ResolveCulture(buffer, index);
            this.FeatureTags = ResolveFeatureTags(buffer, index);
            ref GlyphShapingData first = ref buffer[index];
            this.textRunIndex = first.TextRunIndex;
            this.direction = first.Direction;
            this.isVertical = UsesVerticalShaping(first.CodePoint, buffer.TextOptions.LayoutMode);
        }

        /// <summary>
        /// Gets the script resolved for the run.
        /// </summary>
        public ScriptClass Script { get; private set; }

        /// <summary>
        /// Gets the culture resolved for the run.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets the additional feature tags resolved for the run.
        /// </summary>
        public IReadOnlyList<Tag> FeatureTags { get; }

        /// <summary>
        /// Attempts to extend the run through one adjacent shaping record.
        /// </summary>
        /// <param name="buffer">The glyph shaping buffer.</param>
        /// <param name="index">The zero-based index immediately after the last record in the run.</param>
        /// <returns><see langword="true"/> when the record belongs to this run.</returns>
        public bool TryInclude(ShapingBuffer buffer, int index)
        {
            ref GlyphShapingData data = ref buffer[index];
            CodePoint codePoint = data.CodePoint;
            ScriptClass next = ResolveScript(buffer, index);
            int nextTextRunIndex = data.TextRunIndex;
            if (data.Direction != this.direction)
            {
                // Browsers resolve bidi runs before shaping and require the
                // direction to match before adjacent segments can share one
                // shaping call.
                // Splitting here confines GSUB, GPOS, and cursive attachment to
                // the same directional boundary without another glyph pass.
                return false;
            }

            LayoutMode layoutMode = buffer.TextOptions.LayoutMode;
            if (layoutMode.IsVerticalMixed()
                && data.GraphemeIndex != buffer[index - 1].GraphemeIndex
                && UsesVerticalShaping(codePoint, layoutMode) != this.isVertical)
            {
                // Sideways text uses horizontal features such as kern. Split at
                // the grapheme boundary to keep those features and keep marks with their base.
                return false;
            }

            if (nextTextRunIndex != this.textRunIndex)
            {
                // Styling-only TextRun boundaries must not fragment contextual
                // shaping. Split only when a value consumed by the shaper changes.

                // Language-system features differ within the same script, so a
                // culture change is a shaping boundary even when the script matches.
                CultureInfo nextCulture = ResolveCulture(buffer, index);
                if (!ReferenceEquals(nextCulture, this.Culture) && !string.Equals(nextCulture.Name, this.Culture.Name, StringComparison.Ordinal))
                {
                    return false;
                }

                IReadOnlyList<Tag> nextFeatureTags = ResolveFeatureTags(buffer, index);
                if (!FeatureTagsEqual(this.FeatureTags, nextFeatureTags))
                {
                    return false;
                }
            }

            if (next != this.Script &&
                this.Script is not ScriptClass.Common and not ScriptClass.Unknown and not ScriptClass.Inherited &&
                next is not ScriptClass.Common and not ScriptClass.Unknown and not ScriptClass.Inherited &&
                !ScriptExtensionData.Contains(codePoint, this.Script))
            {
                return false;
            }

            if (this.Script is ScriptClass.Common or ScriptClass.Unknown or ScriptClass.Inherited)
            {
                this.Script = next;
            }

            this.textRunIndex = nextTextRunIndex;
            return true;
        }

        /// <summary>
        /// Compares effective feature lists without turning equivalent run
        /// declarations into shaping boundaries.
        /// </summary>
        /// <param name="left">The first feature list.</param>
        /// <param name="right">The second feature list.</param>
        /// <returns><see langword="true"/> when both lists contain the same ordered tags.</returns>
        private static bool FeatureTagsEqual(IReadOnlyList<Tag> left, IReadOnlyList<Tag> right)
        {
            // Inherited features normally resolve to the same collection, making
            // the usual path one reference comparison.
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            // Feature lists are normally only a few tags long. A scalar comparison
            // avoids allocating contiguous storage for an interface-backed list.
            if (left.Count != right.Count)
            {
                return false;
            }

            // Order matters because later user declarations are planned after
            // defaults and can override whether a feature is enabled.
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
