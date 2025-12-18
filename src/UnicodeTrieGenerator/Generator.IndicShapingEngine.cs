// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
using System.Text.RegularExpressions;
using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using Categories = SixLabors.Fonts.Unicode.Resources.IndicShapingData.Categories;
using IPC = SixLabors.Fonts.Unicode.IndicPositionalCategory;
using ISC = SixLabors.Fonts.Unicode.IndicSyllabicCategory;
using Positions = SixLabors.Fonts.Unicode.Resources.IndicShapingData.Positions;

namespace UnicodeTrieGenerator;

/// <content>
/// Contains code to generate a trie and state machine for storing Indic Shaping Data category data.
/// </content>
public static partial class Generator
{
    private static readonly Dictionary<ISC, Categories> CategoryMap = new()
    {
        // { ISC.Other, Categories.X },
        { ISC.Avagraha, Categories.Symbol },
        { ISC.Bindu, Categories.SM },
        { ISC.BrahmiJoiningNumber, Categories.Placeholder },
        { ISC.CantillationMark, Categories.A },
        { ISC.Consonant, Categories.C },
        { ISC.ConsonantDead, Categories.C },
        { ISC.ConsonantFinal, Categories.CM },
        { ISC.ConsonantHeadLetter, Categories.C },
        { ISC.ConsonantKiller, Categories.M },
        { ISC.ConsonantMedial, Categories.CM },
        { ISC.ConsonantPlaceholder, Categories.Placeholder },
        { ISC.ConsonantPrecedingRepha, Categories.Repha },
        { ISC.ConsonantPrefixed, Categories.X },
        { ISC.ConsonantSubjoined, Categories.CM },
        { ISC.ConsonantSucceedingRepha, Categories.N },
        { ISC.ConsonantWithStacker, Categories.Repha },
        { ISC.GeminationMark, Categories.SM }, // https://github.com/harfbuzz/harfbuzz/issues/552
        { ISC.InvisibleStacker, Categories.Coeng }, // TODO: Use H once we add explicit Khmer shaper
        { ISC.Joiner, Categories.ZWJ },
        { ISC.ModifyingLetter, Categories.X },
        { ISC.NonJoiner, Categories.ZWNJ },
        { ISC.Nukta, Categories.N },
        { ISC.Number, Categories.Placeholder },
        { ISC.NumberJoiner, Categories.Placeholder },
        { ISC.PureKiller, Categories.M },
        { ISC.RegisterShifter, Categories.RS },
        { ISC.SyllableModifier, Categories.M },
        { ISC.ToneLetter, Categories.X },
        { ISC.ToneMark, Categories.N },
        { ISC.Virama, Categories.H },
        { ISC.Visarga, Categories.SM },
        { ISC.Vowel, Categories.V },
        { ISC.VowelDependent, Categories.M },
        { ISC.VowelIndependent, Categories.V }
    };

