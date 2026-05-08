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
    /// <summary>The 'mset' (mark positioning via substitution) feature tag.</summary>
    private static readonly Tag MsetTag = Tag.Parse("mset");

    /// <summary>The 'fina' (terminal forms) feature tag.</summary>
    private static readonly Tag FinaTag = Tag.Parse("fina");

    /// <summary>The 'fin2' (terminal forms #2) feature tag.</summary>
    private static readonly Tag Fin2Tag = Tag.Parse("fin2");

    /// <summary>The 'fin3' (terminal forms #3) feature tag.</summary>
    private static readonly Tag Fin3Tag = Tag.Parse("fin3");

    /// <summary>The 'isol' (isolated forms) feature tag.</summary>
    private static readonly Tag IsolTag = Tag.Parse("isol");

    /// <summary>The 'init' (initial forms) feature tag.</summary>
    private static readonly Tag InitTag = Tag.Parse("init");

    /// <summary>The 'medi' (medial forms) feature tag.</summary>
    private static readonly Tag MediTag = Tag.Parse("medi");

    /// <summary>The 'med2' (medial forms #2) feature tag.</summary>
    private static readonly Tag Med2Tag = Tag.Parse("med2");

    /// <summary>No joining action.</summary>
    private const byte None = 0;

    /// <summary>Isolated form action.</summary>
    private const byte Isol = 1;

    /// <summary>Final form action.</summary>
    private const byte Fina = 2;

    /// <summary>Final form #2 action (for ALAPH).</summary>
    private const byte Fin2 = 3;

    /// <summary>Final form #3 action (for ALAPH after DALATH RISH).</summary>
    private const byte Fin3 = 4;

    /// <summary>Medial form action.</summary>
    private const byte Medi = 5;

    /// <summary>Medial form #2 action (for ALAPH).</summary>
    private const byte Med2 = 6;

    /// <summary>Initial form action.</summary>
    private const byte Init = 7;

    /// <summary>
    /// Arabic joining state machine table. Each entry is [prevAction, curAction, nextState].
    /// Rows are states (0-6), columns are joining type categories.
    /// </summary>
    private static readonly byte[,][] StateTable =
    {
        // #           NonJoining,                    LeftJoining,                 RightJoining,                 DualJoining,                    ALAPH,                     DALATH RISH
        // State 0: prev was U,  not willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { None, Isol, 1 }, new byte[] { None, Isol, 2 }, new byte[] { None, Isol, 1 }, new byte[] { None, Isol, 6 } },

        // State 1: prev was R or ISOL/ALAPH,  not willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { None, Isol, 1 }, new byte[] { None, Isol, 2 }, new byte[] { None, Fin2, 5 }, new byte[] { None, Isol, 6 } },

        // State 2: prev was D/L in ISOL form,  willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { Init, Fina, 1 }, new byte[] { Init, Fina, 3 }, new byte[] { Init, Fina, 4 }, new byte[] { Init, Fina, 6 } },

        // State 3: prev was D in FINA form,  willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { Medi, Fina, 1 }, new byte[] { Medi, Fina, 3 }, new byte[] { Medi, Fina, 4 }, new byte[] { Medi, Fina, 6 } },

        // State 4: prev was FINA ALAPH,  not willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { Med2, Isol, 1 }, new byte[] { Med2, Isol, 2 }, new byte[] { Med2, Fin2, 5 }, new byte[] { Med2, Isol, 6 } },

        // State 5: prev was FIN2/FIN3 ALAPH,  not willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { Isol, Isol, 1 }, new byte[] { Isol, Isol, 2 }, new byte[] { Isol, Fin2, 5 }, new byte[] { Isol, Isol, 6 } },

        // State 6: prev was DALATH/RISH,  not willing to join.
        { new byte[] { None, None, 0 }, new byte[] { None, Isol, 2 }, new byte[] { None, Isol, 1 }, new byte[] { None, Isol, 2 }, new byte[] { None, Fin3, 5 }, new byte[] { None, Isol, 6 } },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ArabicShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
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

        // HarfBuzz plans these as Arabic-script features, independently of the
        // generic horizontal feature list. Horizontal runs already get them from
        // DefaultShaper; forced vertical Arabic needs them here as well.
        if (collection.TextOptions.LayoutMode.IsVertical())
        {
            this.AddFeature(collection, index, count, CaltTag);
            this.AddFeature(collection, index, count, LigaTag);
            this.AddFeature(collection, index, count, CligTag);
        }

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

    /// <summary>
    /// Maps an Arabic joining type to the corresponding column index in the state table.
    /// </summary>
    /// <param name="joiningType">The Arabic joining type.</param>
    /// <returns>The state table column index.</returns>
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
