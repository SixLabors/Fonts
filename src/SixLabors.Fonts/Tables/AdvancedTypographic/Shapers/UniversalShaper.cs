// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Globalization;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Dfa;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// This shaper is an implementation of the Universal Shaping Engine, which
    /// uses Unicode data to shape a number of scripts without a dedicated shaping engine.
    /// <see href="https://www.microsoft.com/typography/OpenTypeDev/USE/intro.htm"/>.
    /// </summary>
    internal class UniversalShaper : DefaultShaper
    {
        // private static readonly StateMachine StateMachine = new(null!, null!, null!);

        public UniversalShaper(TextOptions textOptions)
            : base(MarkZeroingMode.PreGPos, textOptions)
        {
        }

        // TODO: Implement. I'm stuck on the state table generation.
        public override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            base.AssignFeatures(collection, index, count);
        }

        //private static void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
        //{
        //    int syllable = 0;
        //    int[] codepoints = new int[collection.Count];
        //    for (int i = 0; i < collection.Count; i++)
        //    {
        //        // TODO: This is wrong. We want the USE category.
        //        Unicode.CodePoint data = collection.GetGlyphShapingData(i).CodePoint;
        //        codepoints[i] = data.Value;
        //    }

        //    foreach (StateMatch item in StateMachine.Match(codepoints))
        //    {
        //        ++syllable;
        //        for (int i = item.StartIndex; i <= item.EndIndex; i++)
        //        {
        //            collection.AddShapingFeature(i, new TagEntry(FeatureTags.Syllable, syllable));
        //        }
        //    }
        //}

        //public static UnicodeCategory GetGeneralCategory(CodePoint codePoint)
        //{
        //     return UnicodeData.Getun(codePoint.value);
        //}
    }
}
