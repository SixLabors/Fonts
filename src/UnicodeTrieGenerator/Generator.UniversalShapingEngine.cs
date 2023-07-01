// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Unicode;
using GC = System.Globalization.UnicodeCategory;
using IPC = SixLabors.Fonts.Unicode.IndicPositionalCategory;
using ISC = SixLabors.Fonts.Unicode.IndicSyllabicCategory;

namespace UnicodeTrieGenerator
{
    /// <content>
    /// Contains code to generate a trie for storing Universal Shaping Data categories.
    /// </content>
    public static partial class Generator
    {
        private static readonly Dictionary<string, List<object>> Categories = new()
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

        private static Dictionary<string, Dictionary<string, List<IPC>>> USEPOSITIONS = new()
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

        private static Dictionary<int, ISC> UISC_OVERRIDE { get; } = new()
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

        public static Dictionary<int, IPC> UIPC_OVERRIDE { get; } = new()
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
            // TODO: proper type inference
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
                        else
                        {
                            var ass = 0;
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

            if (value == pattern)
            {
                var t = 1;
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

            foreach (string key in pattern.Keys)
            {
                if (!Check(pattern[key], GetCodeValue(code, key)))
                {
                    return false;
                }
            }

            return true;
        }

        private static ISC GetUISC(Codepoint code)
            => UISC_OVERRIDE.ContainsKey(code.Code) ? UISC_OVERRIDE[code.Code] : code.IndicSyllabicCategory;

        private static IPC GetUIPC(Codepoint code)
            => UIPC_OVERRIDE.ContainsKey(code.Code) ? UIPC_OVERRIDE[code.Code] : code.IndicPositionalCategory;

        private static string GetPositionalCategory(Codepoint code, string uSE)
        {
            IPC uIPC = GetUIPC(code);
            if (USEPOSITIONS.ContainsKey(uSE))
            {
                Dictionary<string, List<IPC>> pos = USEPOSITIONS[uSE];
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

        private static object? GetCodeValue(Codepoint code, string key)
            => key switch
            {
                "UISC" => GetUISC(code),
                "UGC" => code.Category,
                "U" => code.Code,
                _ => null,
            };

        private static string? GetCategory(Codepoint code)
        {
            foreach (string category in Categories.Keys)
            {
                foreach (object pattern in Categories[category])
                {
                    if (Matches(pattern, code))
                    {
                        return GetPositionalCategory(code, category);
                    }
                }
            }

            return null;
        }

        private static void GenerateUniversalShapingDataTrie(
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

                // TODO: Override these properties using MS shaping.
                codepoint.IndicSyllabicCategory = (ISC)indicSyllabicCategoryTrie.Get((uint)codepoint.Code);
                codepoint.IndicPositionalCategory = (IPC)indicPositionalCategoryTrie.Get((uint)codepoint.Code);

                uint value = arabicJoiningTrie.Get((uint)codepoint.Code);
                codepoint.ArabicJoiningType = GetJoiningType(codepoint.Code, value, codepoint.Category);
                codepoint.ArabicJoiningGroup = (ArabicJoiningGroup)((value >> 16) & 0xFF);

                codePoints.Add(codepoint);
            }

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

        private class Codepoint
        {
            public int Code { get; set; }

            public ISC IndicSyllabicCategory { get; set; }

            public IPC IndicPositionalCategory { get; set; }

            public ArabicJoiningType ArabicJoiningType { get; set; }

            public ArabicJoiningGroup ArabicJoiningGroup { get; set; }

            public IEnumerable<int> Decomposition { get; set; } = Array.Empty<int>();

            public GC Category { get; set; }
        }
    }
}
