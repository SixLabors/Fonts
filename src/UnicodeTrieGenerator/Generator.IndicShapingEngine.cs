// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text.RegularExpressions;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
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
        { ISC.GeminationMark, Categories.SM },
        { ISC.InvisibleStacker, Categories.Coeng },
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

    private static readonly Dictionary<int, Categories> IndicShapingOverrides = new()
    {
        { 0x0953, Categories.SM },
        { 0x0954, Categories.SM },
        { 0x0A72, Categories.C },
        { 0x0A73, Categories.C },
        { 0x1CF5, Categories.C },
        { 0x1CF6, Categories.C },
        { 0x1CE2, Categories.A },
        { 0x1CE3, Categories.A },
        { 0x1CE4, Categories.A },
        { 0x1CE5, Categories.A },
        { 0x1CE6, Categories.A },
        { 0x1CE7, Categories.A },
        { 0x1CE8, Categories.A },
        { 0x1CED, Categories.A },
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
        { 0x17C6, Categories.N },
        { 0x2010, Categories.Placeholder },
        { 0x2011, Categories.Placeholder },
        { 0x25CC, Categories.Dotted_Circle },
        { 0x0930, Categories.Ra },
        { 0x09B0, Categories.Ra },
        { 0x09F0, Categories.Ra },
        { 0x0A30, Categories.Ra },
        { 0x0AB0, Categories.Ra },
        { 0x0B30, Categories.Ra },
        { 0x0BB0, Categories.Ra },
        { 0x0C30, Categories.Ra },
        { 0x0CB0, Categories.Ra },
        { 0x0D30, Categories.Ra },
        { 0x0DBB, Categories.Ra },
        { 0x179A, Categories.Ra }
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

        if ((category & IndicShapingData.ConsonantFlags) != 0)
        {
            position = Positions.Base_C;
        }
        else if (category == Categories.M)
        {
            position = MatraPosition(codepoint, position);
        }
        else if (category is Categories.SM or Categories.VD or Categories.A or Categories.Symbol)
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

        var symbols = ((Categories[])Enum.GetValues(typeof(Categories))).ToDictionary(c => c.ToString(), c => (int)Math.Log((int)c, 2));

        UnicodeTrieBuilder builder = new();
        for (int i = 0; i < codePoints.Length; i++)
        {
            Codepoint codePoint = codePoints[i];
            Categories category = IndicShapingOverrides.GetValueOrDefault(codePoint.Code, CategoryMap.GetValueOrDefault(codePoint.IndicSyllabicCategory, Categories.X));
            int position = GetPosition(codePoint, category);

            builder.Set(codePoint.Code, (uint)((symbols[category.ToString()] << 8) | position));
        }

        UnicodeTrie trie = builder.Freeze();
        GenerateTrieClass("IndicShaping", trie);

        StateMachine machine = GetStateMachine("indic", symbols);

        GenerateDataClass("IndicShaping", null, null, machine, true);
    }

    private static void SetBlocks(Codepoint[] codePoints)
    {
        var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*([\w\s-]+)");

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