    // Per-codepoint category overrides for Indic-style shaping.
    // These values augment the base Unicode/Indic tables to match
    // HarfBuzz behavior for Indic, Khmer, and Myanmar scripts.
    private static readonly Dictionary<int, Categories> IndicShapingOverrides = new()
    {
        // --------------------------------------------------------------------
        // Variation selectors
        //
        // These are the Unicode variation selectors. They only appear in the
        // Myanmar grammar, but they are not Myanmar specific.
        // --------------------------------------------------------------------
        { 0xFE00, Categories.VS },
        { 0xFE01, Categories.VS },
        { 0xFE02, Categories.VS },
        { 0xFE03, Categories.VS },
        { 0xFE04, Categories.VS },
        { 0xFE05, Categories.VS },
        { 0xFE06, Categories.VS },
        { 0xFE07, Categories.VS },
        { 0xFE08, Categories.VS },
        { 0xFE09, Categories.VS },
        { 0xFE0A, Categories.VS },
        { 0xFE0B, Categories.VS },
        { 0xFE0C, Categories.VS },
        { 0xFE0D, Categories.VS },
        { 0xFE0E, Categories.VS },
        { 0xFE0F, Categories.VS },

        // --------------------------------------------------------------------
        // Placeholder-like characters from the OT Myanmar spec
        //
        // These are listed in the Myanmar OpenType spec as placeholder bases,
        // but they are not Myanmar specific.
        // --------------------------------------------------------------------
        { 0x2015, Categories.Placeholder },
        { 0x2022, Categories.Placeholder },
        { 0x25FB, Categories.Placeholder },
        { 0x25FC, Categories.Placeholder },
        { 0x25FD, Categories.Placeholder },
        { 0x25FE, Categories.Placeholder },

        // --------------------------------------------------------------------
        // Indic overrides
        // --------------------------------------------------------------------

        // Ra (script-specific consonant Ra)
        { 0x0930, Categories.Ra }, // Devanagari
        { 0x09B0, Categories.Ra }, // Bengali
        { 0x09F0, Categories.Ra }, // Bengali
        { 0x0A30, Categories.Ra }, // Gurmukhi, no reph
        { 0x0AB0, Categories.Ra }, // Gujarati
        { 0x0B30, Categories.Ra }, // Oriya
        { 0x0BB0, Categories.Ra }, // Tamil, no reph
        { 0x0C30, Categories.Ra }, // Telugu, reph formed only with ZWJ
        { 0x0CB0, Categories.Ra }, // Kannada
        { 0x0D30, Categories.Ra }, // Malayalam, no reph, logical repha

        // These act more like bindus.
        { 0x0953, Categories.SM },
        { 0x0954, Categories.SM },

        // U+0A40 GURMUKHI VOWEL SIGN II may be preceded by U+0A02 GURMUKHI SIGN BINDI.
        { 0x0A40, Categories.MPst },

        // Characters that act like consonants.
        { 0x0A72, Categories.C },
        { 0x0A73, Categories.C },
        { 0x1CF5, Categories.C },
        { 0x1CF6, Categories.C },

        // TODO: These should only be allowed after a visarga.
        // For now, treat them like regular tone marks (A).
        { 0x1CE2, Categories.A },
        { 0x1CE3, Categories.A },
        { 0x1CE4, Categories.A },
        { 0x1CE5, Categories.A },
        { 0x1CE6, Categories.A },
        { 0x1CE7, Categories.A },
        { 0x1CE8, Categories.A },

        // TODO: Should only be allowed after some nasalization marks.
        // For now, treat as tone mark (A).
        { 0x1CED, Categories.A },

        // Take marks in standalone clusters, similar to Avagraha, so classify
        // as Symbol.
        { 0xA8F2, Categories.Symbol },
        { 0xA8F3, Categories.Symbol },
        { 0xA8F4, Categories.Symbol },
        { 0xA8F5, Categories.Symbol },
        { 0xA8F6, Categories.Symbol },
        { 0xA8F7, Categories.Symbol },
        { 0x1CE9, Categories.Symbol },
        { 0x1CEA, Categories.Symbol },
        { 0x1CEB, Categories.Symbol },
        { 0x1CEC, Categories.Symbol },
        { 0x1CEE, Categories.Symbol },
        { 0x1CEF, Categories.Symbol },
        { 0x1CF0, Categories.Symbol },
        { 0x1CF1, Categories.Symbol },

        // Special matra classification.
        // https://github.com/harfbuzz/harfbuzz/issues/524
        { 0x0A51, Categories.M },

        // Grantha marks that can also appear in Tamil (per ScriptExtensions.txt),
        // so the Indic shaper must know their categories.
        { 0x11301, Categories.SM },
        { 0x11302, Categories.SM },
        { 0x11303, Categories.SM },
        { 0x1133B, Categories.N },
        { 0x1133C, Categories.N },

        // Additional nukta-like marks.
        // https://github.com/harfbuzz/harfbuzz/issues/552
        { 0x0AFB, Categories.N },

        // https://github.com/harfbuzz/harfbuzz/issues/2849
        { 0x0B55, Categories.N },

        // Extra placeholders for standalone shaping.
        // https://github.com/harfbuzz/harfbuzz/pull/1613
        { 0x09FC, Categories.Placeholder },

        // https://github.com/harfbuzz/harfbuzz/pull/623
        { 0x0C80, Categories.Placeholder },

        // https://github.com/harfbuzz/harfbuzz/pull/3511
        { 0x0D04, Categories.Placeholder },
        { 0x25CC, Categories.Dotted_Circle },

        // --------------------------------------------------------------------
        // Khmer overrides
        // --------------------------------------------------------------------
        { 0x179A, Categories.Ra },       // Khmer Ra
        { 0x17C6, Categories.N }, // TODO: Replace with Xgroup as per below once we support it.

        // { 0x17CC, Categories.Robatic },
        // { 0x17C9, Categories.Robatic },
        // { 0x17CA, Categories.Robatic },
        // { 0x17C6, Categories.Xgroup },
        // { 0x17CB, Categories.Xgroup },
        // { 0x17CD, Categories.Xgroup },
        // { 0x17CE, Categories.Xgroup },
        // { 0x17CF, Categories.Xgroup },
        // { 0x17D0, Categories.Xgroup },
        // { 0x17D1, Categories.Xgroup },
        // { 0x17C7, Categories.Ygroup },
        // { 0x17C8, Categories.Ygroup },
        // { 0x17DD, Categories.Ygroup },
        // { 0x17D3, Categories.Ygroup },   // Just guessing. Uniscribe does not categorize it.

        // https://github.com/harfbuzz/harfbuzz/issues/2384
        { 0x17D9, Categories.Placeholder },

        // --------------------------------------------------------------------
        // Myanmar overrides
        //
        // Spec reference:
        // https://learn.microsoft.com/en-us/typography/script-development/myanmar#analyze
        // --------------------------------------------------------------------

        // The spec says C, IndicSyllableCategory says Consonant_Placeholder.
        { 0x104E, Categories.C },
        { 0x1004, Categories.Ra },
        { 0x101B, Categories.Ra },
        { 0x105A, Categories.Ra },
        { 0x1032, Categories.A },
        { 0x1036, Categories.A },
        { 0x103A, Categories.As },

        // 0x1040: D0 in the spec, but Uniscribe does not seem to treat it as such.
        // (intentionally not overridden here)
        { 0x103E, Categories.MH },
        { 0x1060, Categories.ML },
        { 0x103C, Categories.MR },
        { 0x103D, Categories.MW },
        { 0x1082, Categories.MW },
        { 0x103B, Categories.MY },
        { 0x105E, Categories.MY },
        { 0x105F, Categories.MY },
        { 0x1063, Categories.PT },
        { 0x1064, Categories.PT },
        { 0x1069, Categories.PT },
        { 0x106A, Categories.PT },
        { 0x106B, Categories.PT },
        { 0x106C, Categories.PT },
        { 0x106D, Categories.PT },
        { 0xAA7B, Categories.PT },
        { 0x1038, Categories.SM },
        { 0x1087, Categories.SM },
        { 0x1088, Categories.SM },
        { 0x1089, Categories.SM },
        { 0x108A, Categories.SM },
        { 0x108B, Categories.SM },
        { 0x108C, Categories.SM },
        { 0x108D, Categories.SM },
        { 0x108F, Categories.SM },
        { 0x109A, Categories.SM },
        { 0x109B, Categories.SM },
        { 0x109C, Categories.SM },
        { 0x104A, Categories.Placeholder },
    };

