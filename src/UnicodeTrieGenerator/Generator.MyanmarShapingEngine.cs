// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using UnicodeTrieGenerator.StateAutomation;
using MyanmarCategories = SixLabors.Fonts.Unicode.Resources.IndicShapingData.MyanmarCategories;

namespace UnicodeTrieGenerator;

/// <content>
/// Contains code to generate a trie and state machine for storing Myanmar Shaping Data category data.
/// </content>
public static partial class Generator
{
    private static void GenerateMyanmarShapingData()
    {
        // HarfBuzz's Ragel state machines use a dense, zero-based alphabet
        // (0..N-1) for DFA transitions, even though the underlying shaping
        // categories (C, V, H, MR, MW, VBlw, etc.) are sparse numeric values.
        // For example, Myanmar categories include values such as 1, 2, 3, 4,
        // 15, 18, 20, 21, 32, 35, 41, 57.
        //
        // Our PEG-generated state machine table also expects its input symbols
        // to be dense 0..N-1 indices, not HarfBuzz's raw category codes.
        // Therefore, we build a compact symbol map by assigning each
        // MyanmarCategories enum value a sequential integer (0..N-1) in the
        // order they appear in the enum.
        //
        // The state machine is generated against these compact IDs, and later
        // SetupSyllables maps each Myanmar category to the corresponding
        // compact symbol before running the DFA.
        MyanmarCategories[] categories = Enum.GetValues<MyanmarCategories>();
        Dictionary<string, int> symbols = new(categories.Length);
        int id = 0;

        foreach (MyanmarCategories c in categories)
        {
            symbols[c.ToString()] = id++;
        }

        StateMachine machine = GetStateMachine("myanmar", symbols);

        GenerateDataClass("MyanmarShaping", null, null, machine, true);
    }
}
