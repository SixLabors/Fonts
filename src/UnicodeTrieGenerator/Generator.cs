// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SixLabors.Fonts.Tests.Unicode;
using SixLabors.Fonts.Unicode;

namespace UnicodeTrieGenerator
{
    /// <summary>
    /// Provides methods to generate Unicode Tries.
    /// Ported from <see href="https://github.com/toptensoftware/RichTextKit/blob/master/BuildUnicodeData/generate.js"/>.
    /// </summary>
    public static class Generator
    {
        private static readonly Dictionary<string, UnicodeCategory> UnicodeCategoryMap
            = new Dictionary<string, UnicodeCategory>(StringComparer.OrdinalIgnoreCase)
        {
            { "Lu", UnicodeCategory.UppercaseLetter },
            { "Ll", UnicodeCategory.LowercaseLetter },
            { "Lt", UnicodeCategory.TitlecaseLetter },
            { "Lm", UnicodeCategory.ModifierLetter },
            { "Lo", UnicodeCategory.OtherLetter },
            { "Mn", UnicodeCategory.NonSpacingMark },
            { "Mc", UnicodeCategory.SpacingCombiningMark },
            { "Me", UnicodeCategory.EnclosingMark },
            { "Nd", UnicodeCategory.DecimalDigitNumber },
            { "Nl", UnicodeCategory.LetterNumber },
            { "No", UnicodeCategory.OtherNumber },
            { "Zs", UnicodeCategory.SpaceSeparator },
            { "Zl", UnicodeCategory.LineSeparator },
            { "Zp", UnicodeCategory.ParagraphSeparator },
            { "Cc", UnicodeCategory.Control },
            { "Cf", UnicodeCategory.Format },
            { "Cs", UnicodeCategory.Surrogate },
            { "Co", UnicodeCategory.PrivateUse },
            { "Pc", UnicodeCategory.ConnectorPunctuation },
            { "Pd", UnicodeCategory.DashPunctuation },
            { "Ps", UnicodeCategory.OpenPunctuation },
            { "Pe", UnicodeCategory.ClosePunctuation },
            { "Pi", UnicodeCategory.InitialQuotePunctuation },
            { "Pf", UnicodeCategory.FinalQuotePunctuation },
            { "Po", UnicodeCategory.OtherPunctuation },
            { "Sm", UnicodeCategory.MathSymbol },
            { "Sc", UnicodeCategory.CurrencySymbol },
            { "Sk", UnicodeCategory.ModifierSymbol },
            { "So", UnicodeCategory.OtherSymbol },
            { "Cn", UnicodeCategory.OtherNotAssigned }
        };

        private static readonly Dictionary<string, BidiPairedBracketType> BidiPairedBracketTypeMap
            = new Dictionary<string, BidiPairedBracketType>(StringComparer.OrdinalIgnoreCase)
        {
            { "N", BidiPairedBracketType.None },
            { "O", BidiPairedBracketType.Open },
            { "C", BidiPairedBracketType.Close }
        };

        // Aliases and enum derived from
        // https://www.unicode.org/Public/13.0.0/ucd/PropertyValueAliases.txt
        private static readonly Dictionary<string, GraphemeClusterClass> GraphemeClusterClassMap
            = new Dictionary<string, GraphemeClusterClass>(StringComparer.OrdinalIgnoreCase)
            {
                { "Any", GraphemeClusterClass.Any },
                { "CR", GraphemeClusterClass.CarriageReturn },
                { "LF", GraphemeClusterClass.LineFeed },
                { "Control", GraphemeClusterClass.Control },
                { "Extend", GraphemeClusterClass.Extend },
                { "Regional_Indicator", GraphemeClusterClass.RegionalIndicator },
                { "Prepend", GraphemeClusterClass.Prepend },
                { "SpacingMark", GraphemeClusterClass.SpacingMark },
                { "L", GraphemeClusterClass.HangulLead },
                { "V", GraphemeClusterClass.HangulVowel },
                { "T", GraphemeClusterClass.HangulTail },
                { "LV", GraphemeClusterClass.HangulLeadVowel },
                { "LVT", GraphemeClusterClass.HangulLeadVowelTail },
                { "ExtPict", GraphemeClusterClass.ExtendedPictographic },
                { "ZWJ", GraphemeClusterClass.ZeroWidthJoiner }
            };