    private static readonly Dictionary<IPC, Positions> PositionMap = new()
    {
        { IPC.Left,  Positions.Pre_C },
        { IPC.Top,  Positions.Above_C },
        { IPC.Bottom,  Positions.Below_C },
        { IPC.Right,  Positions.Post_C },

          // These should resolve to the position of the last part of the split sequence.
        { IPC.BottomAndRight,  Positions.Post_C },
        { IPC.LeftAndRight,  Positions.Post_C },
        { IPC.TopAndBottom,  Positions.Below_C },
        { IPC.TopAndBottomAndRight,  Positions.Post_C },
        { IPC.TopAndLeft,  Positions.Above_C },
        { IPC.TopAndLeftAndRight,  Positions.Post_C },
        { IPC.TopAndRight,  Positions.Post_C },
        { IPC.Overstruck,  Positions.After_Main },
        { IPC.VisualOrderLeft,  Positions.Pre_M }
    };

    private static Positions MatraPosition(Codepoint c, Positions pos)
        => pos switch
        {
            Positions.Pre_C => Positions.Pre_M,
            Positions.Post_C => c.Block switch
            {
                "Devanagari" => Positions.After_Sub,
                "Bengali" => Positions.After_Post,
                "Gurmukhi" => Positions.After_Post,
                "Gujarati" => Positions.After_Post,
                "Oriya" => Positions.After_Post,
                "Tamil" => Positions.After_Post,
                "Telugu" => c.Code <= 0x0C42 ? Positions.Before_Sub : Positions.After_Sub,
                "Kannada" => c.Code is < 0x0CC3 or > 0xCD6 ? Positions.Before_Sub : Positions.After_Sub,
                "Malayalam" => Positions.After_Post,
                "Sinhala" => Positions.After_Sub,
                "Khmer" => Positions.After_Post,
                _ => Positions.After_Sub,
            },
            Positions.Above_C => c.Block switch
            {
                "Devanagari" => Positions.After_Sub,
                "Gurmukhi" => Positions.After_Post, // Deviate from spec
                "Gujarati" => Positions.After_Sub,
                "Oriya" => Positions.After_Main,
                "Tamil" => Positions.After_Sub,
                "Telugu" => Positions.Before_Sub,
                "Kannada" => Positions.Before_Sub,
                "Sinhala" => Positions.After_Sub,
                "Khmer" => Positions.After_Post,
                _ => Positions.After_Sub,
            },
            Positions.Below_C => c.Block switch
            {
                "Devanagari" => Positions.After_Sub,
                "Bengali" => Positions.After_Sub,
                "Gurmukhi" => Positions.After_Post,
                "Gujarati" => Positions.After_Post,
                "Oriya" => Positions.After_Sub,
                "Tamil" => Positions.After_Post,
                "Telugu" => Positions.Before_Sub,
                "Kannada" => Positions.Before_Sub,
                "Malayalam" => Positions.After_Post,
                "Sinhala" => Positions.After_Sub,
                "Khmer" => Positions.After_Post,
                _ => Positions.After_Sub,
            },
            _ => pos,
        };

