// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Text.RegularExpressions;
using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using GC = System.Globalization.UnicodeCategory;
using IPC = SixLabors.Fonts.Unicode.IndicPositionalCategory;
using ISC = SixLabors.Fonts.Unicode.IndicSyllabicCategory;

namespace UnicodeTrieGenerator;

/// <content>
/// Contains code to generate a trie and state machine for storing Universal Shaping Data category data.
/// </content>
public static partial class Generator
{
    /// <summary>
    /// The Sinhala sign Al-Lakuna code point.
    /// </summary>
    private const int SinhalaSignAlLakuna = 0x0DCA;

    /// <summary>
    /// The Tai Tham sign Sakot code point.
    /// </summary>
    private const int TaiThamSignSakot = 0x1A60;

    /// <summary>
    /// Generator-only value for the USE Symbol_Modifier extension.
    /// </summary>
    private const ISC SymbolModifierCategory = (ISC)0x100;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph extension.
    /// </summary>
    private const ISC HieroglyphCategory = (ISC)0x101;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Joiner extension.
    /// </summary>
    private const ISC HieroglyphJoinerCategory = (ISC)0x102;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Mark_Begin extension.
    /// </summary>
    private const ISC HieroglyphMarkBeginCategory = (ISC)0x103;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Mark_End extension.
    /// </summary>
    private const ISC HieroglyphMarkEndCategory = (ISC)0x104;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Mirror extension.
    /// </summary>
    private const ISC HieroglyphMirrorCategory = (ISC)0x105;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Modifier extension.
    /// </summary>
    private const ISC HieroglyphModifierCategory = (ISC)0x106;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Segment_Begin extension.
    /// </summary>
    private const ISC HieroglyphSegmentBeginCategory = (ISC)0x107;

    /// <summary>
    /// Generator-only value for the USE Hieroglyph_Segment_End extension.
    /// </summary>
    private const ISC HieroglyphSegmentEndCategory = (ISC)0x108;

    /// <summary>
    /// Characters explicitly treated as generic bases by the USE category table.
    /// </summary>
    private static readonly int[] GenericBaseCodePoints = [0x2015, 0x2022, 0x25FB, 0x25FC, 0x25FD, 0x25FE];

    /// <summary>
    /// Default-ignorable characters excluded from the word-joiner category.
    /// </summary>
    private static readonly int[] VisibleDefaultIgnorables = [0x115F, 0x1160, 0x3164, 0xFFA0, 0x1BCA0, 0x1BCA1, 0x1BCA2, 0x1BCA3];

    /// <summary>
    /// Syllabic category names defined by the USE override file rather than the Unicode property.
    /// </summary>
    private static readonly Dictionary<string, ISC> UniversalSyllabicCategoryOverrides = new()
    {
        { "Consonant_Final_Modifier", ISC.SyllableModifier },
        { "Symbol_Modifier", SymbolModifierCategory },
        { "Hieroglyph", HieroglyphCategory },
        { "Hieroglyph_Joiner", HieroglyphJoinerCategory },
        { "Hieroglyph_Mark_Begin", HieroglyphMarkBeginCategory },
        { "Hieroglyph_Mark_End", HieroglyphMarkEndCategory },
        { "Hieroglyph_Mirror", HieroglyphMirrorCategory },
        { "Hieroglyph_Modifier", HieroglyphModifierCategory },
        { "Hieroglyph_Segment_Begin", HieroglyphSegmentBeginCategory },
        { "Hieroglyph_Segment_End", HieroglyphSegmentEndCategory }
    };

    /// <summary>
    /// The complete alphabet consumed by the generated USE state machine.
    /// </summary>
    private static readonly string[] UniversalCategoryNames =
    [
        "O", "B", "N", "GB", "CGJ", "SUB", "H", "HN", "ZWNJ", "WJ", "R", "CS", "IS", "Sk", "G", "J", "SB", "SE", "HVM", "HM", "HR", "RK",
        "FAbv", "FBlw", "FPst", "MAbv", "MBlw", "MPst", "MPre", "CMAbv", "CMBlw", "VAbv", "VBlw", "VPst", "VPre", "VMAbv", "VMBlw", "VMPst",
        "VMPre", "SMAbv", "SMBlw", "FMAbv", "FMBlw", "FMPst"
    ];