        private static readonly Dictionary<string, Script> ScriptMap
            = new Dictionary<string, Script>(StringComparer.OrdinalIgnoreCase)
        {
                { "Unknown", Script.Unknown },
                { "Common", Script.Common },
                { "Inherited", Script.Inherited },
                { "Adlam", Script.Adlam },
                { "Caucasian_Albanian", Script.CaucasianAlbanian },
                { "Ahom", Script.Ahom },
                { "Arabic", Script.Arabic },
                { "Imperial_Aramaic", Script.ImperialAramaic },
                { "Armenian", Script.Armenian },
                { "Avestan", Script.Avestan },
                { "Balinese", Script.Balinese },
                { "Bamum", Script.Bamum },
                { "Bassa_Vah", Script.BassaVah },
                { "Batak", Script.Batak },
                { "Bengali", Script.Bengali },
                { "Bhaiksuki", Script.Bhaiksuki },
                { "Bopomofo", Script.Bopomofo },
                { "Brahmi", Script.Brahmi },
                { "Braille", Script.Braille },
                { "Buginese", Script.Buginese },
                { "Buhid", Script.Buhid },
                { "Chakma", Script.Chakma },
                { "Canadian_Aboriginal", Script.CanadianAboriginal },
                { "Carian", Script.Carian },
                { "Cham", Script.Cham },
                { "Cherokee", Script.Cherokee },
                { "Chorasmian", Script.Chorasmian },
                { "Coptic", Script.Coptic },
                { "Cypriot", Script.Cypriot },
                { "Cyrillic", Script.Cyrillic },
                { "Devanagari", Script.Devanagari },
                { "Dives_Akuru", Script.DivesAkuru },
                { "Dogra", Script.Dogra },
                { "Deseret", Script.Deseret },
                { "Duployan", Script.Duployan },
                { "Egyptian_Hieroglyphs", Script.EgyptianHieroglyphs },
                { "Elbasan", Script.Elbasan },
                { "Elymaic", Script.Elymaic },
                { "Ethiopic", Script.Ethiopic },
                { "Georgian", Script.Georgian },
                { "Glagolitic", Script.Glagolitic },
                { "Gunjala_Gondi", Script.GunjalaGondi },
                { "Masaram_Gondi", Script.MasaramGondi },
                { "Gothic", Script.Gothic },
                { "Grantha", Script.Grantha },
                { "Greek", Script.Greek },
                { "Gujarati", Script.Gujarati },
                { "Gurmukhi", Script.Gurmukhi },
                { "Hangul", Script.Hangul },
                { "Han", Script.Han },
                { "Hanunoo", Script.Hanunoo },
                { "Hatran", Script.Hatran },
                { "Hebrew", Script.Hebrew },
                { "Hiragana", Script.Hiragana },
                { "Anatolian_Hieroglyphs", Script.AnatolianHieroglyphs },
                { "Pahawh_Hmong", Script.PahawhHmong },
                { "Nyiakeng_Puachue_Hmong", Script.NyiakengPuachueHmong },
                { "Katakana_Or_Hiragana", Script.KatakanaOrHiragana },
                { "Old_Hungarian", Script.OldHungarian },
                { "Old_Italic", Script.OldItalic },
                { "Javanese", Script.Javanese },
                { "Kayah_Li", Script.KayahLi },
                { "Katakana", Script.Katakana },
                { "Kharoshthi", Script.Kharoshthi },
                { "Khmer", Script.Khmer },
                { "Khojki", Script.Khojki },
                { "Khitan_Small_Script", Script.KhitanSmallScript },
                { "Kannada", Script.Kannada },
                { "Kaithi", Script.Kaithi },
                { "Tai_Tham", Script.TaiTham },
                { "Lao", Script.Lao },
                { "Latin", Script.Latin },
                { "Lepcha", Script.Lepcha },
                { "Limbu", Script.Limbu },
                { "Linear_A", Script.LinearA },
                { "Linear_B", Script.LinearB },
                { "Lisu", Script.Lisu },
                { "Lycian", Script.Lycian },
                { "Lydian", Script.Lydian },
                { "Mahajani", Script.Mahajani },
                { "Makasar", Script.Makasar },
                { "Mandaic", Script.Mandaic },
                { "Manichaean", Script.Manichaean },
                { "Marchen", Script.Marchen },
                { "Medefaidrin", Script.Medefaidrin },
                { "Mende_Kikakui", Script.MendeKikakui },
                { "Meroitic_Cursive", Script.MeroiticCursive },
                { "Meroitic_Hieroglyphs", Script.MeroiticHieroglyphs },
                { "Malayalam", Script.Malayalam },
                { "Modi", Script.Modi },
                { "Mongolian", Script.Mongolian },
                { "Mro", Script.Mro },
                { "Meetei_Mayek", Script.MeeteiMayek },
                { "Multani", Script.Multani },
                { "Myanmar", Script.Myanmar },
                { "Nandinagari", Script.Nandinagari },
                { "Old_North_Arabian", Script.OldNorthArabian },
                { "Nabataean", Script.Nabataean },
                { "Newa", Script.Newa },
                { "Nko", Script.Nko },
                { "Nushu", Script.Nushu },
                { "Ogham", Script.Ogham },
                { "Ol_Chiki", Script.OlChiki },
                { "Old_Turkic", Script.OldTurkic },
                { "Oriya", Script.Oriya },
                { "Osage", Script.Osage },
                { "Osmanya", Script.Osmanya },
                { "Palmyrene", Script.Palmyrene },
                { "Pau_Cin_Hau", Script.PauCinHau },
                { "Old_Permic", Script.OldPermic },
                { "Phags_Pa", Script.PhagsPa },
                { "Inscriptional_Pahlavi", Script.InscriptionalPahlavi },
                { "Psalter_Pahlavi", Script.PsalterPahlavi },
                { "Phoenician", Script.Phoenician },
                { "Miao", Script.Miao },
                { "Inscriptional_Parthian", Script.InscriptionalParthian },
                { "Rejang", Script.Rejang },
                { "Hanifi_Rohingya", Script.HanifiRohingya },
                { "Runic", Script.Runic },
                { "Samaritan", Script.Samaritan },
                { "Old_South_Arabian", Script.OldSouthArabian },
                { "Saurashtra", Script.Saurashtra },
                { "SignWriting", Script.SignWriting },
                { "Shavian", Script.Shavian },
                { "Sharada", Script.Sharada },
                { "Siddham", Script.Siddham },
                { "Khudawadi", Script.Khudawadi },
                { "Sinhala", Script.Sinhala },
                { "Sogdian", Script.Sogdian },
                { "Old_Sogdian", Script.OldSogdian },
                { "Sora_Sompeng", Script.SoraSompeng },
                { "Soyombo", Script.Soyombo },
                { "Sundanese", Script.Sundanese },
                { "Syloti_Nagri", Script.SylotiNagri },
                { "Syriac", Script.Syriac },
                { "Tagbanwa", Script.Tagbanwa },
                { "Takri", Script.Takri },
                { "Tai_Le", Script.TaiLe },
                { "New_Tai_Lue", Script.NewTaiLue },
                { "Tamil", Script.Tamil },
                { "Tangut", Script.Tangut },
                { "Tai_Viet", Script.TaiViet },
                { "Telugu", Script.Telugu },
                { "Tifinagh", Script.Tifinagh },
                { "Tagalog", Script.Tagalog },
                { "Thaana", Script.Thaana },
                { "Thai", Script.Thai },
                { "Tibetan", Script.Tibetan },
                { "Tirhuta", Script.Tirhuta },
                { "Ugaritic", Script.Ugaritic },
                { "Vai", Script.Vai },
                { "Warang_Citi", Script.WarangCiti },
                { "Wancho", Script.Wancho },
                { "Old_Persian", Script.OldPersian },
                { "Cuneiform", Script.Cuneiform },
                { "Yezidi", Script.Yezidi },
                { "Yi", Script.Yi },
                { "Zanabazar_Square", Script.ZanabazarSquare }
        };