    private static int GetPosition(Codepoint codepoint, Categories category)
    {
        Positions position = PositionMap.GetValueOrDefault(codepoint.IndicPositionalCategory, Positions.End);

        // Keep in sync with ethe constants flag in the shaper.
        if (category is Categories.C or Categories.Ra or Categories.CM or Categories.V or Categories.Placeholder or Categories.Dotted_Circle)
        {
            position = Positions.Base_C;
        }
        else if (category is Categories.M)
        {
            position = MatraPosition(codepoint, position);
        }
        else if (category is Categories.SM or Categories.A or Categories.Symbol)
        {
            position = Positions.SMVD;
        }

        // Oriya Bindu is Before_Sub in the spec.
        if (codepoint.Code == 0x0B01)
        {
            position = Positions.Before_Sub;
        }

        return (int)Math.Log((int)position, 2);
    }

    private static void GenerateIndicShapingDataTrie(Codepoint[] codePoints)
    {
        SetBlocks(codePoints);

        Dictionary<string, int> symbols = Enum.GetValues<Categories>().ToDictionary(c => c.ToString(), c => (int)c);

        UnicodeTrieBuilder builder = new();
        for (int i = 0; i < codePoints.Length; i++)
        {
            Codepoint codePoint = codePoints[i];
            Categories rawCategory = IndicShapingOverrides.GetValueOrDefault(
                codePoint.Code,
                CategoryMap.GetValueOrDefault(codePoint.IndicSyllabicCategory, Categories.X));

            // Apply HarfBuzz-style matra normalization for Khmer/Myanmar blocks.
            Categories category = NormalizeCategoryForBlock(codePoint, rawCategory);

            int position = GetPosition(codePoint, category);

            builder.Set(codePoint.Code, (uint)((symbols[category.ToString()] << 8) | position));
        }

        UnicodeTrie trie = builder.Freeze();
        GenerateTrieClass("IndicShaping", trie);

        StateMachine machine = GetStateMachine("indic", symbols);

        GenerateDataClass("IndicShaping", null, null, machine, true);
    }

    private static Categories NormalizeCategoryForBlock(Codepoint codepoint, Categories category)
    {
        // HarfBuzz: matra_categories = ('M', 'MPst')
        // For Khmer and Myanmar blocks, convert generic matras (M/MPst)
        // into positional vowel categories VPre/VAbv/VBlw/VPst based on
        // the base positional category (PRE_C / ABOVE_C / BELOW_C / POST_C).
        if (category is Categories.M or Categories.MPst)
        {
            string block = codepoint.Block;

            // TODO: Once we implement the Khmer shaper, enable Khmer here too.
            // if (block.StartsWith("Khmer", StringComparison.Ordinal) ||
            //    block.StartsWith("Myanmar", StringComparison.Ordinal))
            if (block.StartsWith("Myanmar", StringComparison.Ordinal))
            {
                // Base positional category from IndicPositionalCategory.txt
                Positions basePos = PositionMap.GetValueOrDefault(
                    codepoint.IndicPositionalCategory,
                    Positions.End);

                return basePos switch
                {
                    Positions.Pre_C => Categories.VPre,
                    Positions.Above_C => Categories.VAbv,
                    Positions.Below_C => Categories.VBlw,
                    Positions.Post_C => Categories.VPst,
                    _ => category
                };
            }
        }

        return category;
    }

    private static void SetBlocks(Codepoint[] codePoints)
    {
        Regex regex = new(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*([\w\s-]+)");

        using StreamReader sr = GetStreamReader("Blocks.txt");
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                string start = match.Groups[1].Value;
                string end = match.Groups[2].Value;
                string block = match.Groups[3].Value;

                if (string.IsNullOrEmpty(end))
                {
                    end = start;
                }

                int min = ParseHexInt(start);
                int max = ParseHexInt(end);

                for (int i = min; i <= max; i++)
                {
                    // TODO: Make an enum of block values and create a trie. This is painfully slow.
                    Codepoint? codePoint = Array.Find(codePoints, x => x.Code == i);
                    if (codePoint is null)
                    {
                        continue;
                    }

                    codePoint.Block = block;
                }
            }
        }
    }
}
