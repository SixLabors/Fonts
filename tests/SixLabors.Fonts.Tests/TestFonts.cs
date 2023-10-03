// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SixLabors.Fonts.Tests;

public static class TestFonts
{
    private static readonly ConcurrentDictionary<string, Stream> Cache = new();

    public static string TwemojiMozillaFile => GetFullPath("Twemoji Mozilla.ttf");

    public static string SegoeuiEmojiFile => GetFullPath("seguiemj-win11.ttf");

    public static string CarterOneFile => GetFullPath("Carter_One/CarterOne.ttf");

    public static string WendyOneFile => GetFullPath("Wendy_One/WendyOne-Regular.ttf");

    // Font from: https://google-webfonts-helper.herokuapp.com/fonts/open-sans?subsets=cyrillic,cyrillic-ext,greek,greek-ext,hebrew,latin,latin-ext,vietnamese
    public static string OpenSansFile => GetFullPath("OpenSans-Regular.ttf");

    public static string OpenSansFileWoff1 => GetFullPath("OpenSans-Regular.woff");

    public static string OpenSansFileWoff2 => GetFullPath("OpenSans-Regular.woff2");

    public static string SimpleFontFile => GetFullPath("SixLaborsSampleAB.ttf");

    public static string SimpleFontFileWoff => GetFullPath("SixLaborsSampleAB.woff");

    public static string ArabicFontFile => GetFullPath("Dubai-Regular.ttf");

    public static string Anchor2FontFile => GetFullPath("DejaVuSans.ttf");

    public static string SegeouiFontFile => GetFullPath("Segoeui.ttf");

    public static string TimesNewRomanFile => GetFullPath("TimesNewRoman.ttf");

    // FdSelect format 0: https://github.com/adobe-fonts/fdarray-test/
    public static string FDArrayTest257File => GetFullPath("FDArrayTest257.otf");

    /// <summary>
    /// Gets a gsub test font file which has the following substitution for unit tests:
    /// - Single Substitution: A -> B
    /// - Multiple Substitution: C -> D
    /// - Alternate Substitution: E -> F
    /// </summary>
    public static string GSubTestFontFile1 => GetFullPath("GsubTestFont1.ttf");