        private const string SixLaborsSolutionFileName = "SixLabors.Fonts.sln";
        private const string InputRulesRelativePath = @"src\UnicodeTrieGenerator\Rules";
        private const string OutputResourcesRelativePath = @"src\SixLabors.Fonts\Unicode\Resources";

        private static readonly Lazy<string> SolutionDirectoryFullPathLazy = new Lazy<string>(GetSolutionDirectoryFullPathImpl);
        private static readonly Dictionary<int, int> Bidi = new Dictionary<int, int>();

        private static string SolutionDirectoryFullPath => SolutionDirectoryFullPathLazy.Value;

        /// <summary>
        /// Generates the various Unicode tries.
        /// </summary>
        public static void GenerateUnicodeTries()
        {
            ProcessUnicodeData();
            GenerateBidiBracketsTrie();
            GenerateLineBreakTrie();
            GenerateUnicodeCategoryTrie();
            GenerateScriptTrie();
            GenerateGraphemeBreakTrie();
        }

        private static void ProcessUnicodeData()
        {
            using StreamReader sr = GetStreamReader("UnicodeData.txt");
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split(';');

                if (parts.Length > 4)
                {
                    // Get the directionality.
                    int codePoint = ParseHexInt(parts[0]);

                    UnicodeTypeMaps.BidiCharacterTypeMap.TryGetValue(parts[4], out BidiCharacterType cls);
                    Bidi[codePoint] = (int)cls << 24;
                }
            }
        }