    /// <summary>
    /// Categories before which a leading repha stops during reordering.
    /// </summary>
    private static readonly string[] UniversalPostBaseCategoryNames =
    [
        "FAbv", "FBlw", "FPst", "FMAbv", "FMBlw", "FMPst", "MAbv", "MBlw", "MPst", "MPre", "VAbv", "VBlw", "VPst", "VPre", "VMAbv", "VMBlw",
        "VMPst", "VMPre"
    ];

    /// <summary>
    /// Positional suffixes applied to categories using the Indic positional property.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, List<IPC>>> UniversalPositions = new()
    {
        {
            "F",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom]
                },
                {
                    "Pst", [IPC.Right]
                }
            }
        },
        {
            "M",
            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom, IPC.BottomAndLeft, IPC.BottomAndRight]
                },
                {
                    "Pst", [IPC.Right]
                },
                {
                    "Pre", [IPC.Left, IPC.TopAndBottomAndLeft]
                }
            }
        },
        {
            "CM",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom, IPC.Overstruck]
                }
            }
        },
        {
            "V",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top, IPC.TopAndBottom, IPC.TopAndBottomAndRight, IPC.TopAndRight]
                },
                {
                    "Blw", [IPC.Bottom, IPC.Overstruck, IPC.BottomAndRight]
                },
                {
                    "Pst", [IPC.Right]
                },
                {
                    "Pre", [IPC.Left, IPC.TopAndLeft, IPC.TopAndLeftAndRight, IPC.LeftAndRight]
                }
            }
        },
        {
            "VM",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom, IPC.Overstruck]
                },
                {
                    "Pst", [IPC.Right]
                },
                {
                    "Pre", [IPC.Left]
                }
            }
        },
        {
            "SM",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom]
                }
            }
        },
        {
            "FM",

            new Dictionary<string, List<IPC>>()
            {
                {
                    "Abv", [IPC.Top]
                },
                {
                    "Blw", [IPC.Bottom]
                },
                {
                    "Pst", [IPC.NA]
                }
            }
        }
    };

    /// <summary>
    /// Adds the positional suffix used by the shaping machine when a category has
    /// position-specific symbols.
    /// </summary>
    private static string GetPositionalCategory(Codepoint code, string uSE)
    {
        IPC uIPC = code.IndicPositionalCategory;
        if (UniversalPositions.TryGetValue(uSE, out Dictionary<string, List<IPC>>? pos))
        {
            foreach (string key in pos.Keys)
            {
                if (pos[key].Contains(uIPC))
                {
                    return uSE + key;
                }
            }
        }

        return uSE;
    }

    /// <summary>
    /// Maps a code point to the category consumed by the Universal Shaping Engine
    /// state machine.
    /// </summary>
    private static string? GetCategory(Codepoint code)
    {
        string? category = code switch
        {
            _ when IsBase(code) => "B",
            _ when code.IndicSyllabicCategory == ISC.BrahmiJoiningNumber => "N",
            _ when IsGenericBase(code) => "GB",
            _ when IsCombiningGraphemeJoiner(code) => "CGJ",
            _ when IsFinalConsonant(code) => "F",
            _ when code.IndicSyllabicCategory == ISC.SyllableModifier => "FM",
            _ when IsMedialConsonant(code) => "M",
            _ when code.IndicSyllabicCategory is ISC.Nukta or ISC.GeminationMark or ISC.ConsonantKiller => "CM",
            _ when code.IndicSyllabicCategory == ISC.ConsonantSubjoined && code.Category != GC.OtherLetter => "SUB",
            _ when code.IndicSyllabicCategory == ISC.ConsonantWithStacker => "CS",
            _ when code.IndicSyllabicCategory == ISC.Virama && code.Code != SinhalaSignAlLakuna => "H",
            _ when code.Code == SinhalaSignAlLakuna => "HVM",
            _ when code.IndicSyllabicCategory == ISC.NumberJoiner => "HN",
            _ when code.IndicSyllabicCategory == ISC.InvisibleStacker && code.Code != TaiThamSignSakot => "IS",
            _ when code.IndicSyllabicCategory == HieroglyphCategory => "G",
            _ when code.IndicSyllabicCategory == HieroglyphModifierCategory => "HM",
            _ when code.IndicSyllabicCategory == HieroglyphMirrorCategory => "HR",
            _ when code.IndicSyllabicCategory == HieroglyphJoinerCategory => "J",
            _ when code.IndicSyllabicCategory is HieroglyphMarkBeginCategory or HieroglyphSegmentBeginCategory => "SB",
            _ when code.IndicSyllabicCategory is HieroglyphMarkEndCategory or HieroglyphSegmentEndCategory => "SE",
            _ when code.IndicSyllabicCategory == ISC.NonJoiner => "ZWNJ",
            _ when IsOther(code) => "O",
            _ when code.IndicSyllabicCategory == ISC.ReorderingKiller => "RK",
            _ when code.IndicSyllabicCategory is ISC.ConsonantPrecedingRepha or ISC.ConsonantPrefixed => "R",
            _ when code.Code == TaiThamSignSakot => "Sk",
            _ when code.IndicSyllabicCategory == SymbolModifierCategory => "SM",
            _ when IsVowel(code) => "V",
            _ when IsVowelModifier(code) => "VM",
            _ when IsWordJoiner(code) => "WJ",
            _ => null,
        };

        return category is null ? null : GetPositionalCategory(code, category);
    }

    /// <summary>
    /// Determines whether a code point is a generic base.
    /// </summary>
    private static bool IsGenericBase(Codepoint code)
        => code.IndicSyllabicCategory == ISC.ConsonantPlaceholder || GenericBaseCodePoints.Contains(code.Code);

    /// <summary>
    /// Determines whether a code point is omitted from the syllable-machine input.
    /// </summary>
    private static bool IsCombiningGraphemeJoiner(Codepoint code)
        => code.IndicSyllabicCategory == ISC.Joiner
        || (code.DefaultIgnorable && code.Category is GC.SpacingCombiningMark or GC.EnclosingMark or GC.NonSpacingMark);

    /// <summary>
    /// Determines whether a code point is a final consonant.
    /// </summary>
    private static bool IsFinalConsonant(Codepoint code)
        => (code.IndicSyllabicCategory == ISC.ConsonantFinal && code.Category != GC.OtherLetter)
        || code.IndicSyllabicCategory == ISC.ConsonantSucceedingRepha;

    /// <summary>
    /// Determines whether a code point is a medial consonant.
    /// </summary>
    private static bool IsMedialConsonant(Codepoint code)
        => (code.IndicSyllabicCategory == ISC.ConsonantMedial && code.Category != GC.OtherLetter)
        || code.IndicSyllabicCategory == ISC.ConsonantInitialPostfixed;

    /// <summary>
    /// Determines whether a code point belongs to the machine's general other
    /// category.
    /// </summary>
    private static bool IsOther(Codepoint code)
        => (code.Category == GC.OtherPunctuation
        || code.IndicSyllabicCategory is ISC.ConsonantDead or ISC.Joiner or ISC.ModifyingLetter or ISC.Other)
        && !IsBase(code)
        && !IsGenericBase(code)
        && !IsCombiningGraphemeJoiner(code)
        && code.IndicSyllabicCategory != SymbolModifierCategory
        && !IsWordJoiner(code);

    /// <summary>
    /// Determines whether a code point is a dependent vowel or pure killer.
    /// </summary>
    private static bool IsVowel(Codepoint code)
        => code.IndicSyllabicCategory == ISC.PureKiller
        || (code.Category != GC.OtherLetter && code.IndicSyllabicCategory is ISC.Vowel or ISC.VowelDependent);

    /// <summary>
    /// Determines whether a code point is a vowel modifier.
    /// </summary>
    private static bool IsVowelModifier(Codepoint code)
        => code.IndicSyllabicCategory is ISC.ToneMark or ISC.CantillationMark or ISC.RegisterShifter or ISC.Visarga
        || (code.Category != GC.OtherLetter && code.IndicSyllabicCategory == ISC.Bindu);

    /// <summary>
    /// Determines whether a code point is a word joiner or reserved character.
    /// </summary>
    private static bool IsWordJoiner(Codepoint code)
        => (code.DefaultIgnorable
        && !VisibleDefaultIgnorables.Contains(code.Code)
        && code.IndicSyllabicCategory == ISC.Other
        && !IsCombiningGraphemeJoiner(code))
        || code.Category == GC.OtherNotAssigned;

    /// <summary>
    /// Generates the character categories and state machine used by the Universal Shaping Engine.
    /// </summary>
    private static List<Codepoint> GenerateUniversalShapingDataTrie(UnicodeTrie unicodeGeneralCategory, UnicodeTrie indicSyllabicCategoryTrie, UnicodeTrie indicPositionalCategoryTrie, UnicodeTrie arabicJoiningTrie, UnicodeTrie scriptTrie)
    {
        static ArabicJoiningType GetJoiningType(int codePoint, uint value, GC category)
        {
            ArabicJoiningType type = (ArabicJoiningType)(value & 0xFF);

            // All others not explicitly listed have joining type U
            if (type == ArabicJoiningType.NonJoining)
            {
                // 200C; ZERO WIDTH NON-JOINER; U; No_Joining_Group
                // 200D; ZERO WIDTH JOINER; C; No_Joining_Group
                // 202F; NARROW NO-BREAK SPACE; U; No_Joining_Group
                // 2066; LEFT-TO-RIGHT ISOLATE; U; No_Joining_Group
                // 2067; RIGHT-TO-LEFT ISOLATE; U; No_Joining_Group
                // 2068; FIRST STRONG ISOLATE; U; No_Joining_Group
                // 2069; POP DIRECTIONAL ISOLATE; U; No_Joining_Group
                if (codePoint is 0x200C
                    or 0x200D
                    or 0x202F
                    or 0x2066
                    or 0x2067
                    or 0x2068
                    or 0x2069)
                {
                    return type;
                }

                // Those that are not explicitly listed and that are of General Category Mn, Me, or Cf have joining type T.
                if (category is GC.NonSpacingMark or GC.EnclosingMark or GC.Format)
                {
                    type = ArabicJoiningType.Transparent;
                }
            }

            return type;
        }

        HashSet<int> universalCodePointValues = [];
        Regex propertyRegex = UnicodePropertyRowRegex();
        AddCodePointRanges(universalCodePointValues, "IndicSyllabicCategory.txt", propertyRegex);
        AddCodePointRanges(universalCodePointValues, "IndicSyllabicCategory-Additional.txt", propertyRegex);
        AddCodePointRanges(universalCodePointValues, "IndicPositionalCategory.txt", propertyRegex);
        AddCodePointRanges(universalCodePointValues, "IndicPositionalCategory-Additional.txt", propertyRegex);
        AddCodePointRanges(universalCodePointValues, "ArabicShaping.txt", ArabicShapingRowRegex());

        UnicodeTrieBuilder defaultIgnorableBuilder = new();
        using (StreamReader propertyReader = GetStreamReader("DerivedCoreProperties.txt"))
        {
            string? propertyLine;
            while ((propertyLine = propertyReader.ReadLine()) != null)
            {
                Match match = propertyRegex.Match(propertyLine);
                if (!match.Success || match.Groups[3].Value != "Default_Ignorable_Code_Point")
                {
                    continue;
                }

                string end = match.Groups[2].Value;
                if (string.IsNullOrEmpty(end))
                {
                    end = match.Groups[1].Value;
                }

                int min = ParseHexInt(match.Groups[1].Value);
                int max = ParseHexInt(end);
                defaultIgnorableBuilder.SetRange(min, max, 1, true);

                for (int codePoint = min; codePoint <= max; codePoint++)
                {
                    universalCodePointValues.Add(codePoint);
                }
            }
        }

        UnicodeTrie defaultIgnorableTrie = defaultIgnorableBuilder.Freeze();
        List<Codepoint> codePoints = [];
        using StreamReader sr = GetStreamReader("UnicodeData.txt");
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            string[] parts = line.Split(';');

            Codepoint codepoint = new()
            {
                Code = ParseHexInt(parts[0])
            };

            codepoint.Category = (GC)unicodeGeneralCategory.Get((uint)codepoint.Code);
            codepoint.DefaultIgnorable = defaultIgnorableTrie.Get((uint)codepoint.Code) != 0;

            codepoint.IndicSyllabicCategory = (ISC)indicSyllabicCategoryTrie.Get((uint)codepoint.Code);
            codepoint.IndicPositionalCategory = (IPC)indicPositionalCategoryTrie.Get((uint)codepoint.Code);

            uint value = arabicJoiningTrie.Get((uint)codepoint.Code);
            codepoint.ArabicJoiningType = GetJoiningType(codepoint.Code, value, codepoint.Category);
            codepoint.ArabicJoiningGroup = (ArabicJoiningGroup)((value >> 16) & 0xFF);

            codePoints.Add(codepoint);
        }

        // The USE table includes only characters present in the syllabic, positional,
        // joining, or default-ignorable inputs, then removes scripts handled by dedicated shapers.
        List<Codepoint> universalCodePoints = new(universalCodePointValues.Count);
        foreach (int codePointValue in universalCodePointValues)
        {
            GC category = (GC)unicodeGeneralCategory.Get((uint)codePointValue);
            uint joiningValue = arabicJoiningTrie.Get((uint)codePointValue);

            universalCodePoints.Add(new Codepoint
            {
                Code = codePointValue,
                IndicSyllabicCategory = (ISC)indicSyllabicCategoryTrie.Get((uint)codePointValue),
                IndicPositionalCategory = (IPC)indicPositionalCategoryTrie.Get((uint)codePointValue),
                ArabicJoiningType = GetJoiningType(codePointValue, joiningValue, category),
                ArabicJoiningGroup = (ArabicJoiningGroup)((joiningValue >> 16) & 0xFF),
                Category = category,
                DefaultIgnorable = defaultIgnorableTrie.Get((uint)codePointValue) != 0
            });
        }

        OverrideIndicSyllabicCategory(universalCodePoints);
        OverrideIndicPositionalCategory(universalCodePoints);

        for (int i = universalCodePoints.Count - 1; i >= 0; i--)
        {
            ScriptClass script = (ScriptClass)scriptTrie.Get((uint)universalCodePoints[i].Code);
            if (script is ScriptClass.Arabic or ScriptClass.Lao or ScriptClass.Samaritan or ScriptClass.Syriac or ScriptClass.Thai)
            {
                universalCodePoints.RemoveAt(i);
            }
        }

        foreach (Codepoint codePoint in universalCodePoints)
        {
            if (codePoint.Code is >= 0x0F18 and <= 0x0F19 or >= 0x0F3E and <= 0x0F3F)
            {
                codePoint.IndicSyllabicCategory = ISC.VowelDependent;
            }
            else if (codePoint.Code is >= 0x1CE2 and <= 0x1CE8)
            {
                codePoint.IndicSyllabicCategory = ISC.CantillationMark;
            }
            else if (codePoint.Code == 0x1CED)
            {
                codePoint.IndicSyllabicCategory = ISC.ToneMark;
            }

            if (codePoint.Code is 0x11302 or 0x11303 or 0x114C1)
            {
                codePoint.IndicPositionalCategory = IPC.Top;
            }
        }

        UnicodeTrieBuilder builder = new();
        Dictionary<string, int> symbols = UniversalCategoryNames.Select((name, value) => (name, value)).ToDictionary(x => x.name, x => x.value);
        for (int i = 0; i < universalCodePoints.Count; i++)
        {
            Codepoint codePoint = universalCodePoints[i];
            string? category = GetCategory(codePoint);

            if (category != null)
            {
                builder.Set(codePoint.Code, (uint)symbols[category]);
            }
        }

        UnicodeTrie trie = builder.Freeze();
        GenerateTrieClass("UniversalShaping", trie);

        StateMachine machine = GetStateMachine("use", symbols);

        GenerateDataClass("UniversalShaping", symbols, null, machine);

        return codePoints;
    }

    /// <summary>
    /// Adds every character range explicitly present in a Unicode property file.
    /// </summary>
    /// <param name="codePoints">The destination set.</param>
    /// <param name="fileName">The property file name.</param>
    /// <param name="regex">The row parser for the property file.</param>
    private static void AddCodePointRanges(HashSet<int> codePoints, string fileName, Regex regex)
    {
        using StreamReader reader = GetStreamReader(fileName);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            Match match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            string end = match.Groups[2].Value;
            if (string.IsNullOrEmpty(end))
            {
                end = match.Groups[1].Value;
            }

            int min = ParseHexInt(match.Groups[1].Value);
            int max = ParseHexInt(end);
            for (int codePoint = min; codePoint <= max; codePoint++)
            {
                codePoints.Add(codePoint);
            }
        }
    }

    private static void OverrideIndicSyllabicCategory(List<Codepoint> codePoints)
    {
        Regex regex = UnicodePropertyRowRegex();

        using StreamReader sr = GetStreamReader("IndicSyllabicCategory-Additional.txt");
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                string start = match.Groups[1].Value;
                string end = match.Groups[2].Value;
                string point = match.Groups[3].Value;

                if (string.IsNullOrEmpty(end))
                {
                    end = start;
                }

                if (!UniversalSyllabicCategoryOverrides.TryGetValue(point, out ISC category)
                    && !IndicSyllabicCategoryMap.TryGetValue(point, out category))
                {
                    continue;
                }

                int min = ParseHexInt(start);
                int max = ParseHexInt(end);

                for (int i = min; i <= max; i++)
                {
                    Codepoint codePoint = codePoints.First(x => x.Code == i);
                    codePoint.IndicSyllabicCategory = category;
                }
            }
        }
    }

    private static void OverrideIndicPositionalCategory(List<Codepoint> codePoints)
    {
        Regex regex = UnicodePropertyRowRegex();

        using StreamReader sr = GetStreamReader("IndicPositionalCategory-Additional.txt");
        string? line;
        while ((line = sr.ReadLine()) != null)
        {
            Match match = regex.Match(line);

            if (match.Success)
            {
                string start = match.Groups[1].Value;
                string end = match.Groups[2].Value;
                string point = match.Groups[3].Value;

                if (string.IsNullOrEmpty(end))
                {
                    end = start;
                }

                if (!IndicPositionalCategoryMap.TryGetValue(point, out IPC category))
                {
                    continue;
                }

                int min = ParseHexInt(start);
                int max = ParseHexInt(end);

                for (int i = min; i <= max; i++)
                {
                    Codepoint codePoint = codePoints.First(x => x.Code == i);
                    codePoint.IndicPositionalCategory = category;
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a code point is a base.
    /// </summary>
    private static bool IsBase(Codepoint codepoint)
        => codepoint.IndicSyllabicCategory is ISC.Number
            or ISC.Consonant
            or ISC.ConsonantHeadLetter
            or ISC.ToneLetter
            or ISC.VowelIndependent
        || (codepoint.ArabicJoiningType is ArabicJoiningType.JoinCausing
            or ArabicJoiningType.DualJoining
            or ArabicJoiningType.LeftJoining
            or ArabicJoiningType.RightJoining
            && codepoint.IndicSyllabicCategory != ISC.Joiner)
        || (codepoint.Category == GC.OtherLetter
            && codepoint.IndicSyllabicCategory is ISC.Avagraha
                or ISC.Bindu
                or ISC.ConsonantFinal
                or ISC.ConsonantMedial
                or ISC.ConsonantSubjoined
                or ISC.Vowel
                or ISC.VowelDependent);

    /// <summary>
    /// Generates the supplementary data for the shaper.
    /// </summary>
    /// <param name="name">The name of the class.</param>
    /// <param name="symbols">The symbols data.</param>
    /// <param name="decompositions">The decompositions data.</param>
    /// <param name="machine">The state machine.</param>
    /// <param name="partial">Whether the generated class is partial.</param>
    private static void GenerateDataClass(
        string name,
        Dictionary<string, int>? symbols,
        Dictionary<int, List<int>>? decompositions,
        StateMachine machine,
        bool partial = false)
    {
        using FileStream fileStream = GetStreamWriter($"{name}Data.Generated.cs");
        using StreamWriter writer = new(fileStream);

        string partialKeyword = partial ? " partial " : " ";

        writer.WriteLine("// Copyright (c) Six Labors.");
        writer.WriteLine("// Licensed under the Six Labors Split License.");
        writer.WriteLine();
        writer.WriteLine("// <auto-generated />");
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine();
        writer.WriteLine("namespace SixLabors.Fonts.Unicode.Resources");
        writer.WriteLine("{");
        writer.WriteLine($"    internal static{partialKeyword}class {name}Data");
        writer.WriteLine("    {");

        int counter = 0;
        int max = 0;

        if (symbols != null)
        {
            // Write the categories.
            writer.WriteLine("        public static string[] Categories => new string[]");
            writer.WriteLine("        {");

            max = symbols.Count - 1;
            foreach (KeyValuePair<string, int> item in symbols)
            {
                writer.Write($"            \"{item.Key}\"");
                if (counter != max)
                {
                    writer.Write(",");
                }

                counter++;
                writer.Write(Environment.NewLine);
            }

            writer.WriteLine("        };");
            writer.Write(Environment.NewLine);

            // Emit the reordering lookup in symbol order so the shaper can index it
            // directly without building a second table when the type initializes.
            writer.WriteLine("        public static ReadOnlySpan<bool> PostBaseCategories => new bool[]");
            writer.WriteLine("        {");

            counter = 0;
            foreach (KeyValuePair<string, int> item in symbols)
            {
                writer.Write($"            {UniversalPostBaseCategoryNames.Contains(item.Key).ToString().ToLowerInvariant()}");
                if (counter != max)
                {
                    writer.Write(",");
                }

                counter++;
                writer.Write(Environment.NewLine);
            }

            writer.WriteLine("        };");
            writer.Write(Environment.NewLine);
        }

        // Write the decompositions
        if (decompositions != null)
        {
            writer.WriteLine("        public static Dictionary<int, int[]> Decompositions { get; } = new()");
            writer.WriteLine("        {");

            counter = 0;
            max = decompositions.Count - 1;
            foreach (KeyValuePair<int, List<int>> item in decompositions)
            {
                writer.Write($"            {{ 0x{item.Key:X}, new int[] {{ {string.Join(',', item.Value.Select(x => "0x" + x.ToString("X", CultureInfo.InvariantCulture)))} }} }}");
                if (counter != max)
                {
                    writer.Write(",");
                }

                counter++;
                writer.Write(Environment.NewLine);
            }

            writer.WriteLine("        };");
            writer.Write(Environment.NewLine);
        }

        // Writes the state machine state table.
        writer.WriteLine($"        public static int[][] StateTable => new int[{machine.StateTable.Length}][]");
        writer.WriteLine("        {");

        counter = 0;
        max = machine.StateTable.Length - 1;
        foreach (int[] item in machine.StateTable)
        {
            writer.Write($"            new int[] {{ {string.Join(',', item.Select(x => x))} }}");
            if (counter != max)
            {
                writer.Write(",");
            }

            counter++;
            writer.Write(Environment.NewLine);
        }

        writer.WriteLine("        };");

        // Writes the state machine accepting states.
        writer.Write(Environment.NewLine);
        writer.WriteLine("        public static bool[] AcceptingStates => new bool[]");
        writer.WriteLine("        {");

        counter = 0;
        max = machine.Accepting.Length - 1;
        foreach (bool item in machine.Accepting)
        {
            writer.Write($"            {item}".ToLowerInvariant());
            if (counter != max)
            {
                writer.Write(",");
            }

            counter++;
            writer.Write(Environment.NewLine);
        }

        writer.WriteLine("        };");

        // Writes the state machine tags.
        writer.Write(Environment.NewLine);
        writer.WriteLine($"        public static string[][] Tags => new string[{machine.Tags.Length}][]");
        writer.WriteLine("        {");

        counter = 0;
        max = machine.Tags.Length - 1;
        foreach (ICollection<string> item in machine.Tags)
        {
            if (item.Count == 0)
            {
                writer.Write("            Array.Empty<string>()");
            }
            else
            {
                writer.Write($"            new string[] {{ {string.Join(',', item.Select(x => $"\"{x}\""))} }}");
            }

            if (counter != max)
            {
                writer.Write(",");
            }

            counter++;
            writer.Write(Environment.NewLine);
        }

        writer.WriteLine("        };");

        writer.WriteLine("    }");
        writer.WriteLine("}");
    }

    private static StateMachine GetStateMachine(string name, Dictionary<string, int> symbols)
    {
        using StreamReader sr = GetStreamReader($"{name}.machine");
        string machine = sr.ReadToEnd();
        return Compile.Build(machine, symbols);
    }

    private class Codepoint
    {
        public int Code { get; set; }

        public ISC IndicSyllabicCategory { get; set; }

        public IPC IndicPositionalCategory { get; set; }

        public ArabicJoiningType ArabicJoiningType { get; set; }

        public ArabicJoiningGroup ArabicJoiningGroup { get; set; }

        public GC Category { get; set; }

        public bool DefaultIgnorable { get; set; }

        public string Block { get; set; } = "No_Block";
    }
}
