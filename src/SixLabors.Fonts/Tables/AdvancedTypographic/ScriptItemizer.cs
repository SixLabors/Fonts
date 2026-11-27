// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Determines whether a source character belongs to the current shaping run.
/// Creates or reuses a plan for each font span and prepares its mapped glyphs.
/// Text preparation also applies to spans whose font has no substitution table.
/// </summary>
internal static class ScriptItemizer
{
    /// <summary>
    /// Prepares a span whose font has no substitution table.
    /// </summary>
    /// <param name="fontMetrics">The font metrics.</param>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="run">The resolved source run.</param>
    /// <param name="index">The first glyph of the span.</param>
    /// <returns>The shape plan for positioning the span.</returns>
    public static ShapePlan PlanRun(FontMetrics fontMetrics, ShapingBuffer buffer, ShapingRun run, int index)
    {
        ShapePlan shapePlan = buffer.GetOrCreatePlan(run.Script, default, fontMetrics, run.Culture, run.FeatureTags, false);
        shapePlan.Shaper.Plan(fontMetrics, buffer, index, buffer.SpanEnd - index, run.IsVertical);

        // The stage actions can change the span length even without lookups.
        List<ShapePlanStageGroup<GSub.LookupTable>> groups = shapePlan.GetOrBuildGSubStageGroups();
        List<ShapingStage> stages = shapePlan.Stages;
        for (int i = 0; i < groups.Count; i++)
        {
            ShapePlanStageGroup<GSub.LookupTable> group = groups[i];
            stages[group.Start].PreProcessFeature(shapePlan, buffer, index, buffer.SpanEnd - index);
            stages[group.End - 1].PostProcessFeature(shapePlan, buffer, index, buffer.SpanEnd - index);
        }

        int count = buffer.SpanEnd - index;
        if (count > 0)
        {
            MarkCursiveTrackingRun(buffer, index, count, run.Script);
        }

        return shapePlan;
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
        // Store the resolved classification on each shaped glyph so layout
        // can use it without resolving the script again.
        int end = index + count;
        for (int i = index; i < end; i++)
        {
            buffer[i].IsCursiveScript = true;
        }
    }

    /// <summary>
    /// Tracks the script, language, features, and orientation of a shaping run.
    /// </summary>
    public struct ShapingRun
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShapingRun"/> struct from the first source character's properties.
        /// </summary>
        /// <param name="script">The character's effective script.</param>
        /// <param name="culture">The effective language.</param>
        /// <param name="featureTags">The effective feature tags.</param>
        /// <param name="isVertical">Whether to shape on the vertical axis.</param>
        public ShapingRun(ScriptClass script, CultureInfo culture, IReadOnlyList<Tag> featureTags, bool isVertical)
        {
            this.Script = script;
            this.Culture = culture;
            this.FeatureTags = featureTags;
            this.IsVertical = isVertical;
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
        /// Gets a value indicating whether the run uses vertical shaping.
        /// </summary>
        public bool IsVertical { get; }

        /// <summary>
        /// Checks the language and features when the text enters a new declaration.
        /// </summary>
        /// <param name="culture">The effective language.</param>
        /// <param name="featureTags">The effective feature tags.</param>
        /// <returns>Whether the declaration can use the run's language and features.</returns>
        public readonly bool HasSameSettings(CultureInfo culture, IReadOnlyList<Tag> featureTags)
            => (ReferenceEquals(culture, this.Culture) || string.Equals(culture.Name, this.Culture.Name, StringComparison.Ordinal))
                && FeatureTagsEqual(this.FeatureTags, featureTags);

        /// <summary>
        /// Attempts to extend the run through the next source character.
        /// </summary>
        /// <param name="codePoint">The next source character.</param>
        /// <param name="script">The character's effective script.</param>
        /// <param name="isVertical">Whether to shape the character on the vertical axis.</param>
        /// <returns>Whether the character belongs to this run.</returns>
        public bool TryInclude(CodePoint codePoint, ScriptClass script, bool isVertical)
        {
            if (isVertical != this.IsVertical)
            {
                return false;
            }

            // Common and inherited characters can join the script around them.
            // Script_Extensions also permits characters shared by several scripts.
            if (this.Script is ScriptClass.Common or ScriptClass.Unknown or ScriptClass.Inherited)
            {
                this.Script = script;
            }
            else if (script != this.Script
                && script is not (ScriptClass.Common or ScriptClass.Unknown or ScriptClass.Inherited)
                && !ScriptExtensionData.Contains(codePoint, this.Script))
            {
                return false;
            }

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