        /// <summary>
        /// Generates the UnicodeTrie for the Grapheme cluster breaks code points.
        /// </summary>
        public static void GenerateGraphemeBreakTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");
            var builder = new UnicodeTrieBuilder((uint)GraphemeClusterClass.Any);

            using (StreamReader sr = GetStreamReader("GraphemeBreakProperty.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string point = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        GraphemeClusterClassMap.TryGetValue(point, out GraphemeClusterClass kind);
                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)kind, true);
                    }
                }
            }

            using (StreamReader sr = GetStreamReader("emoji-data.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string prop = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        if (prop == "Extended_Pictographic")
                        {
                            builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)GraphemeClusterClass.ExtendedPictographic, true);
                        }
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();

            using FileStream stream = GetStreamWriter("Grapheme.trie");
            trie.Save(stream);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the Bidi Brackets code points.
        /// </summary>
        private static void GenerateBidiBracketsTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+);\s([0-9A-F]+);\s([ocn])");
            var builder = new UnicodeTrieBuilder(0u);

            using (StreamReader sr = GetStreamReader("BidiBrackets.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        int point = ParseHexInt(match.Groups[1].Value);
                        int otherPoint = ParseHexInt(match.Groups[2].Value);

                        BidiPairedBracketTypeMap.TryGetValue(match.Groups[3].Value, out BidiPairedBracketType kind);

                        Bidi[point] |= otherPoint | ((int)kind << 16);
                    }
                }
            }

            foreach (KeyValuePair<int, int> item in Bidi)
            {
                if (item.Value != 0)
                {
                    builder.Set(item.Key, (uint)item.Value);
                }
            }

            UnicodeTrie trie = builder.Freeze();

            using FileStream stream = GetStreamWriter("Bidi.trie");
            trie.Save(stream);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the LineBreak code point ranges.
        /// </summary>
        private static void GenerateLineBreakTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");
            var builder = new UnicodeTrieBuilder((uint)LineBreakClass.XX);

            using (StreamReader sr = GetStreamReader("LineBreak.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string point = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)Enum.Parse<LineBreakClass>(point), true);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();

            using FileStream stream = GetStreamWriter("LineBreak.trie");
            trie.Save(stream);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the general category code point ranges.
        /// </summary>
        private static void GenerateUnicodeCategoryTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");
            var builder = new UnicodeTrieBuilder((uint)UnicodeCategory.OtherNotAssigned);

            using (StreamReader sr = GetStreamReader("DerivedGeneralCategory.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string point = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)UnicodeCategoryMap[point], true);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();

            using FileStream stream = GetStreamWriter("UnicodeCategory.trie");
            trie.Save(stream);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the script code point ranges.
        /// </summary>
        private static void GenerateScriptTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");
            var builder = new UnicodeTrieBuilder((uint)Script.Unknown);

            // TODO: Figure out how to map to shared categories via ScripExtensions.txt
            using (StreamReader sr = GetStreamReader("Scripts.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string point = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)ScriptMap[point], true);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();

            using FileStream stream = GetStreamWriter("Script.trie");
            trie.Save(stream);
        }

        private static StreamReader GetStreamReader(string path)
        {
            string filename = GetFullPath(Path.Combine(InputRulesRelativePath, path));
            return File.OpenText(filename);
        }

        private static FileStream GetStreamWriter(string path)
        {
            string filename = GetFullPath(Path.Combine(OutputResourcesRelativePath, path));
            return File.OpenWrite(filename);
        }

        private static string GetSolutionDirectoryFullPathImpl()
        {
            string assemblyLocation = typeof(Generator).Assembly.Location;

            var assemblyFile = new FileInfo(assemblyLocation);

            DirectoryInfo directory = assemblyFile.Directory;

            while (!directory.EnumerateFiles(SixLaborsSolutionFileName).Any())
            {
                try
                {
                    directory = directory.Parent;
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Unable to find SixLabors solution directory from {assemblyLocation} because of {ex.GetType().Name}!",
                        ex);
                }

                if (directory == null)
                {
                    throw new Exception($"Unable to find SixLabors solution directory from {assemblyLocation}!");
                }
            }

            return directory.FullName;
        }

        private static int ParseHexInt(string value)
            => int.Parse(value, NumberStyles.HexNumber);

        private static string GetFullPath(string relativePath)
            => Path.Combine(SolutionDirectoryFullPath, relativePath).Replace('\\', Path.DirectorySeparatorChar);
    }
}
