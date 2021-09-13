// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// This is a shaper for Arabic, and other cursive scripts.
    /// The shaping state machine was ported from fontkit.
    /// <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/ArabicShaper.js"/>
    /// </summary>
    internal sealed class ArabicShaper : DefaultShaper
    {
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

        /// <inheritdoc/>
        public override void AssignFeatures(GlyphSubstitutionCollection collection, int index, int count)
        {
            base.AssignFeatures(collection, index, count);

            int prev = -1;
            int state = 0;
            byte[] actions = new byte[count];

            // Apply the state machine to map glyphs to features.
            for (int i = 0; i < count; i++)
            {
                collection.GetCodePointAndGlyphIds(i + index, out CodePoint codePoint, out int _, out IEnumerable<int> _);
                JoiningClass joiningClass = CodePoint.GetJoiningClass(codePoint);
                JoiningType joiningType = joiningClass.JoiningType;
                if (joiningType == JoiningType.Transparent)
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

            // TODO: Perf. Tags should be static.
            // Apply the chosen features to their respective glyphs.
            for (int i = 0; i < actions.Length; i++)
            {
                switch (actions[i])
                {
                    case Fina:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("fina"));
                        break;
                    case Fin2:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("fin2"));
                        break;
                    case Fin3:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("fin3"));
                        break;
                    case Isol:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("isol"));
                        break;
                    case Init:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("init"));
                        break;
                    case Medi:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("medi"));
                        break;
                    case Med2:
                        collection.AddSubstitutionFeature(i + index, Tag.Parse("med2"));
                        break;
                }
            }
        }

        private static int GetShapingClassIndex(JoiningType joiningType) => joiningType switch
        {
            JoiningType.NonJoining => 0,
            JoiningType.LeftJoining => 1,
            JoiningType.RightJoining => 2,
            JoiningType.DualJoining or JoiningType.JoinCausing => 3,

            // TODO: ALAPH: 4
            // TODO: DALATH RISH': 5
            JoiningType.Transparent => 6,
            _ => 0,
        };
    }
}
