// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Chooses the form each character of a cursive script takes from the characters
/// around it. Several scripts join this way, and the shapers that handle them all
/// settle the forms here.
/// </summary>
internal static class ArabicJoining
{
    /// <summary>
    /// No form is chosen.
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
    /// The 'isol' (isolated forms) feature tag.
    /// </summary>
    public static readonly Tag IsolTag = Tag.Parse("isol");

    /// <summary>
    /// The 'fina' (terminal forms) feature tag.
    /// </summary>
    public static readonly Tag FinaTag = Tag.Parse("fina");

    /// <summary>
    /// The 'fin2' (terminal forms #2) feature tag.
    /// </summary>
    public static readonly Tag Fin2Tag = Tag.Parse("fin2");

    /// <summary>
    /// The 'fin3' (terminal forms #3) feature tag.
    /// </summary>
    public static readonly Tag Fin3Tag = Tag.Parse("fin3");

    /// <summary>
    /// The 'medi' (medial forms) feature tag.
    /// </summary>
    public static readonly Tag MediTag = Tag.Parse("medi");

    /// <summary>
    /// The 'med2' (medial forms #2) feature tag.
    /// </summary>
    public static readonly Tag Med2Tag = Tag.Parse("med2");

    /// <summary>
    /// The 'init' (initial forms) feature tag.
    /// </summary>
    public static readonly Tag InitTag = Tag.Parse("init");

    /// <summary>
    /// The joining state machine table. Each entry is [prevAction, curAction, nextState].
    /// Rows are states (0-6), columns are joining categories.
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
    /// Determines whether the script's characters take their form from the ones
    /// around them. Only these scripts carry the data the state machine reads.
    /// </summary>
    /// <param name="script">The script the text is written in.</param>
    /// <returns><see langword="true"/> when the script joins.</returns>
    public static bool Joins(ScriptClass script)
        => script switch
        {
            ScriptClass.Arabic
            or ScriptClass.Mongolian
            or ScriptClass.Syriac
            or ScriptClass.Nko
            or ScriptClass.PhagsPa
            or ScriptClass.Mandaic
            or ScriptClass.Manichaean
            or ScriptClass.PsalterPahlavi
            or ScriptClass.Adlam
            or ScriptClass.HanifiRohingya
            or ScriptClass.Chorasmian
            or ScriptClass.Sogdian
            or ScriptClass.OldUyghur => true,
            _ => false,
        };

    /// <summary>
    /// Walks the text and turns on, for each character, the feature naming the form
    /// it takes among its neighbours.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records.</param>
    /// <param name="script">The script the text is written in.</param>
    /// <param name="features">The features holding the masks to turn on.</param>
    public static void Apply(ShapingBuffer buffer, int index, int count, ScriptClass script, ShapePlanFeatures features)
    {
        int prev = -1;
        int state = 0;
        byte[] actions = buffer.GetShaperScratch(count);

        for (int i = 0; i < count; i++)
        {
            ref GlyphShapingData data = ref buffer[i + index];
            ArabicJoiningClass joiningClass = CodePoint.GetArabicJoiningClass(data.CodePoint);
            if (joiningClass.JoiningType == ArabicJoiningType.Transparent)
            {
                actions[i] = None;
                continue;
            }

            byte[] actionsWithState = StateTable[state, GetShapingClassIndex(joiningClass)];
            byte prevAction = actionsWithState[0];
            byte curAction = actionsWithState[1];
            state = actionsWithState[2];

            if (prevAction != None && prev != -1)
            {
                actions[prev] = prevAction;
            }

            actions[i] = curAction;
            prev = i;
        }

        if (script == ScriptClass.Mongolian)
        {
            CarryFormsToVariationSelectors(buffer, index, count, actions);
        }

        // Form selection uses the same small feature set for the complete run.
        // Resolve the masks once because lookup in the plan's feature lists is
        // otherwise repeated for every non-transparent character.
        uint finaMask = features.GetMask(FinaTag);
        uint fin2Mask = features.GetMask(Fin2Tag);
        uint fin3Mask = features.GetMask(Fin3Tag);
        uint isolMask = features.GetMask(IsolTag);
        uint initMask = features.GetMask(InitTag);
        uint mediMask = features.GetMask(MediTag);
        uint med2Mask = features.GetMask(Med2Tag);

        for (int i = 0; i < count; i++)
        {
            switch (actions[i])
            {
                case Fina:
                    buffer.EnableShapingFeature(i + index, finaMask);
                    break;
                case Fin2:
                    buffer.EnableShapingFeature(i + index, fin2Mask);
                    break;
                case Fin3:
                    buffer.EnableShapingFeature(i + index, fin3Mask);
                    break;
                case Isol:
                    buffer.EnableShapingFeature(i + index, isolMask);
                    break;
                case Init:
                    buffer.EnableShapingFeature(i + index, initMask);
                    break;
                case Medi:
                    buffer.EnableShapingFeature(i + index, mediMask);
                    break;
                case Med2:
                    buffer.EnableShapingFeature(i + index, med2Mask);
                    break;
            }
        }
    }

    /// <summary>
    /// Gives each free variation selector the form of the character it follows. A
    /// selector chooses between shapes of that character, so it has to be drawn in
    /// the same form as the character it qualifies.
    /// </summary>
    /// <param name="buffer">The glyph shaping buffer.</param>
    /// <param name="index">The zero-based index of the first record.</param>
    /// <param name="count">The number of records.</param>
    /// <param name="actions">The form chosen for each record.</param>
    private static void CarryFormsToVariationSelectors(ShapingBuffer buffer, int index, int count, byte[] actions)
    {
        // U+180B..U+180D FREE VARIATION SELECTOR ONE..THREE and
        // U+180F FREE VARIATION SELECTOR FOUR.
        const int FirstFreeVariationSelector = 0x180B;
        const int LastFreeVariationSelector = 0x180D;
        const int FourthFreeVariationSelector = 0x180F;

        for (int i = 1; i < count; i++)
        {
            int value = buffer[i + index].CodePoint.Value;
            if ((value >= FirstFreeVariationSelector && value <= LastFreeVariationSelector)
                || value == FourthFreeVariationSelector)
            {
                actions[i] = actions[i - 1];
            }
        }
    }

    /// <summary>
    /// Maps the joining properties of a character to the column of the state table
    /// that describes it.
    /// </summary>
    /// <param name="joiningClass">The joining properties of the character.</param>
    /// <returns>The state table column index.</returns>
    private static int GetShapingClassIndex(ArabicJoiningClass joiningClass)
    {
        // Two joining groups have rules of their own, and the columns that carry
        // them stand in place of the ones the joining type would choose.
        if (joiningClass.JoiningGroup == ArabicJoiningGroup.Alaph)
        {
            return 4;
        }

        if (joiningClass.JoiningGroup == ArabicJoiningGroup.DalathRish)
        {
            return 5;
        }

        return joiningClass.JoiningType switch
        {
            ArabicJoiningType.NonJoining => 0,
            ArabicJoiningType.LeftJoining => 1,
            ArabicJoiningType.RightJoining => 2,
            ArabicJoiningType.DualJoining or ArabicJoiningType.JoinCausing => 3,
            ArabicJoiningType.Transparent => 6,
            _ => 0,
        };
    }
}
