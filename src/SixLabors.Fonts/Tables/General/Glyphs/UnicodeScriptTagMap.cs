// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    /// <summary>
    /// Provides a map from Unicode <see cref="Script"/> to OTF <see cref="Tag"/>.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/spec/scripttags"/>
    /// </summary>
    internal sealed class UnicodeScriptTagMap : Dictionary<Script, Tag[]>
    {
        private static readonly Lazy<UnicodeScriptTagMap> Lazy = new Lazy<UnicodeScriptTagMap>(() => CreateMap());

        private UnicodeScriptTagMap()
        {
        }

        public static UnicodeScriptTagMap Instance => Lazy.Value;

        // This map is a port from Apache FontBox with changes from HarfBuzz.
        private static UnicodeScriptTagMap CreateMap()
            => new UnicodeScriptTagMap
            {
                { Script.Adlam, new[] { Tag.Parse("adlm") } },
                { Script.Ahom, new[] { Tag.Parse("ahom") } },
                { Script.AnatolianHieroglyphs, new[] { Tag.Parse("hluw") } },
                { Script.Arabic, new[] { Tag.Parse("arab") } },
                { Script.Armenian, new[] { Tag.Parse("armn") } },
                { Script.Avestan, new[] { Tag.Parse("avst") } },
                { Script.Balinese, new[] { Tag.Parse("bali") } },
                { Script.Bamum, new[] { Tag.Parse("bamu") } },
                { Script.BassaVah, new[] { Tag.Parse("bass") } },
                { Script.Batak, new[] { Tag.Parse("batk") } },
                { Script.Bengali, new[] { Tag.Parse("bng2"), Tag.Parse("beng") } },
                { Script.Bhaiksuki, new[] { Tag.Parse("bhks") } },
                { Script.Bopomofo, new[] { Tag.Parse("bopo") } },
                { Script.Brahmi, new[] { Tag.Parse("brah") } },
                { Script.Braille, new[] { Tag.Parse("brai") } },
                { Script.Buginese, new[] { Tag.Parse("bugi") } },
                { Script.Buhid, new[] { Tag.Parse("buhd") } },

                // TODO:  Byzantine Music: byzm
                { Script.CanadianAboriginal, new[] { Tag.Parse("cans") } },
                { Script.Carian, new[] { Tag.Parse("cari") } },
                { Script.CaucasianAlbanian, new[] { Tag.Parse("aghb") } },
                { Script.Chakma, new[] { Tag.Parse("cakm") } },
                { Script.Cham, new[] { Tag.Parse("cham") } },
                { Script.Cherokee, new[] { Tag.Parse("cher") } },
                { Script.Common, new[] { Tag.Parse("zyyy") } },
                { Script.Coptic, new[] { Tag.Parse("copt") } },
                { Script.Cuneiform, new[] { Tag.Parse("xsux") } }, // "Sumero-Akkadian Cuneiform" in OpenType
                { Script.Cypriot, new[] { Tag.Parse("cprt") } },
                { Script.Cyrillic, new[] { Tag.Parse("cyrl") } },
                { Script.Deseret, new[] { Tag.Parse("dsrt") } },
                { Script.Devanagari, new[] { Tag.Parse("dev2"), Tag.Parse("deva") } },
                { Script.Duployan, new[] { Tag.Parse("dupl") } },
                { Script.EgyptianHieroglyphs, new[] { Tag.Parse("egyp") } },
                { Script.Elbasan, new[] { Tag.Parse("elba") } },
                { Script.Ethiopic, new[] { Tag.Parse("ethi") } },
                { Script.Georgian, new[] { Tag.Parse("geor") } },
                { Script.Glagolitic, new[] { Tag.Parse("glag") } },
                { Script.Gothic, new[] { Tag.Parse("goth") } },
                { Script.Grantha, new[] { Tag.Parse("gran") } },
                { Script.Greek, new[] { Tag.Parse("grek") } },
                { Script.Gujarati, new[] { Tag.Parse("gjr2"), Tag.Parse("gujr") } },
                { Script.Gurmukhi, new[] { Tag.Parse("gur2"), Tag.Parse("guru") } },
                { Script.Han, new[] { Tag.Parse("hani") } }, // "CJK Ideographic" in OpenType
                { Script.Hangul, new[] { Tag.Parse("hang"), Tag.Parse("jamo") } },
                { Script.Hanunoo, new[] { Tag.Parse("hano") } },
                { Script.Hatran, new[] { Tag.Parse("hatr") } },
                { Script.Hebrew, new[] { Tag.Parse("hebr") } },
                { Script.Hiragana, new[] { Tag.Parse("kana") } },
                { Script.ImperialAramaic, new[] { Tag.Parse("armi") } },
                { Script.Inherited, new[] { Tag.Parse("zinh") } },
                { Script.InscriptionalPahlavi, new[] { Tag.Parse("phli") } },
                { Script.InscriptionalParthian, new[] { Tag.Parse("prti") } },
                { Script.Javanese, new[] { Tag.Parse("java") } },
                { Script.Kaithi, new[] { Tag.Parse("kthi") } },
                { Script.Kannada, new[] { Tag.Parse("knd2"), Tag.Parse("knda") } },
                { Script.Katakana, new[] { Tag.Parse("kana") } },
                { Script.KayahLi, new[] { Tag.Parse("kali") } },
                { Script.Kharoshthi, new[] { Tag.Parse("khar") } },
                { Script.Khmer, new[] { Tag.Parse("khmr") } },
                { Script.Khojki, new[] { Tag.Parse("khoj") } },
                { Script.Khudawadi, new[] { Tag.Parse("sind") } },
                { Script.Lao, new[] { Tag.Parse("lao ") } },
                { Script.Latin, new[] { Tag.Parse("latn") } },
                { Script.Lepcha, new[] { Tag.Parse("lepc") } },
                { Script.Limbu, new[] { Tag.Parse("limb") } },
                { Script.LinearA, new[] { Tag.Parse("lina") } },
                { Script.LinearB, new[] { Tag.Parse("linb") } },
                { Script.Lisu, new[] { Tag.Parse("lisu") } },
                { Script.Lycian, new[] { Tag.Parse("lyci") } },
                { Script.Lydian, new[] { Tag.Parse("lydi") } },
                { Script.Mahajani, new[] { Tag.Parse("mahj") } },
                { Script.Malayalam, new[] { Tag.Parse("mlm2"), Tag.Parse("mlym") } },
                { Script.Mandaic, new[] { Tag.Parse("mand") } },
                { Script.Manichaean, new[] { Tag.Parse("mani") } },
                { Script.Marchen, new[] { Tag.Parse("marc") } },

                // TODO: Mathematical Alphanumeric Symbols: math
                { Script.MeeteiMayek, new[] { Tag.Parse("mtei") } },
                { Script.MendeKikakui, new[] { Tag.Parse("mend") } },
                { Script.MeroiticCursive, new[] { Tag.Parse("merc") } },
                { Script.MeroiticHieroglyphs, new[] { Tag.Parse("mero") } },
                { Script.Miao, new[] { Tag.Parse("plrd") } },
                { Script.Modi, new[] { Tag.Parse("modi") } },
                { Script.Mongolian, new[] { Tag.Parse("mong") } },
                { Script.Mro, new[] { Tag.Parse("mroo") } },
                { Script.Multani, new[] { Tag.Parse("mult") } },

                // TODO: Musical Symbols: musc
                { Script.Myanmar, new[] { Tag.Parse("mym2"), Tag.Parse("mymr") } },
                { Script.Nabataean, new[] { Tag.Parse("nbat") } },
                { Script.Newa, new[] { Tag.Parse("newa") } },
                { Script.NewTaiLue, new[] { Tag.Parse("talu") } },
                { Script.Nko, new[] { Tag.Parse("nko ") } },
                { Script.Ogham, new[] { Tag.Parse("ogam") } },
                { Script.OlChiki, new[] { Tag.Parse("olck") } },
                { Script.OldItalic, new[] { Tag.Parse("ital") } },
                { Script.OldHungarian, new[] { Tag.Parse("hung") } },
                { Script.OldNorthArabian, new[] { Tag.Parse("narb") } },
                { Script.OldPermic, new[] { Tag.Parse("perm") } },
                { Script.OldPersian, new[] { Tag.Parse("xpeo") } },
                { Script.OldSouthArabian, new[] { Tag.Parse("sarb") } },
                { Script.OldTurkic, new[] { Tag.Parse("orkh") } },
                { Script.Oriya, new[] { Tag.Parse("ory2"), Tag.Parse("orya") } }, // "Odia (formerly Oriya)" in OpenType
                { Script.Osage, new[] { Tag.Parse("osge") } },
                { Script.Osmanya, new[] { Tag.Parse("osma") } },
                { Script.PahawhHmong, new[] { Tag.Parse("hmng") } },
                { Script.Palmyrene, new[] { Tag.Parse("palm") } },
                { Script.PauCinHau, new[] { Tag.Parse("pauc") } },
                { Script.PhagsPa, new[] { Tag.Parse("phag") } },
                { Script.Phoenician, new[] { Tag.Parse("phnx") } },
                { Script.PsalterPahlavi, new[] { Tag.Parse("phlp") } },
                { Script.Rejang, new[] { Tag.Parse("rjng") } },
                { Script.Runic, new[] { Tag.Parse("runr") } },
                { Script.Samaritan, new[] { Tag.Parse("samr") } },
                { Script.Saurashtra, new[] { Tag.Parse("saur") } },
                { Script.Sharada, new[] { Tag.Parse("shrd") } },
                { Script.Shavian, new[] { Tag.Parse("shaw") } },
                { Script.Siddham, new[] { Tag.Parse("sidd") } },
                { Script.SignWriting, new[] { Tag.Parse("sgnw") } },
                { Script.Sinhala, new[] { Tag.Parse("sinh") } },
                { Script.SoraSompeng, new[] { Tag.Parse("sora") } },
                { Script.Sundanese, new[] { Tag.Parse("sund") } },
                { Script.SylotiNagri, new[] { Tag.Parse("sylo") } },
                { Script.Syriac, new[] { Tag.Parse("syrc") } },
                { Script.Tagalog, new[] { Tag.Parse("tglg") } },
                { Script.Tagbanwa, new[] { Tag.Parse("tagb") } },
                { Script.TaiLe, new[] { Tag.Parse("tale") } },
                { Script.TaiTham, new[] { Tag.Parse("lana") } },
                { Script.TaiViet, new[] { Tag.Parse("tavt") } },
                { Script.Takri, new[] { Tag.Parse("takr") } },
                { Script.Tamil, new[] { Tag.Parse("tml2"), Tag.Parse("taml") } },
                { Script.Tangut, new[] { Tag.Parse("tang") } },
                { Script.Telugu, new[] { Tag.Parse("tel2"), Tag.Parse("telu") } },
                { Script.Thaana, new[] { Tag.Parse("thaa") } },
                { Script.Thai, new[] { Tag.Parse("thai") } },
                { Script.Tibetan, new[] { Tag.Parse("tibt") } },
                { Script.Tifinagh, new[] { Tag.Parse("tfng") } },
                { Script.Tirhuta, new[] { Tag.Parse("tirh") } },
                { Script.Ugaritic, new[] { Tag.Parse("ugar") } },
                { Script.Unknown, new[] { Tag.Parse("zzzz") } },
                { Script.Vai, new[] { Tag.Parse("vai ") } },
                { Script.WarangCiti, new[] { Tag.Parse("wara") } },
                { Script.Yi, new[] { Tag.Parse("yi  ") } }
            };
    }
}
