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
            = new(StringComparer.OrdinalIgnoreCase)
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
            = new(StringComparer.OrdinalIgnoreCase)
            {
                { "N", BidiPairedBracketType.None },
                { "O", BidiPairedBracketType.Open },
                { "C", BidiPairedBracketType.Close }
            };

        // Aliases and enum derived from
        // https://www.unicode.org/Public/13.0.0/ucd/PropertyValueAliases.txt
        private static readonly Dictionary<string, GraphemeClusterClass> GraphemeClusterClassMap
            = new(StringComparer.OrdinalIgnoreCase)
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

        private static readonly Dictionary<string, ScriptClass> ScriptMap
            = new(StringComparer.OrdinalIgnoreCase)
            {
                { "Unknown", ScriptClass.Unknown },
                { "Common", ScriptClass.Common },
                { "Inherited", ScriptClass.Inherited },
                { "Adlam", ScriptClass.Adlam },
                { "Caucasian_Albanian", ScriptClass.CaucasianAlbanian },
                { "Ahom", ScriptClass.Ahom },
                { "Arabic", ScriptClass.Arabic },
                { "Imperial_Aramaic", ScriptClass.ImperialAramaic },
                { "Armenian", ScriptClass.Armenian },
                { "Avestan", ScriptClass.Avestan },
                { "Balinese", ScriptClass.Balinese },
                { "Bamum", ScriptClass.Bamum },
                { "Bassa_Vah", ScriptClass.BassaVah },
                { "Batak", ScriptClass.Batak },
                { "Bengali", ScriptClass.Bengali },
                { "Bhaiksuki", ScriptClass.Bhaiksuki },
                { "Bopomofo", ScriptClass.Bopomofo },
                { "Brahmi", ScriptClass.Brahmi },
                { "Braille", ScriptClass.Braille },
                { "Buginese", ScriptClass.Buginese },
                { "Buhid", ScriptClass.Buhid },
                { "Chakma", ScriptClass.Chakma },
                { "Canadian_Aboriginal", ScriptClass.CanadianAboriginal },
                { "Carian", ScriptClass.Carian },
                { "Cham", ScriptClass.Cham },
                { "Cherokee", ScriptClass.Cherokee },
                { "Chorasmian", ScriptClass.Chorasmian },
                { "Coptic", ScriptClass.Coptic },
                { "Cypro_Minoan", ScriptClass.CyproMinoan },
                { "Cypriot", ScriptClass.Cypriot },
                { "Cyrillic", ScriptClass.Cyrillic },
                { "Devanagari", ScriptClass.Devanagari },
                { "Dives_Akuru", ScriptClass.DivesAkuru },
                { "Dogra", ScriptClass.Dogra },
                { "Deseret", ScriptClass.Deseret },
                { "Duployan", ScriptClass.Duployan },
                { "Egyptian_Hieroglyphs", ScriptClass.EgyptianHieroglyphs },
                { "Elbasan", ScriptClass.Elbasan },
                { "Elymaic", ScriptClass.Elymaic },
                { "Ethiopic", ScriptClass.Ethiopic },
                { "Georgian", ScriptClass.Georgian },
                { "Glagolitic", ScriptClass.Glagolitic },
                { "Gunjala_Gondi", ScriptClass.GunjalaGondi },
                { "Masaram_Gondi", ScriptClass.MasaramGondi },
                { "Gothic", ScriptClass.Gothic },
                { "Grantha", ScriptClass.Grantha },
                { "Greek", ScriptClass.Greek },
                { "Gujarati", ScriptClass.Gujarati },
                { "Gurmukhi", ScriptClass.Gurmukhi },
                { "Hangul", ScriptClass.Hangul },
                { "Han", ScriptClass.Han },
                { "Hanunoo", ScriptClass.Hanunoo },
                { "Hatran", ScriptClass.Hatran },
                { "Hebrew", ScriptClass.Hebrew },
                { "Hiragana", ScriptClass.Hiragana },
                { "Anatolian_Hieroglyphs", ScriptClass.AnatolianHieroglyphs },
                { "Pahawh_Hmong", ScriptClass.PahawhHmong },
                { "Nyiakeng_Puachue_Hmong", ScriptClass.NyiakengPuachueHmong },
                { "Katakana_Or_Hiragana", ScriptClass.KatakanaOrHiragana },
                { "Old_Hungarian", ScriptClass.OldHungarian },
                { "Old_Italic", ScriptClass.OldItalic },
                { "Javanese", ScriptClass.Javanese },
                { "Kayah_Li", ScriptClass.KayahLi },
                { "Katakana", ScriptClass.Katakana },
                { "Kharoshthi", ScriptClass.Kharoshthi },
                { "Khmer", ScriptClass.Khmer },
                { "Khojki", ScriptClass.Khojki },
                { "Khitan_Small_Script", ScriptClass.KhitanSmallScript },
                { "Kannada", ScriptClass.Kannada },
                { "Kaithi", ScriptClass.Kaithi },
                { "Tai_Tham", ScriptClass.TaiTham },
                { "Lao", ScriptClass.Lao },
                { "Latin", ScriptClass.Latin },
                { "Lepcha", ScriptClass.Lepcha },
                { "Limbu", ScriptClass.Limbu },
                { "Linear_A", ScriptClass.LinearA },
                { "Linear_B", ScriptClass.LinearB },
                { "Lisu", ScriptClass.Lisu },
                { "Lycian", ScriptClass.Lycian },
                { "Lydian", ScriptClass.Lydian },
                { "Mahajani", ScriptClass.Mahajani },
                { "Makasar", ScriptClass.Makasar },
                { "Mandaic", ScriptClass.Mandaic },
                { "Manichaean", ScriptClass.Manichaean },
                { "Marchen", ScriptClass.Marchen },
                { "Medefaidrin", ScriptClass.Medefaidrin },
                { "Mende_Kikakui", ScriptClass.MendeKikakui },
                { "Meroitic_Cursive", ScriptClass.MeroiticCursive },
                { "Meroitic_Hieroglyphs", ScriptClass.MeroiticHieroglyphs },
                { "Malayalam", ScriptClass.Malayalam },
                { "Modi", ScriptClass.Modi },
                { "Mongolian", ScriptClass.Mongolian },
                { "Mro", ScriptClass.Mro },
                { "Meetei_Mayek", ScriptClass.MeeteiMayek },
                { "Multani", ScriptClass.Multani },
                { "Myanmar", ScriptClass.Myanmar },
                { "Nandinagari", ScriptClass.Nandinagari },
                { "Old_North_Arabian", ScriptClass.OldNorthArabian },
                { "Nabataean", ScriptClass.Nabataean },
                { "Newa", ScriptClass.Newa },
                { "Nko", ScriptClass.Nko },
                { "Nushu", ScriptClass.Nushu },
                { "Ogham", ScriptClass.Ogham },
                { "Ol_Chiki", ScriptClass.OlChiki },
                { "Old_Turkic", ScriptClass.OldTurkic },
                { "Oriya", ScriptClass.Oriya },
                { "Osage", ScriptClass.Osage },
                { "Osmanya", ScriptClass.Osmanya },
                { "Old_Uyghur", ScriptClass.OldUyghur },
                { "Palmyrene", ScriptClass.Palmyrene },
                { "Pau_Cin_Hau", ScriptClass.PauCinHau },
                { "Old_Permic", ScriptClass.OldPermic },
                { "Phags_Pa", ScriptClass.PhagsPa },
                { "Inscriptional_Pahlavi", ScriptClass.InscriptionalPahlavi },
                { "Psalter_Pahlavi", ScriptClass.PsalterPahlavi },
                { "Phoenician", ScriptClass.Phoenician },
                { "Miao", ScriptClass.Miao },
                { "Inscriptional_Parthian", ScriptClass.InscriptionalParthian },
                { "Rejang", ScriptClass.Rejang },
                { "Hanifi_Rohingya", ScriptClass.HanifiRohingya },
                { "Runic", ScriptClass.Runic },
                { "Samaritan", ScriptClass.Samaritan },
                { "Old_South_Arabian", ScriptClass.OldSouthArabian },
                { "Saurashtra", ScriptClass.Saurashtra },
                { "SignWriting", ScriptClass.SignWriting },
                { "Shavian", ScriptClass.Shavian },
                { "Sharada", ScriptClass.Sharada },
                { "Siddham", ScriptClass.Siddham },
                { "Khudawadi", ScriptClass.Khudawadi },
                { "Sinhala", ScriptClass.Sinhala },
                { "Sogdian", ScriptClass.Sogdian },
                { "Old_Sogdian", ScriptClass.OldSogdian },
                { "Sora_Sompeng", ScriptClass.SoraSompeng },
                { "Soyombo", ScriptClass.Soyombo },
                { "Sundanese", ScriptClass.Sundanese },
                { "Syloti_Nagri", ScriptClass.SylotiNagri },
                { "Syriac", ScriptClass.Syriac },
                { "Tagbanwa", ScriptClass.Tagbanwa },
                { "Takri", ScriptClass.Takri },
                { "Tai_Le", ScriptClass.TaiLe },
                { "New_Tai_Lue", ScriptClass.NewTaiLue },
                { "Tamil", ScriptClass.Tamil },
                { "Tangut", ScriptClass.Tangut },
                { "Tai_Viet", ScriptClass.TaiViet },
                { "Telugu", ScriptClass.Telugu },
                { "Tifinagh", ScriptClass.Tifinagh },
                { "Tagalog", ScriptClass.Tagalog },
                { "Thaana", ScriptClass.Thaana },
                { "Thai", ScriptClass.Thai },
                { "Tibetan", ScriptClass.Tibetan },
                { "Tirhuta", ScriptClass.Tirhuta },
                { "Tangsa", ScriptClass.Tangsa },
                { "Toto", ScriptClass.Toto },
                { "Ugaritic", ScriptClass.Ugaritic },
                { "Vai", ScriptClass.Vai },
                { "Vithkuqi", ScriptClass.Vithkuqi },
                { "Warang_Citi", ScriptClass.WarangCiti },
                { "Wancho", ScriptClass.Wancho },
                { "Old_Persian", ScriptClass.OldPersian },
                { "Cuneiform", ScriptClass.Cuneiform },
                { "Yezidi", ScriptClass.Yezidi },
                { "Yi", ScriptClass.Yi },
                { "Zanabazar_Square", ScriptClass.ZanabazarSquare }
            };

        private static readonly Dictionary<string, JoiningType> JoiningTypeMap
            = new(StringComparer.OrdinalIgnoreCase)
            {
                { "R", JoiningType.RightJoining },
                { "L", JoiningType.LeftJoining },
                { "D", JoiningType.DualJoining },
                { "C", JoiningType.JoinCausing },
                { "U", JoiningType.NonJoining },
                { "T", JoiningType.Transparent }
            };

        private static readonly Dictionary<string, JoiningGroup> JoiningGroupMap
            = new(StringComparer.OrdinalIgnoreCase)
            {
                { "African_Feh", JoiningGroup.AfricanFeh },
                { "African_Noon", JoiningGroup.AfricanNoon },
                { "African_Qaf", JoiningGroup.AfricanQaf },
                { "Ain", JoiningGroup.Ain },
                { "Alaph", JoiningGroup.Alaph },
                { "Alef", JoiningGroup.Alef },
                { "Beh", JoiningGroup.Beh },
                { "Beth", JoiningGroup.Beth },
                { "Burushaski_Yeh_Barree", JoiningGroup.BurushaskiYehBarree },
                { "Dal", JoiningGroup.Dal },
                { "Dalath_Rish", JoiningGroup.DalathRish },
                { "E", JoiningGroup.E },
                { "Farsi_Yeh", JoiningGroup.FarsiYeh },
                { "Fe", JoiningGroup.Fe },
                { "Feh", JoiningGroup.Feh },
                { "Final_Semkath", JoiningGroup.FinalSemkath },
                { "Gaf", JoiningGroup.Gaf },
                { "Gamal", JoiningGroup.Gamal },
                { "Hah", JoiningGroup.Hah },
                { "Hanifi_Rohingya_Kinna_Ya", JoiningGroup.HanifiRohingyaKinnaYa },
                { "Hanifi_Rohingya_Pa", JoiningGroup.HanifiRohingyaPa },
                { "He", JoiningGroup.He },
                { "Heh", JoiningGroup.Heh },
                { "Heh_Goal", JoiningGroup.HehGoal },
                { "Heth", JoiningGroup.Heth },
                { "Kaf", JoiningGroup.Kaf },
                { "Kaph", JoiningGroup.Kaph },
                { "Khaph", JoiningGroup.Khaph },
                { "Knotted_Heh", JoiningGroup.KnottedHeh },
                { "Lam", JoiningGroup.Lam },
                { "Lamadh", JoiningGroup.Lamadh },
                { "Malayalam_Bha", JoiningGroup.MalayalamBha },
                { "Malayalam_Ja", JoiningGroup.MalayalamJa },
                { "Malayalam_Lla", JoiningGroup.MalayalamLla },
                { "Malayalam_Llla", JoiningGroup.MalayalamLlla },
                { "Malayalam_Nga", JoiningGroup.MalayalamNga },
                { "Malayalam_Nna", JoiningGroup.MalayalamNna },
                { "Malayalam_Nnna", JoiningGroup.MalayalamNnna },
                { "Malayalam_Nya", JoiningGroup.MalayalamNya },
                { "Malayalam_Ra", JoiningGroup.MalayalamRa },
                { "Malayalam_Ssa", JoiningGroup.MalayalamSsa },
                { "Malayalam_Tta", JoiningGroup.MalayalamTta },
                { "Manichaean_Aleph", JoiningGroup.ManichaeanAleph },
                { "Manichaean_Ayin", JoiningGroup.ManichaeanAyin },
                { "Manichaean_Beth", JoiningGroup.ManichaeanBeth },
                { "Manichaean_Daleth", JoiningGroup.ManichaeanDaleth },
                { "Manichaean_Dhamedh", JoiningGroup.ManichaeanDhamedh },
                { "Manichaean_Five", JoiningGroup.ManichaeanFive },
                { "Manichaean_Gimel", JoiningGroup.ManichaeanGimel },
                { "Manichaean_Heth", JoiningGroup.ManichaeanHeth },
                { "Manichaean_Hundred", JoiningGroup.ManichaeanHundred },
                { "Manichaean_Kaph", JoiningGroup.ManichaeanKaph },
                { "Manichaean_Lamedh", JoiningGroup.ManichaeanLamedh },
                { "Manichaean_Mem", JoiningGroup.ManichaeanMem },
                { "Manichaean_Nun", JoiningGroup.ManichaeanNun },
                { "Manichaean_One", JoiningGroup.ManichaeanOne },
                { "Manichaean_Pe", JoiningGroup.ManichaeanPe },
                { "Manichaean_Qoph", JoiningGroup.ManichaeanQoph },
                { "Manichaean_Resh", JoiningGroup.ManichaeanResh },
                { "Manichaean_Sadhe", JoiningGroup.ManichaeanSadhe },
                { "Manichaean_Samekh", JoiningGroup.ManichaeanSamekh },
                { "Manichaean_Taw", JoiningGroup.ManichaeanTaw },
                { "Manichaean_Ten", JoiningGroup.ManichaeanTen },
                { "Manichaean_Teth", JoiningGroup.ManichaeanTeth },
                { "Manichaean_Thamedh", JoiningGroup.ManichaeanThamedh },
                { "Manichaean_Twenty", JoiningGroup.ManichaeanTwenty },
                { "Manichaean_Waw", JoiningGroup.ManichaeanWaw },
                { "Manichaean_Yodh", JoiningGroup.ManichaeanYodh },
                { "Manichaean_Zayin", JoiningGroup.ManichaeanZayin },
                { "Meem", JoiningGroup.Meem },
                { "Mim", JoiningGroup.Mim },
                { "No_Joining_Group", JoiningGroup.NoJoiningGroup },
                { "Noon", JoiningGroup.Noon },
                { "Nun", JoiningGroup.Nun },
                { "Nya", JoiningGroup.Nya },
                { "Pe", JoiningGroup.Pe },
                { "Qaf", JoiningGroup.Qaf },
                { "Qaph", JoiningGroup.Qaph },
                { "Reh", JoiningGroup.Reh },
                { "Reversed_Pe", JoiningGroup.ReversedPe },
                { "Rohingya_Yeh", JoiningGroup.RohingyaYeh },
                { "Sad", JoiningGroup.Sad },
                { "Sadhe", JoiningGroup.Sadhe },
                { "Seen", JoiningGroup.Seen },
                { "Semkath", JoiningGroup.Semkath },
                { "Shin", JoiningGroup.Shin },
                { "Straight_Waw", JoiningGroup.StraightWaw },
                { "Swash_Kaf", JoiningGroup.SwashKaf },
                { "Syriac_Waw", JoiningGroup.SyriacWaw },
                { "Tah", JoiningGroup.Tah },
                { "Taw", JoiningGroup.Taw },
                { "Teh_Marbuta", JoiningGroup.TehMarbuta },
                { "Teh_Marbuta_Goal", JoiningGroup.TehMarbutaGoal },
                { "Hamza_On_Heh_Goal", JoiningGroup.TehMarbutaGoal },
                { "Teth", JoiningGroup.Teth },
                { "Thin_Yeh", JoiningGroup.ThinYeh },
                { "Vertical_Tail", JoiningGroup.VerticalTail },
                { "Waw", JoiningGroup.Waw },
                { "Yeh", JoiningGroup.Yeh },
                { "Yeh_Barree", JoiningGroup.YehBarree },
                { "Yeh_With_Tail", JoiningGroup.YehWithTail },
                { "Yudh", JoiningGroup.Yudh },
                { "Yudh_He", JoiningGroup.YudhHe },
                { "Zain", JoiningGroup.Zain },
                { "Zhain", JoiningGroup.Zhain }
            };

        private const string SixLaborsSolutionFileName = "SixLabors.Fonts.sln";
        private const string InputRulesRelativePath = @"src\UnicodeTrieGenerator\Rules";
        private const string OutputResourcesRelativePath = @"src\SixLabors.Fonts\Unicode\Resources";

        private static readonly Lazy<string> SolutionDirectoryFullPathLazy = new(GetSolutionDirectoryFullPathImpl);
        private static readonly Dictionary<int, int> Bidi = new();

        private static string SolutionDirectoryFullPath => SolutionDirectoryFullPathLazy.Value;

        /// <summary>
        /// Generates the various Unicode tries.
        /// </summary>
        public static void GenerateUnicodeTries()
        {
            ProcessUnicodeData();
            GenerateBidiBracketsTrie();
            GenerateBidiMirrorTrie();
            GenerateLineBreakTrie();
            GenerateUnicodeCategoryTrie();
            GenerateScriptTrie();
            GenerateGraphemeBreakTrie();
            GenerateArabicShapingTrie();
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
            GenerateTrieClass("Grapheme", trie);
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
            GenerateTrieClass("Bidi", trie);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the Bidi Brackets code points.
        /// </summary>
        private static void GenerateBidiMirrorTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+);\s([0-9A-F]+)\s#");
            var builder = new UnicodeTrieBuilder(0u);

            using (StreamReader sr = GetStreamReader("BidiMirroring.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        int point = ParseHexInt(match.Groups[1].Value);
                        int otherPoint = ParseHexInt(match.Groups[2].Value);
                        builder.Set(point, (uint)otherPoint);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();
            GenerateTrieClass("BidiMirror", trie);
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
            GenerateTrieClass("LineBreak", trie);
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
            GenerateTrieClass("UnicodeCategory", trie);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the script code point ranges.
        /// </summary>
        private static void GenerateScriptTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;\s*(.*?)\s*#");
            var builder = new UnicodeTrieBuilder((uint)ScriptClass.Unknown);

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
                        string script = match.Groups[3].Value;

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)ScriptMap[script], true);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();
            GenerateTrieClass("Script", trie);
        }

        /// <summary>
        /// Generates the UnicodeTrie for the script code point ranges.
        /// </summary>
        private static void GenerateArabicShapingTrie()
        {
            var regex = new Regex(@"^([0-9A-F]+)(?:\.\.([0-9A-F]+))?\s*;[\w\s]+;\s*([A-Z]+);\s*([\w\s]+)");
            const uint initial = ((int)JoiningType.NonJoining) | (((int)JoiningGroup.NoJoiningGroup) << 16);
            var builder = new UnicodeTrieBuilder(initial);

            using (StreamReader sr = GetStreamReader("ArabicShaping.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match match = regex.Match(line);

                    if (match.Success)
                    {
                        string start = match.Groups[1].Value;
                        string end = match.Groups[2].Value;
                        string type = match.Groups[3].Value;
                        string group = match.Groups[4].Value.Replace(" ", "_");

                        if (end?.Length == 0)
                        {
                            end = start;
                        }

                        JoiningType jt = JoiningTypeMap[type];
                        JoiningGroup jg = JoiningGroupMap[group];

                        builder.SetRange(ParseHexInt(start), ParseHexInt(end), (uint)((int)jt | ((int)jg << 16)), true);
                    }
                }
            }

            UnicodeTrie trie = builder.Freeze();
            GenerateTrieClass("ArabicShaping", trie);
        }

        /// <summary>
        /// Generates the trie class.
        /// </summary>
        /// <param name="name">The name of the class.</param>
        /// <param name="trie">The Unicode trie.</param>
        private static void GenerateTrieClass(string name, UnicodeTrie trie)
        {
            var stream = new MemoryStream();

            trie.Save(stream);

            using FileStream fileStream = GetStreamWriter($"{name}Trie.Generated.cs");
            using var writer = new StreamWriter(fileStream);

            writer.WriteLine("// Copyright (c) Six Labors.");
            writer.WriteLine("// Licensed under the Apache License, Version 2.0.");
            writer.WriteLine();
            writer.WriteLine("// <auto-generated />");
            writer.WriteLine("using System;");
            writer.WriteLine();
            writer.WriteLine("namespace SixLabors.Fonts.Unicode.Resources");
            writer.WriteLine("{");
            writer.WriteLine($"    internal static class {name}Trie");
            writer.WriteLine("    {");
            writer.WriteLine("        public static ReadOnlySpan<byte> Data => new byte[]");
            writer.WriteLine("        {");

            stream.Position = 0;

            writer.Write("            ");

            long length = stream.Length;
            while (true)
            {
                int b = stream.ReadByte();

                if (b == -1)
                {
                    break;
                }

                writer.Write(b.ToString());

                if (stream.Position % 100 > 0 && stream.Position != length)
                {
                    writer.Write(", ");
                }
                else
                {
                    writer.Write(',');
                    writer.Write(Environment.NewLine);

                    if (stream.Position != length)
                    {
                        writer.Write("            ");
                    }
                }
            }

            writer.WriteLine("        };");
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }

        private static StreamReader GetStreamReader(string path)
        {
            string filename = GetFullPath(Path.Combine(InputRulesRelativePath, path));
            return File.OpenText(filename);
        }

        private static FileStream GetStreamWriter(string path)
        {
            string filename = GetFullPath(Path.Combine(OutputResourcesRelativePath, path));
            return File.Create(filename);
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
