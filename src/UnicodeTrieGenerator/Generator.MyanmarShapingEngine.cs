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
        Dictionary<string, int> symbols = Enum.GetValues<MyanmarCategories>().ToDictionary(c => c.ToString(), c => (int)c);

        StateMachine machine = GetStateMachine("myanmar", symbols);

        GenerateDataClass("MyanmarShaping", null, null, machine, true);
    }
}
