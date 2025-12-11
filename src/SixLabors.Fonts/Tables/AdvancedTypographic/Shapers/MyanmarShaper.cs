// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.MyanmarShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

internal sealed class MyanmarShaper : DefaultShaper
{
    private static readonly StateMachine StateMachine =
        new(StateTable, AcceptingStates, Tags);

    // Basic features.
    // These features are applied in order, one at a time, after reordering, constrained to the syllable.
    private static readonly Tag RphfTag = Tag.Parse("rphf");
    private static readonly Tag PrefTag = Tag.Parse("pref");
    private static readonly Tag BlwfTag = Tag.Parse("blwf");
    private static readonly Tag PstfTag = Tag.Parse("pstf");

    // Other features.
    // These features are applied all at once, after clearing syllables.
    private static readonly Tag PresTag = Tag.Parse("pres");
    private static readonly Tag AbvsTag = Tag.Parse("abvs");
    private static readonly Tag BlwsTag = Tag.Parse("blws");
    private static readonly Tag PstsTag = Tag.Parse("psts");

    private readonly FontMetrics fontMetrics;

    public MyanmarShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
       : base(script, MarkZeroingMode.PreGPos, textOptions)
        => this.fontMetrics = fontMetrics;

    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
    }

    private static void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
    {
        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];

        for (int i = index; i < index + count; i++)
        {
            CodePoint codePoint = substitutionCollection[i].CodePoint;
            values[i - index] = IndicShapingCategory(codePoint);
        }
    }

    private static int IndicShapingCategory(CodePoint codePoint)
    {
        // TODO: We need to augment this to use the Myanmar specific data.
        return UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> 8;
    }
}
