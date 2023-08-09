// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.Fonts.Unicode;
using UnicodeTrieGenerator.StateAutomation;
using GC = System.Globalization.UnicodeCategory;
using IPC = SixLabors.Fonts.Unicode.IndicPositionalCategory;
using ISC = SixLabors.Fonts.Unicode.IndicSyllabicCategory;

namespace UnicodeTrieGenerator
{
    /// <content>
    /// Contains code to generate a trie and state machine for storing Universal Shaping Data category data.
    /// </content>
    public static partial class Generator
    {
        private static readonly Dictionary<string, List<object>> UniversalCategories = new()
        {
            {
                "B", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.Number } },
                    new Dictionary<string, object> { { "UISC", ISC.Avagraha }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.Bindu }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.Consonant } },
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantFinal }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantHeadLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantMedial }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantSubjoined }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.ToneLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.Vowel }, { "UGC", GC.OtherLetter } },
                    new Dictionary<string, object> { { "UISC", ISC.VowelIndependent } },
                    new Dictionary<string, object> { { "UISC", ISC.VowelDependent }, { "UGC", GC.OtherLetter } }
                }
            },
            {
                "CGJ", new List<object> { 0x034f }
            },
            {
                "CM", new List<object>
                {
                    ISC.Nukta,
                    ISC.GeminationMark,
                    ISC.ConsonantKiller
                }
            },
            { "CS", new List<object> { ISC.ConsonantWithStacker } },
            {
                "F", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantFinal }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } },
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantSucceedingRepha } }
                }
            },
            { "FM", new List<object> { ISC.SyllableModifier } },
            {
                "GB", new List<object>
                {
                    ISC.ConsonantPlaceholder,
                    0x2015,
                    0x2022,
                    0x25fb,
                    0x25fc,
                    0x25fd,
                    0x25fe
                }
            },
            {
                "H", new List<object>
                {
                    ISC.Virama,
                    ISC.InvisibleStacker
                }
            },
            { "HN", new List<object> { ISC.NumberJoiner } },
            {
                "IND", new List<object>
                {
                    ISC.ConsonantDead,
                    ISC.ModifyingLetter,
                    new Dictionary<string, object> { { "UGC", GC.OtherPunctuation }, { "U", new Dictionary<string, object> { { "not", new List<object> { 0x104e, 0x2022 } } } } }
                }
            },
            {
                "M", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantMedial }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } },
                    ISC.ConsonantInitialPostfixed
                }
            },
            { "N", new List<object> { ISC.BrahmiJoiningNumber } },
            {
                "R", new List<object>
                {
                    ISC.ConsonantPrecedingRepha,
                    ISC.ConsonantPrefixed
                }
            },
            { "Rsv", new List<object> { new Dictionary<string, object> { { "UGC", GC.OtherNotAssigned } } } },
            {
                "S", new List<object>
                {
                    new Dictionary<string, object> { { "UGC", GC.OtherSymbol }, { "U", new Dictionary<string, object> { { "not", 0x25cc } } } },
                    new Dictionary<string, object> { { "UGC", GC.CurrencySymbol } }
                }
            },
            {
                "SM", new List<object>
                {
                    0x1b6b,
                    0x1b6c,
                    0x1b6d,
                    0x1b6e,
                    0x1b6f,
                    0x1b70,
                    0x1b71,
                    0x1b72,
                    0x1b73
                }
            },
            {
                "SUB", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.ConsonantSubjoined }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } }
                }
            },
            {
                "V", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.Vowel }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } },
                    new Dictionary<string, object> { { "UISC", ISC.VowelDependent }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } },
                    new Dictionary<string, object> { { "UISC", ISC.PureKiller } }
                }
            },
            {
                "VM", new List<object>
                {
                    new Dictionary<string, object> { { "UISC", ISC.Bindu }, { "UGC", new Dictionary<string, object> { { "not", GC.OtherLetter } } } },
                    ISC.ToneMark,
                    ISC.CantillationMark,
                    ISC.RegisterShifter,
                    ISC.Visarga
                }
            },
            {
                "VS", new List<object>
                {
                    0xfe00, 0xfe01, 0xfe02, 0xfe03, 0xfe04, 0xfe05, 0xfe06, 0xfe07,
                    0xfe08, 0xfe09, 0xfe0a, 0xfe0b, 0xfe0c, 0xfe0d, 0xfe0e, 0xfe0f
                }
            },
            { "WJ", new List<object> { 0x2060 } },
            { "ZWJ", new List<object> { ISC.Joiner } },
            { "ZWNJ", new List<object> { ISC.NonJoiner } },
            { "O", new List<object> { ISC.Other } }
        };

        private static readonly Dictionary<string, Dictionary<string, List<IPC>>> UniversalPositions = new()
        {
            {
                "F",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom }
                    },
                    {
                        "Pst", new List<IPC>() { IPC.Right }
                    }
                }
            },
            {
                "M",
                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom, IPC.BottomAndLeft, IPC.BottomAndRight }
                    },
                    {
                        "Pst", new List<IPC>() { IPC.Right }
                    },
                    {
                        "Pre", new List<IPC>() { IPC.Left, IPC.TopAndBottomAndLeft }
                    }
                }
            },
            {
                "CM",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom, IPC.Overstruck }
                    }
                }
            },
            {
                "V",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top, IPC.TopAndBottom, IPC.TopAndBottomAndRight, IPC.TopAndRight }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom, IPC.Overstruck, IPC.BottomAndRight }
                    },
                    {
                        "Pst", new List<IPC>() { IPC.Right }
                    },
                    {
                        "Pre", new List<IPC>() { IPC.Left, IPC.TopAndLeft, IPC.TopAndLeftAndRight, IPC.LeftAndRight }
                    }
                }
            },
            {
                "VM",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom, IPC.Overstruck }
                    },
                    {
                        "Pst", new List<IPC>() { IPC.Right }
                    },
                    {
                        "Pre", new List<IPC>() { IPC.Left }
                    }
                }
            },
            {
                "SM",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom }
                    }
                }
            },
            {
                "FM",

                new Dictionary<string, List<IPC>>()
                {
                    {
                        "Abv", new List<IPC>() { IPC.Top }
                    },
                    {
                        "Blw", new List<IPC>() { IPC.Bottom }
                    },
                    {
                        "Pst", new List<IPC>() { IPC.NA }
                    }
                }
            }
        };

        private static readonly Dictionary<int, ISC> SyllabicOverrides = new()
        {
            { 0x17dd, ISC.VowelDependent },
            { 0x1ce2, ISC.CantillationMark },
            { 0x1ce3, ISC.CantillationMark },
            { 0x1ce4, ISC.CantillationMark },
            { 0x1ce5, ISC.CantillationMark },
            { 0x1ce6, ISC.CantillationMark },
            { 0x1ce7, ISC.CantillationMark },
            { 0x1ce8, ISC.CantillationMark },
            { 0x1ced, ISC.ToneMark }
        };

        private static readonly Dictionary<int, IPC> PositionalOverrides = new()
        {
            { 0x1b6c, IPC.Bottom },
            { 0x953, IPC.NA },
            { 0x954, IPC.NA },
            { 0x103c, IPC.Left },
            { 0xa926, IPC.Top },
            { 0xa927, IPC.Top },
            { 0xa928, IPC.Top },
            { 0xa929, IPC.Top },
            { 0xa92a, IPC.Top },
            { 0x111ca, IPC.Bottom },
            { 0x11300, IPC.Top },
            { 0x1133c, IPC.Bottom },
            { 0x1171e, IPC.Left },
            { 0x1cf2, IPC.Right },
            { 0x1cf3, IPC.Right },
            { 0x1cf8, IPC.Top },
            { 0x1cf9, IPC.Top }
        };

        private static bool Check(dynamic pattern, dynamic value)
        {
            // TODO:This is nasty. Port to use the harfbuzz approach using Function<CodePoint, bool>
            if (pattern is Dictionary<string, object> dictionary && dictionary.ContainsKey("not"))
            {
                object not = dictionary["not"];
                if (not is List<object> list)
                {
                    foreach (object item in list)
                    {
                        if (value is int i && item is int i2)
                        {
                            if (i == i2)
                            {
                                return false;
                            }
                        }

                        if (value is GC gc && item is GC gc2)
                        {
                            if (gc == gc2)
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }
                else
                {
                    if (value is int i && not is int i2)
                    {
                        return i != i2;
                    }

                    if (value is GC gc && not is GC gc2)
                    {
                        return gc != gc2;
                    }
                }
            }

            return value == pattern;
        }

        private static bool Matches(dynamic pattern, Codepoint code)
        {
            if (pattern is int i)
            {
                pattern = new Dictionary<string, int> { { "U", i } };
            }
            else if (pattern is ISC isc)
            {
                pattern = new Dictionary<string, dynamic> { { "UISC", isc } };
            }

            foreach (string? key in pattern.Keys)
            {
                if (!Check(pattern[key], GetCodeValue(code, key)))
                {
                    return false;
                }
            }

            return true;
        }

        private static ISC GetUISC(Codepoint code)
            => SyllabicOverrides.ContainsKey(code.Code) ? SyllabicOverrides[code.Code] : code.IndicSyllabicCategory;

        private static IPC GetUIPC(Codepoint code)
            => PositionalOverrides.ContainsKey(code.Code) ? PositionalOverrides[code.Code] : code.IndicPositionalCategory;

        private static string GetPositionalCategory(Codepoint code, string uSE)
        {
            IPC uIPC = GetUIPC(code);
            if (UniversalPositions.ContainsKey(uSE))
            {
                Dictionary<string, List<IPC>> pos = UniversalPositions[uSE];
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

        private static object? GetCodeValue(Codepoint code, string? key)
            => key switch
            {
                "UISC" => GetUISC(code),
                "UGC" => code.Category,
                "U" => code.Code,
                _ => null,
            };

        private static string? GetCategory(Codepoint code)
        {
            foreach (string category in UniversalCategories.Keys)
            {
                foreach (object pattern in UniversalCategories[category])
                {
                    if (Matches(pattern, code))
                    {
                        return GetPositionalCategory(code, category);
                    }
                }
            }

            return null;
        }

        private static List<Codepoint> GenerateUniversalShapingDataTrie(
                    UnicodeTrie unicodeGeneralCategory,
                    UnicodeTrie indicSyllabicCategoryTrie,
                    UnicodeTrie indicPositionalCategoryTrie,
                    UnicodeTrie arabicJoiningTrie)
        {
            static IEnumerable<int>? ParseCodes(string code)
            {
                if (string.IsNullOrEmpty(code) || code.StartsWith('<'))
                {
                    return null;
                }

                return Array.ConvertAll(code.Split(' '), ParseHexInt);
            }

            static ArabicJoiningType GetJoiningType(int codePoint, uint value, GC category)
            {
                var type = (ArabicJoiningType)(value & 0xFF);

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

            List<Codepoint> codePoints = new();
            using StreamReader sr = GetStreamReader("UnicodeData.txt");
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split(';');

                Codepoint codepoint = new()
                {
                    Code = ParseHexInt(parts[0])
                };

                // See https://www.unicode.org/reports/tr44 for mapping of fields
                if (parts.Length > 4)
                {
                    codepoint.Decomposition = ParseCodes(parts[5]) ?? Array.Empty<int>();
                }

                codepoint.Category = (GC)unicodeGeneralCategory.Get((uint)codepoint.Code);

                codepoint.IndicSyllabicCategory = (ISC)indicSyllabicCategoryTrie.Get((uint)codepoint.Code);
                codepoint.IndicPositionalCategory = (IPC)indicPositionalCategoryTrie.Get((uint)codepoint.Code);

                uint value = arabicJoiningTrie.Get((uint)codepoint.Code);
                codepoint.ArabicJoiningType = GetJoiningType(codepoint.Code, value, codepoint.Category);
                codepoint.ArabicJoiningGroup = (ArabicJoiningGroup)((value >> 16) & 0xFF);

                codePoints.Add(codepoint);
            }

            // TODO: Override these properties using MS shaping. It's likely we just need to implement the harfbuzz
            // category matching using Func<string, bool>
            // OverrideIndicSyllabicCategory(codePoints);
            // OverrideIndicPositionalCategory(codePoints);
            UnicodeTrieBuilder builder = new();
            Dictionary<string, int> symbols = new();
            int numSymbols = 0;
            Dictionary<int, List<int>> decompositions = new();
            for (int i = 0; i < codePoints.Count; i++)
            {
                Codepoint codePoint = codePoints[i];
                string? category = GetCategory(codePoint);

                if (category != null)
                {
                    if (!symbols.ContainsKey(category))
                    {
                        symbols[category] = numSymbols++;
                    }

                    builder.Set(codePoint.Code, (uint)symbols[category]);
                }

                if (codePoint.IndicSyllabicCategory == ISC.VowelDependent && codePoint.Decomposition.Any())
                {
                    decompositions[codePoint.Code] = Decompose(codePoint.Code, codePoints);
                }
            }

            UnicodeTrie trie = builder.Freeze();
            GenerateTrieClass("UniversalShaping", trie);

            StateMachine machine = GetStateMachine("use", symbols);

            GenerateDataClass("UniversalShaping", symbols, decompositions, machine);

            return codePoints;
        }

        private static void OverrideIndicSyllabicCategory(List<Codepoint> codePoints)
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");

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

                    if (!IndicSyllabicCategoryMap.TryGetValue(point, out ISC category))
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
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");

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

        private static List<int> Decompose(int code, List<Codepoint> codepoints)
        {
            List<int> decomposition = new();
            Codepoint codePoint = codepoints.First(x => x.Code == code);
            foreach (int c in codePoint.Decomposition)
            {
                List<int> codes = Decompose(c, codepoints);
                codes = codes.Count > 0 ? codes : new List<int> { c };
                decomposition.AddRange(codes);
            }

            return decomposition;
        }

        private static bool IsBase(Codepoint codepoint)
            => new List<ISC>
                {
                    ISC.Number,
                    ISC.Consonant,
                    ISC.ConsonantHeadLetter,
                    ISC.ToneLetter,
                    ISC.VowelIndependent
                }
                .Contains(codepoint.IndicSyllabicCategory)
            || (new List<ArabicJoiningType>
                {
                    ArabicJoiningType.JoinCausing,
                    ArabicJoiningType.DualJoining,
                    ArabicJoiningType.LeftJoining,
                    ArabicJoiningType.RightJoining
                }.Contains(codepoint.ArabicJoiningType)
            && codepoint.IndicSyllabicCategory != ISC.Joiner)
               || (codepoint.Category == GC.OtherLetter
               && new List<ISC>
                   {
                       ISC.Avagraha,
                       ISC.Bindu,
                       ISC.ConsonantFinal,
                       ISC.ConsonantMedial,
                       ISC.ConsonantSubjoined,
                       ISC.Vowel,
                       ISC.VowelDependent
                   }
                   .Contains(codepoint.IndicSyllabicCategory));

        /// <summary>
        /// Generates the supplementary data for the shaper.
        /// </summary>
        /// <param name="name">The name of the class.</param>
        /// <param name="symbols">The symbols data.</param>
        /// <param name="decompositions">The decompositions data.</param>
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
            writer.WriteLine("// Licensed under the Apache License, Version 2.0.");
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
            }

            // Write the decompositions
            if (decompositions != null)
            {
                writer.WriteLine("        public static Dictionary<int, int[]> Decompositions => new()");
                writer.WriteLine("        {");

                counter = 0;
                max = decompositions.Count - 1;
                foreach (KeyValuePair<int, List<int>> item in decompositions)
                {
                    writer.Write($"            {{ 0x{item.Key:X}, new int[] {{ {string.Join(',', item.Value.Select(x => "0x" + x.ToString("X")))} }} }}");
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
                    writer.Write($"            Array.Empty<string>()");
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

            public IEnumerable<int> Decomposition { get; set; } = Array.Empty<int>();

            public GC Category { get; set; }

            public string Block { get; set; } = "No_Block";
        }
    }
}
