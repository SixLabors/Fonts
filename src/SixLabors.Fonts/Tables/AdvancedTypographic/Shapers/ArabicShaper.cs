// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// This is a shaper for Arabic, and other cursive scripts.
/// The shaping state machine was ported from fontkit.
/// <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/ArabicShaper.js"/>
/// </summary>
internal sealed class ArabicShaper : DefaultShaper
{
    private static readonly Tag MsetTag = Tag.Parse("mset");

    private static readonly Tag FinaTag = Tag.Parse("fina");

    private static readonly Tag Fin2Tag = Tag.Parse("fin2");

    private static readonly Tag Fin3Tag = Tag.Parse("fin3");

    private static readonly Tag IsolTag = Tag.Parse("isol");

    private static readonly Tag InitTag = Tag.Parse("init");

    private static readonly Tag MediTag = Tag.Parse("medi");

    private static readonly Tag Med2Tag = Tag.Parse("med2");

    private const byte None = 0;

    private const byte Isol = 1;

    private const byte Fina = 2;

    private const byte Fin2 = 3;

    private const byte Fin3 = 4;

    private const byte Medi = 5;

    private const byte Med2 = 6;

    private const byte Init = 7;

    // Each entry is [prevAction, curAction, nextState]
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

    public ArabicShaper(ScriptClass script, TextOptions textOptions)
        : base(script, MarkZeroingMode.PostGpos, textOptions)
    {
    }

    /// <inheritdoc/>
    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        this.AddFeature(collection, index, count, CcmpTag);
        this.AddFeature(collection, index, count, LoclTag);

        this.AddFeature(collection, index, count, IsolTag, false);
        this.AddFeature(collection, index, count, FinaTag, false);
        this.AddFeature(collection, index, count, Fin2Tag, false);
        this.AddFeature(collection, index, count, Fin3Tag, false);
        this.AddFeature(collection, index, count, MediTag, false);
        this.AddFeature(collection, index, count, Med2Tag, false);
        this.AddFeature(collection, index, count, InitTag, false);
        this.AddFeature(collection, index, count, MsetTag);
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        base.AssignFeatures(collection, index, count);

        int prev = -1;
        int state = 0;
        byte[] actions = new byte[count];

        // Apply the state machine to map glyphs to features.
        for (int i = 0; i < count; i++)
        {
            GlyphShapingData data = collection[i + index];
            ArabicJoiningClass joiningClass = CodePoint.GetArabicJoiningClass(data.CodePoint);
            ArabicJoiningType joiningType = joiningClass.JoiningType;
            if (joiningType == ArabicJoiningType.Transparent)
            {
                actions[i] = None;
                continue;
            }

            int shapingClassIndex = GetShapingClassIndex(joiningType);
            byte[] actionsWithState = StateTable[state, shapingClassIndex];
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

        // Apply the chosen features to their respective glyphs.
        for (int i = 0; i < actions.Length; i++)
        {
            switch (actions[i])
            {
                case Fina:
                    collection.EnableShapingFeature(i + index, FinaTag);
                    break;
                case Fin2:
                    collection.EnableShapingFeature(i + index, Fin2Tag);
                    break;
                case Fin3:
                    collection.EnableShapingFeature(i + index, Fin3Tag);
                    break;
                case Isol:
                    collection.EnableShapingFeature(i + index, IsolTag);
                    break;
                case Init:
                    collection.EnableShapingFeature(i + index, InitTag);
                    break;
                case Medi:
                    collection.EnableShapingFeature(i + index, MediTag);
                    break;
                case Med2:
                    collection.EnableShapingFeature(i + index, Med2Tag);
                    break;
            }
        }
    }

    private static int GetShapingClassIndex(ArabicJoiningType joiningType) => joiningType switch
    {
        ArabicJoiningType.NonJoining => 0,
        ArabicJoiningType.LeftJoining => 1,
        ArabicJoiningType.RightJoining => 2,
        ArabicJoiningType.DualJoining or ArabicJoiningType.JoinCausing => 3,

        // TODO: ALAPH: 4
        // TODO: DALATH RISH': 5
        ArabicJoiningType.Transparent => 6,
        _ => 0,
    };
}