    /// <summary>
    /// Gets a gsub test font file which has the following substitution for unit tests:
    /// - Chained Context Substitution, Format 3: x=y -> x>y
    /// - Reverse Chaining Contextual Single Substitution: X89 -> XYZ
    /// </summary>
    public static string GSubTestFontFile2 => GetFullPath("GsubTestFont2.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Chained Context Substitution, Format 2:
    /// "\u1361\u136B\u1361" -> The character in the middle should be replaced with the final form.
    /// </summary>
    public static string GSubTestFontFile3 => GetFullPath("TestShapeEthi.ttf");

    /// <summary>
    /// Gets a gsub test file specifically crafted to intiate a billion laughs attack.
    /// </summary>
    public static string GSubLookupType2BillionLaughs => GetFullPath("TestGSUBThree.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Context Substitution Format 1:
    /// "6566" ("\u0041\u0042") -> "6576"
    /// </summary>
    public static string GSubLookupType5Format1 => GetFullPath("GsubLookupType5Format1.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Context Substitution Format 3:
    /// "65666768" ("\u0041\u0042\u0043\u0044") -> "657678"
    /// </summary>
    public static string GSubLookupType5Format3 => GetFullPath("GsubLookupType5Format3.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Context Substitution Format 2:
    /// "6566" ("\u0041\u0042") -> "6576"
    /// </summary>
    public static string GSubLookupType5Format2 => GetFullPath("GsubLookupType5Format2.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Chained Contexts Substitution Subtable Format 1:
    /// "20212223" ("\u0014\u0015\u0016\u0017") -> "20636423"
    /// </summary>
    public static string GSubLookupType6Format1 => GetFullPath("GsubLookupType6Format1.ttf");

    /// <summary>
    /// Gets a gsub test font file (from harfbuzz tests) which has the following substitution for unit tests:
    /// - Chained Contexts Substitution Subtable Format 2:
    /// "20212223" ("\u0014\u0015\u0016\u0017") -> "20216423"
    /// </summary>
    public static string GSubLookupType6Format2 => GetFullPath("GsubLookupType6Format2.ttf");

    /// <summary>
    /// Gets a gsub test font file which has the following substitution for unit tests:
    /// - Chained Context Substitution, Format 3: [bovw] -> [a-z]
    /// Script from FontForge example: https://fontforge.org/docs/ui/dialogs/contextchain.html
    /// </summary>
    public static string FormalScript => GetFullPath("FormalScript.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Single Adjustment Positioning, Format 1: "\u0012" and "\u0014" XPlacement minus 200.
    /// </summary>
    public static string GposLookupType1Format1 => GetFullPath("GposLookupType1Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Single Adjustment Positioning, Format 2: "\u0012" XPlacement minus 200 and "\u0014" XPlacement minus 300.
    /// </summary>
    public static string GposLookupType1Format2 => GetFullPath("GposLookupType1Format2.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Pair Adjustment Positioning, Format 1: "\u0012\u0014" first XPlacement minus 300 and second YPlacement minus 400.
    /// </summary>
    public static string GposLookupType2Format1 => GetFullPath("GposLookupType2Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Cursive Attachment Positioning, Format 1: "\u0012\u0012" characters should overlap.
    /// </summary>
    public static string GposLookupType3Format1 => GetFullPath("GposLookupType3Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Mark-to-Base Attachment Positioning, Format 1: "\u0012\u0013" Base 100, 150; Mark 200, 230
    /// </summary>
    public static string GposLookupType4Format1 => GetFullPath("GposLookupType4Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Mark-to-Ligature Attachment Positioning, Format 1: "\u0012\u0013" Ligature 100, 150; Mark 200, 230
    /// </summary>
    public static string GposLookupType5Format1 => GetFullPath("GposLookupType5Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Mark-to-Mark Attachment Positioning, Format 1: "\u0012\u0013" FirstMark 100, 150; SecondMark 200, 230
    /// </summary>
    public static string GposLookupType6Format1 => GetFullPath("GposLookupType6Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Contextual Positioning Subtables, Format 1: "\u0014\u0015\u0016" XPlacement plus 20.
    /// </summary>
    public static string GposLookupType7Format1 => GetFullPath("GposLookupType7Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Contextual Positioning Subtables, Format 2: "\u0014\u0015\u0016" XPlacement plus 20.
    /// </summary>
    public static string GposLookupType7Format2 => GetFullPath("GposLookupType7Format2.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Contextual Positioning Subtables, Format 3: "\u0014\u0015\u0016" XPlacement plus 20.
    /// </summary>
    public static string GposLookupType7Format3 => GetFullPath("GposLookupType7Format3.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Chained Contexts Positioning, Format 1:
    /// "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
    /// </summary>
    public static string GposLookupType8Format1 => GetFullPath("GposLookupType8Format1.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Chained Contexts Positioning, Format 2:
    /// "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
    /// </summary>
    public static string GposLookupType8Format2 => GetFullPath("GposLookupType8Format2.ttf");

    /// <summary>
    /// Gets a gpos test font file which has the following substitution for unit tests:
    /// - Chained Contexts Positioning, Format 3:
    /// "\u0014\u0015\u0016\u0017" backtrack:\u0014, input:\u0015\u0016, lookahead:u0017 -> XPlacement plus 200.
    /// </summary>
    public static string GposLookupType8Format3 => GetFullPath("GposLookupType8Format3.ttf");

    /// <summary>
    /// Gets the eb-garamond font, which can be used to test old style figures feature.
    /// </summary>
    public static string EbGaramond => GetFullPath("EBGaramond-Regular.ttf");

    public static string RobotoRegular => GetFullPath("Roboto-Regular.ttf");

    public static string RobotoFlex => GetFullPath("RobotoFlex.ttf");

    public static string SimpleTrueTypeCollection => GetFullPath("Sample.ttc");

    public static string WhitneyBookFile => GetFullPath("whitney-book.ttf");

    public static string Version1Font => GetFullPath("Font-Version1.ttf");

    public static string NotoSansSCThinFile => GetFullPath("NotoSansSC-Thin.ttf");

    public static string NotoSansKRRegular => GetFullPath("NotoSansKR-Regular.otf");

    public static string NotoSansBalineseRegular => GetFullPath("NotoSansBalinese-Regular.ttf");

    public static string NotoSansBengaliRegular => GetFullPath("NotoSansBengali-Regular.ttf");

    public static string NotoSansDevanagariRegular => GetFullPath("NotoSansDevanagari-Regular.ttf");

    public static string NotoSerifKannadaRegular => GetFullPath("NotoSerifKannada-Regular.ttf");

    public static string NotoSansTeluguRegular => GetFullPath("NotoSansTelugu-Regular.ttf");

    public static string NotoSansTamilRegular => GetFullPath("NotoSansTamil-Regular.ttf");

    public static string NotoSansKannadaRegular => GetFullPath("NotoSansKannada-Regular.ttf");

    public static string NotoSansGurmukhiRegular => GetFullPath("NotoSansGurmukhi-Regular.ttf");

    public static string NotoSansGujaratiRegular => GetFullPath("NotoSansGujarati-Regular.ttf");

    public static string NotoSansMalayalamRegular => GetFullPath("NotoSansMalayalam-Regular.ttf");

    public static string NotoSansOriyaRegular => GetFullPath("NotoSansOriya-Regular.ttf");

    public static string NotoSansKhmerRegular => GetFullPath("NotoSansKhmer-Regular.ttf");

    public static string HelveticaTTCFile => GetFullPath("Helvetica.ttc");

    public static string MeQuranFile => GetFullPath("me_quran_volt_newmet.ttf");

    public static string PMINGLIUFile => GetFullPath("PMINGLIU.ttf");

    public static string AliceFrancesHMKRegularFile => GetFullPath("AliceFrancesHMK-Regular.ttf");

    public static string SumanaRegularFile => GetFullPath("Sumana-Regular.ttf");

    public static string DFKaiSBFile => GetFullPath("kaiu.ttf");

    public static string THSarabunFile => GetFullPath("THSarabun.ttf");

    public static Stream TwemojiMozillaData() => OpenStream(TwemojiMozillaFile);

    public static Stream SegoeuiEmojiData() => OpenStream(SegoeuiEmojiFile);

    public static Stream WendyOneFileData() => OpenStream(WendyOneFile);

    public static Stream CarterOneFileData() => OpenStream(CarterOneFile);

    public static Stream SimpleFontFileData() => OpenStream(SimpleFontFile);

    public static Stream ArabicFontFileData() => OpenStream(ArabicFontFile);

    public static Stream OpenSansTtfData() => OpenStream(OpenSansFile);

    public static Stream OpensSansWoff1Data() => OpenStream(OpenSansFileWoff1);

    public static Stream OpensSansWoff2Data() => OpenStream(OpenSansFileWoff2);

    public static Stream SimpleFontFileWoffData() => OpenStream(SimpleFontFileWoff);

    public static Stream SSimpleTrueTypeCollectionData() => OpenStream(SimpleTrueTypeCollection);

    public static class Issues
    {
        public static string Issue96File => GetFullPath("Issues/Issue96.fuzz");

        public static string Issue97File => GetFullPath("Issues/Issue97.fuzz");

        public static string Issue298File => GetFullPath("Issues/StyleScript.ttf");
    }

    private static Stream OpenStream(string path) =>
        Cache.GetOrAdd(
            path,
            p =>
            {
                using FileStream fs = File.OpenRead(p);
                return fs.Clone();
            }).Clone();

    private static Stream Clone(this Stream src)
    {
        var ms = new MemoryStream();
        src.Position = 0;
        src.CopyTo(ms);
        ms.Position = 0;
        return ms;
    }

    private static string GetFullPath(string path)
    {
        string root = Path.GetDirectoryName(new Uri(typeof(TestFonts).GetTypeInfo().Assembly.CodeBase).LocalPath);

        string[] paths = new[]
        {
            "Fonts",
            @"..\..\Fonts",
            @"..\..\..\..\Fonts",
            @"..\..\..\..\..\Fonts"
        };

        IEnumerable<string> fullPaths = paths.Select(x => Path.GetFullPath(Path.Combine(root, x)));
        string rootPath = fullPaths.FirstOrDefault(Directory.Exists);

        Assert.True(rootPath != null, $"could not find the font folder in any of these location, \n{string.Join("\n", fullPaths)}");

        return Path.Combine(rootPath, path);
    }
}