// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HarfBuzzSharp;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Unicode;
using HBBuffer = HarfBuzzSharp.Buffer;
using HBFace = HarfBuzzSharp.Face;
using HBFont = HarfBuzzSharp.Font;
using HBTag = HarfBuzzSharp.Tag;
using HBVariation = HarfBuzzSharp.Variation;
using ShapingTag = SixLabors.Fonts.Tables.AdvancedTypographic.Tag;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Shapes the reference implementation's own corpus and requires this library to
/// produce the same glyphs in the same places, one test per case.
/// </summary>
/// <remarks>
/// <para>
/// The corpus is a pinned submodule fetched by the generator project. It contains
/// the reference implementation's shaping cases and OpenType lookup tests.
/// AAT comparisons use the results in the pinned corpus because HarfBuzzSharp
/// 14.2.1.1 gives different AAT results for some cases. OpenType cases run against
/// HarfBuzzSharp. Verified OpenType reference errors are skipped on the affected version.
/// </para>
/// <para>
/// Every glyph is compared whole: which glyph, where it sits, how far it advances,
/// and which character it came from. Comparing glyph identity alone would say
/// nothing about mark attachment or advance zeroing, which is where shaping most
/// often goes wrong.
/// </para>
/// <para>
/// A case this harness cannot mirror is not emitted as a test at all, rather than
/// emitted and passed: the options it cannot set are listed in
/// <see cref="UnsupportedOptions"/>, and a case naming a font the host does not
/// carry is left out on that host and shaped wherever the font is installed. Every
/// unresolvable reference in the corpus is an Apple system font named by absolute
/// path, so a macOS run covers cases a Windows run cannot.
/// </para>
/// <para>
/// Shaping-only and damaged fonts this harness cannot load are listed in
/// <see cref="UnsupportedFontFiles"/>.
/// </para>
/// </remarks>
public partial class HarfBuzzCorpusTests
{
    /// <summary>
    /// The largest scalar value the standard defines.
    /// </summary>
    private const int LastScalarValue = 0x10FFFF;

    /// <summary>
    /// The first surrogate code point. A surrogate is not a scalar value and cannot
    /// stand on its own in text.
    /// </summary>
    private const int FirstSurrogate = 0xD800;

    /// <summary>
    /// The last surrogate code point.
    /// </summary>
    private const int LastSurrogate = 0xDFFF;

    /// <summary>
    /// The reference implementation is invoked through its OpenType shaper.
    /// </summary>
    private static readonly string[] OpenTypeShaper = ["ot"];

    /// <summary>
    /// The command line options this harness cannot mirror. A case using one of them
    /// is not emitted.
    /// </summary>
    private static readonly string[] UnsupportedOptions =
    [
        "--preserve-default-ignorables",
        "--bot",
        "--eot",
        "--shaper",
        "--font-ptem",
        "--unicodes-before",
        "--unicodes-after",
        "--font-slant",
        "--font-bold",
        "--not-found-variation-selector-glyph",
        "--face-index"
    ];

    /// <summary>
    /// The font files this harness cannot compare.
    /// </summary>
    private static readonly HashSet<string> UnsupportedFontFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // This minimal spacing fixture omits the required name, OS/2, and post tables.
        "1c2c3fc37b2d4c3cb2ef726c6cdaaabd4b7f3eb9.ttf",

        // This bitmap-only fixture has CBDT and CBLC data but no supported outline tables.
        "3cf6f8ac6d647473a43a3100e7494b202b2cfafe.ttf",

        // This Indic fixture omits the required name and OS/2 tables and has no outline tables.
        "755160ddba002332349fda3eb999e629d63dccf6.ttf",

        // This Indic fixture omits required metrics and naming tables and has no complete outline-table pair.
        "932ad5132c2761297c74e9976fe25b08e5ffa10b.ttf",

        // This OTTO fixture has neither a CFF nor CFF2 outline table.
        "a59fd13f1525a91cbe529c882e93d9d1fbb80463.ttf",

        // This bitmap-only fixture has CBDT and CBLC data but no supported outline tables.
        "ee39587d13b2afa5499cc79e45780aa79293bbd4.ttf",

        // These shaping-only AAT fixtures omit required font tables, including name.
        "3503b8963831c2381809884633f483b7361baa97.ttf",
        "7fb6608532fe39dd993eb3fc74e84256bd2d547e.ttf",

        // This deliberately damaged fixture reaches truncated glyph data for some inputs.
        "HarfBust.ttf"
    };

    private readonly ITestOutputHelper output;

    /// <summary>
    /// Initializes a new instance of the <see cref="HarfBuzzCorpusTests"/> class.
    /// </summary>
    /// <param name="output">The test output helper.</param>
    public HarfBuzzCorpusTests(ITestOutputHelper output) => this.output = output;

    /// <summary>
    /// Gets every corpus case this library is expected to match.
    /// </summary>
    /// <returns>
    /// The font, shaping options, input code points, and recorded result for each case.
    /// A null result selects a live HarfBuzzSharp comparison for an OpenType case.
    /// </returns>
    public static TheoryData<string, string, string, string?> CorpusCases()
    {
        TheoryData<string, string, string, string?> data = [];

        foreach (string file in EnumerateCorpusFiles())
        {
            string name = Path.GetFileName(file);
            bool isAat = name.StartsWith("aat-", StringComparison.Ordinal) || IsMetamorphosisTestFile(name);

            foreach (string rawLine in File.ReadLines(file))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                string[] parts = line.Split(';');
                if (parts.Length < 3)
                {
                    continue;
                }

                if (!TryReadText(parts[2], out string text) || HasUnsupportedOptions(parts[1], isAat, text))
                {
                    continue;
                }

                string? fontPath = ResolveFontPath(file, parts[0]);
                if (fontPath is null)
                {
                    if (isAat && !Path.IsPathRooted(parts[0]))
                    {
                        throw new FileNotFoundException($"Missing font '{parts[0]}' for AAT corpus '{file}'.");
                    }

                    continue;
                }

                // Shaping-only and damaged fixtures cannot satisfy the Font contract,
                // so they are excluded explicitly instead of hiding loader exceptions.
                if (UnsupportedFontFiles.Contains(Path.GetFileName(fontPath)))
                {
                    continue;
                }

                data.Add(fontPath, parts[1], parts[2], isAat ? parts[3] : null);
            }
        }

        return data;
    }

    /// <summary>
    /// Compares one shaped result with the reference output for the case.
    /// </summary>
    /// <param name="fontPath">The font to shape with.</param>
    /// <param name="options">The case's options, which may name features.</param>
    /// <param name="codePoints">The characters to shape.</param>
    /// <param name="expectedOutput">The recorded AAT result. A null value selects a live comparison.</param>
    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void ShapesAsReferenceDoes(string fontPath, string options, string codePoints, string? expectedOutput)
        => this.CompareShape(fontPath, options, codePoints, expectedOutput);

    /// <summary>
    /// Shapes a corpus case or a regression fixture made for the Fonts suite.
    /// </summary>
    /// <param name="fontPath">The font to shape with.</param>
    /// <param name="options">The requested shaping options.</param>
    /// <param name="codePoints">The characters to shape.</param>
    /// <param name="expectedOutput">The recorded result, or null to shape the input with HarfBuzzSharp.</param>
    private void CompareShape(string fontPath, string options, string codePoints, string? expectedOutput)
    {
        this.output.WriteLine($"Font {fontPath}");
        this.output.WriteLine($"Options {options}");
        this.output.WriteLine($"Input {codePoints}");

#if HARFBUZZSHARP_14_2_1_1
        // Fonts matches native HarfBuzz at the corpus submodule commit.
        // HarfBuzzSharp 14.2.1.1 gives incorrect results for these rows.
        // Remove these skips when a newer reference passes the live comparisons.
        if (expectedOutput is null && GetReferenceSkipReason(Path.GetFileName(fontPath), options, codePoints) is string reason)
        {
            Assert.Skip(reason);
        }
#endif

        Assert.True(TryReadText(codePoints, out string text));

        List<Feature> features = ReadFeatures(options);

        using Blob blob = Blob.FromFile(fontPath);
        using HBFace face = new(blob, 0);
        using HBFont referenceFont = new(face);
        referenceFont.SetFunctionsOpenType();

        // The corpus positions are expressed at the face's em scale. Giving both
        // engines that size preserves direct, exact comparisons of their outputs.
        string requestedSize = ReadOptionValue(options, "--font-size");
        int shapingSize = requestedSize.Length == 0
            ? (int)face.UnitsPerEm
            : int.Parse(requestedSize, CultureInfo.InvariantCulture);

        referenceFont.SetScale(shapingSize, shapingSize);

        Assert.True(TryReadVariations(options, out HBVariation[] referenceVariations, out FontVariation[] variations));
        referenceFont.SetVariations(referenceVariations);
        using HBBuffer buffer = new();
        buffer.AddUtf16(text);

        string requestedScript = ReadOptionValue(options, "--script");
        if (requestedScript.Length > 0)
        {
            buffer.Script = Script.Parse(requestedScript);
        }

        Direction? requestedDirection = ReadDirection(options);
        if (requestedDirection.HasValue)
        {
            buffer.Direction = requestedDirection.Value;
        }

        string language = ReadOptionValue(options, "--language");
        if (language.Length != 0)
        {
            buffer.Language = new Language(language.Replace('_', '-'));
        }

        buffer.GuessSegmentProperties();
        if (expectedOutput is null)
        {
            referenceFont.Shape(buffer, features, OpenTypeShaper);
        }

        FontCollection fontCollection = new();
        string extension = Path.GetExtension(fontPath);
        Font font;
        if (extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otc", StringComparison.OrdinalIgnoreCase))
        {
            FileFontMetrics metrics = FileFontMetrics.LoadFontCollection(fontPath).Span[0];

            // HBFace above selects collection face zero. Adding only that face prevents
            // family/style resolution from selecting another face in the collection.
            FontFamily family = ((IFontMetricsCollection)fontCollection).AddMetrics(metrics, CultureInfo.InvariantCulture);
            font = family.CreateFont(shapingSize, metrics.Description.Style, variations);
        }
        else
        {
            font = fontCollection.Add(fontPath).CreateFont(shapingSize, variations);
        }

        ShapingTag[] featureTags = features
            .Where(f => f.Value != 0)
            .Select(f => ShapingTag.Parse(f.Tag.ToString()))
            .ToArray();

        TextShapingBuffer shapingBuffer = new();
        shapingBuffer.Add(text);
        shapingBuffer.TextDirection = buffer.Direction is Direction.RightToLeft or Direction.BottomToTop
            ? TextDirection.RightToLeft
            : TextDirection.LeftToRight;

        if (requestedDirection is Direction.TopToBottom or Direction.BottomToTop)
        {
            shapingBuffer.LayoutMode = LayoutMode.VerticalLeftRight;
        }

        shapingBuffer.Script = ReadScriptClass(requestedScript);
        if (language.Length != 0)
        {
            shapingBuffer.Language = CultureInfo.GetCultureInfo(language.Replace('_', '-'));
        }

        if (features.Any(f => f.Tag.ToString() == "kern" && f.Value == 0))
        {
            shapingBuffer.KerningMode = KerningMode.None;
        }

        TextShaper.ShapeRun(font, shapingBuffer, featureTags);

        if (expectedOutput == "*")
        {
            // The corpus uses an asterisk to check that shaping completes.
            return;
        }

        if (expectedOutput is not null)
        {
            // Compare the glyph sequence and geometry. Compare cluster indices only
            // at the default cluster level. Remove flags used for HarfBuzz buffer reuse.
            bool compareClusters = ReadOptionValue(options, "--cluster-level") is "" or "0";
            string geometry = options.Contains("--show-flags", StringComparison.Ordinal)
                ? Regex.Replace(expectedOutput, compareClusters ? "#[0-9]+" : "[=#][0-9]+", string.Empty)
                : expectedOutput;

            if (Path.GetFileName(fontPath) == "e3fbfe1110e277b89dc44ad184c4362b6764b237.ttf")
            {
                // HarfBuzz's OpenType font functions omit outline points. Fonts
                // aligns base point 1 with acute point 0. The point deltas are
                // (400, 580) for A and (590, 860) for B at wght=1.
                // Subtract the acute's pen position and, in vertical text,
                // express the result relative to its vertical origin.
                geometry = (options, codePoints) switch
                {
                    ("--font-funcs=ot", "U+0041,U+0301") => "[A=0+600|acute=0@-200,580+200]",
                    ("--font-funcs=ot --variations=wght=1", "U+0042,U+0301") => "[B=0+600|acute=0@-10,860+200]",
                    ("--font-funcs=ot --direction=ttb", "U+0041,U+0301") => "[A=0@-300,-800+0,-1000|acute=0@100,780+0,-1000]",
                    _ => geometry
                };
            }

            string actual = DescribeAat(shapingBuffer, referenceFont, options, text, compareClusters);
            if (geometry.Contains("+-", StringComparison.Ordinal))
            {
                // A negative reference pen step can be represented by shorter
                // neighboring advances. Check the absolute origin of every glyph
                // and the final pen position to verify that the kerning is intact.
                Assert.Equal(DescribeAatOrigins(geometry), DescribeAatOrigins(actual));
            }
            else
            {
                Assert.Equal(geometry, actual);
            }

            return;
        }

        CompareGlyphs(buffer, shapingBuffer);
    }

    /// <summary>
    /// Checks AAT behaviors with fonts made for the Fonts regression suite.
    /// </summary>
    /// <param name="fontName">The name of the test font file.</param>
    /// <param name="options">The shaping options in corpus format.</param>
    /// <param name="codePoints">The input code points in corpus format.</param>
    /// <param name="expected">The expected glyph output in corpus format.</param>
    [Theory]
    [InlineData("kerx-attachment-chain.ttf", "--no-glyph-names", "U+0041,U+0041,U+0041", "[1=0@1280,200+600|1=1@640,100+600|1=2+600]")]
    [InlineData("kerx-attachment-chain.ttf", "--no-glyph-names --direction=ttb", "U+0041,U+0041,U+0041", "[1=0@-220,-2100+0,-1000|1=1@-260,-1200+0,-1000|1=2@-300,-300+0,-1000]")]
    [InlineData("kerx-attachment-chain.ttf", "--no-glyph-names --direction=btt", "U+0041,U+0041,U+0041", "[1=2@-220,-2100+0,-1000|1=1@-260,-1200+0,-1000|1=0@-300,-300+0,-1000]")]
    [InlineData("kerx-shared-gpos-lookup.ttf", "--no-glyph-names", "U+0041,U+0056", "[1=0+560|2=1+600]")]
    [InlineData("morx-specialized-mark.ttf", "--no-glyph-names", "U+1780,U+17B7", "[2=0+600|3=0+200]")]
    [InlineData("morx-specialized-mark.ttf", "--no-glyph-names", "U+0628,U+064E", "[3=0@250,162+0|2=0+600]")]
    public void ShapesAatRegression(string fontName, string options, string codePoints, string expected)
        => this.CompareShape(Path.Combine(TestEnvironment.FontTestDataFullPath, "Issues", fontName), options, codePoints, expected);

    /// <summary>
    /// An empty morx table takes precedence over an active mort table.
    /// </summary>
    [Fact]
    public void ShapeRun_EmptyMorx_TakesPrecedenceOverMort()
    {
        const string text = "AB";
        string path = Path.Combine(CorpusRoots[2], "fonts", "TestAATMort.ttf");
        Font mortFont = new FontCollection().Add(path).CreateFont(1000);
        TextShapingBuffer mort = new();
        mort.Add(text);
        TextShaper.ShapeRun(mortFont, mort);

        // The MORT-1 fixture swaps the nominal glyphs for A and B.
        Assert.Equal(2, mort.Glyphs.Length);
        Assert.Equal((ushort)3, mort.Glyphs[0].GlyphId);
        Assert.Equal((ushort)2, mort.Glyphs[1].GlyphId);

        using FileStream source = File.OpenRead(path);
        using FontReader reader = new(source);
        using MemoryStream stream = new();
        BigEndianBinaryWriter writer = new(stream);

        // An empty morx contains a 32-bit version and a zero chain count.
        // The zero count leaves the version word as the table checksum.
        TableHeader[] headers = [.. reader.Headers.Values, new("morx", 0x00020000, 0, 8)];
        Array.Sort(headers, static (left, right) => string.CompareOrdinal(left.Tag, right.Tag));

        // The sfnt search range covers the largest power of two that fits
        // in the directory. Each record occupies 16 bytes.
        int entrySelector = BitOperations.Log2((uint)headers.Length);
        int searchRange = 16 << entrySelector;
        writer.WriteTrueTypeFileHeader((ushort)headers.Length, (ushort)searchRange, (ushort)entrySelector, (ushort)((headers.Length * 16) - searchRange));

        // Write the tables at aligned offsets while keeping the directory in tag order.
        uint offset = (uint)(12 + (headers.Length * 16));
        foreach (TableHeader header in headers)
        {
            writer.WriteTableHeader(header.Tag, header.CheckSum, offset, header.Length);
            long nextEntry = stream.Position;
            stream.Position = offset;
            if (header.Tag == "morx")
            {
                writer.WriteUInt32(0x00020000); // Version 2 in the high 16 bits.
                writer.WriteUInt32(0); // No chains.
            }
            else
            {
                Assert.True(reader.TryGetTableData(ShapingTag.Parse(header.Tag), out ReadOnlyMemory<byte> data));
                stream.Write(data.Span);
            }

            stream.Position = nextEntry;

            // SFNT tables start on four-byte boundaries. Add three and clear
            // the low two bits to round the end up to the next boundary.
            offset = (offset + header.Length + 3) & ~3U;
        }

        stream.Position = 0;
        Font combinedFont = new FontCollection().Add(stream).CreateFont(1000);
        TextShapingBuffer combined = new();
        combined.Add(text);
        TextShaper.ShapeRun(combinedFont, combined);

        Assert.Equal(2, combined.Glyphs.Length);
        Assert.Equal((ushort)2, combined.Glyphs[0].GlyphId);
        Assert.Equal((ushort)3, combined.Glyphs[1].GlyphId);
    }

    /// <summary>
    /// Resolves ordinary kerx and legacy pairs from the same font.
    /// </summary>
    /// <param name="first">The first character in the pair.</param>
    /// <param name="second">The second character in the pair.</param>
    /// <param name="expected">The expected design-unit adjustment.</param>
    [Theory]
    [InlineData('A', 'V', -23)]
    [InlineData('V', 'A', -31)]
    public void TryGetKerningOffset_KerxAndKern_UsesApplicablePair(char first, char second, int expected)
    {
        Font font = new FontCollection().Add(Path.Combine(TestEnvironment.FontTestDataFullPath, "Issues", "kerx-attachment-chain.ttf")).CreateFont(1000);
        Assert.True(font.TryGetGlyphs(new CodePoint(first), out Glyph? left));
        Assert.True(font.TryGetGlyphs(new CodePoint(second), out Glyph? right));

        // A/V has a backwards kerx pair that is skipped, so its legacy pair wins.
        // V/A has only an ordinary kerx pair.
        Assert.True(font.TryGetKerningOffset(left.Value, right.Value, 72, out Vector2 adjustment));
        Assert.Equal(new Vector2(expected, 0), adjustment);
    }

    /// <summary>
    /// Asserts that the corpus is present, so a missing submodule fails loudly
    /// instead of reducing the theory to nothing and passing.
    /// </summary>
    [Fact]
    public void CorpusIsPresent()
    {
        foreach (string root in CorpusRoots)
        {
            Assert.True(Directory.Exists(root), $"The corpus is missing from '{root}'.");
            Assert.NotEmpty(Directory.GetFiles(Path.Combine(root, "fonts")));
        }

        List<string> files = EnumerateCorpusFiles();
        Assert.Contains(files, static file => Path.GetFileName(file) == "aat-kerx.tests");
        Assert.Contains(files, static file => Path.GetFileName(file) == "aat-morx.tests");
        Assert.Contains(files, static file => Path.GetFileName(file).StartsWith("MORT-", StringComparison.Ordinal));
        Assert.Contains(files, static file => Path.GetFileName(file).StartsWith("MORX-", StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets the directories with corpus data in the submodule.
    /// </summary>
    private static string[] CorpusRoots
    {
        get
        {
            string data = Path.Combine(TestEnvironment.SolutionDirectoryFullPath, "tests", "harfbuzz", "test", "shape", "data");
            return [Path.Combine(data, "in-house"), Path.Combine(data, "aots"), Path.Combine(data, "text-rendering-tests")];
        }
    }

    /// <summary>
    /// Enumerates the corpus files carrying cases this library is meant to match.
    /// </summary>
    /// <returns>The corpus file paths.</returns>
    private static List<string> EnumerateCorpusFiles()
    {
        List<string> files = [];
        foreach (string root in CorpusRoots)
        {
            string directory = Path.Combine(root, "tests");
            if (!Directory.Exists(directory))
            {
                continue;
            }

            bool textRenderingTests = Path.GetFileName(root) == "text-rendering-tests";
            foreach (string file in Directory.EnumerateFiles(directory, "*.tests"))
            {
                string name = Path.GetFileName(file);
                if (textRenderingTests && !IsMetamorphosisTestFile(name))
                {
                    continue;
                }

                // This issue covers metamorphosis and extended kerning. The
                // other AAT corpus files require separate table support.
                if (!name.StartsWith("aat-", StringComparison.OrdinalIgnoreCase) ||
                    name is "aat-morx.tests" or "aat-mort.tests" or "aat-kerx.tests")
                {
                    files.Add(file);
                }
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

#if HARFBUZZSHARP_14_2_1_1
    /// <summary>
    /// Finds verified OpenType reference errors in HarfBuzzSharp 14.2.1.1.
    /// </summary>
    /// <param name="font">The name of the font file.</param>
    /// <param name="options">The shaping options in the corpus row.</param>
    /// <param name="input">The code points in the corpus row.</param>
    /// <returns>The skip reason, or <see langword="null"/> when the case can use the reference.</returns>
    private static string? GetReferenceSkipReason(string font, string options, string input)
        => (font, options, input) switch
        {
            ("arabic-fallback-missing-component.ttf", "", "U+0644,U+0645,U+0644,U+0647" or "U+0644,U+0644,U+0647")
                => "HarfBuzzSharp 14.2.1.1 counts components from unmatched Arabic fallback ligatures.",
            ("ba51123c80fa9526c9c3032cd41c22700a4bc3e2.ttf", "--show-extents", "U+1E49,0306")
                => "HarfBuzzSharp 14.2.1.1 computes a different mark offset for this COLR glyph.",
            ("8c6ec4f0065bbd81884c0fb46aa88929a5d42f13.ttf", "", "U+11005,U+11038")
                => "HarfBuzzSharp 14.2.1.1 does not initialize the properties of the inserted Brahmi dotted circle.",
            ("a9fff8bd6db3b7426dac8e7eb74431145d5dc38f.ttf", "--variations=wght=0.25" or "--variations=wght=0.75", "U+0041,U+0301")
                => "HarfBuzzSharp 14.2.1.1 rounds the two variable anchors before calculating the mark offset.",
            ("AdobeBlank2.ttf", "--no-glyph-names --no-positions --font-funcs=ot",
                "U+11B0A,U+093C" or "U+11B0A,U+093F" or "U+11B0A,U+094C" or "U+11B0A,U+094D,U+0915"
                or "U+11DF1,U+09BC" or "U+11DF1,U+09BF" or "U+11DF1,U+09BE")
                => "HarfBuzzSharp 14.2.1.1 predates the Unicode 18 Indic data.",
            ("e8ab562f44ca0ab7cb4fd758c0dceaae33fc6869.ttf", "--no-positions",
                "U+11D46,U+2015,U+11D31" or "U+11D46,U+2022,U+11D31" or "U+11D46,U+25FB,U+11D31"
                or "U+11D46,U+25FC,U+11D31" or "U+11D46,U+25FD,U+11D31" or "U+11D46,U+25FE,U+11D31")
                => "HarfBuzzSharp 14.2.1.1 lacks the USE generic-base fix.",

            // Match the exact repeated input so other rows that use these
            // fonts still compare with the live reference.
            ("8116e5d8fedfbec74e45dc350d2416d810bed8c4.ttf", "--no-positions", _)
                when input == string.Concat(Enumerable.Repeat("U+091F,U+094D,", 33)) + "U+091F,U+093F"
                => "HarfBuzzSharp 14.2.1.1 reports incorrect clusters in this long Indic syllable.",
            ("a17e599cdb54f45957c1c14e6b6a623bd69d5cd0.ttf", "--no-positions", _)
                when input == "U+1015,U+103C," + string.Concat(Enumerable.Repeat("U+1000,", 15)) + "U+102F"
                => "HarfBuzzSharp 14.2.1.1 applies a lookup across Myanmar syllables.",
            _ => null
        };
#endif

    /// <summary>
    /// Identifies the mort and morx cases in the text rendering tests.
    /// </summary>
    /// <param name="name">The name of the corpus test file.</param>
    /// <returns><see langword="true"/> if the file name starts with MORT- or MORX-.</returns>
    private static bool IsMetamorphosisTestFile(string name)
        => name.StartsWith("MORT-", StringComparison.Ordinal) || name.StartsWith("MORX-", StringComparison.Ordinal);

    /// <summary>
    /// Finds the font for a corpus case.
    /// When the case specifies a hash, the font file must match it.
    /// </summary>
    /// <param name="corpusFile">The path of the corpus test file.</param>
    /// <param name="fontReference">The font path and optional file hash.</param>
    /// <returns>The path of a matching font file, or <see langword="null"/> if none exists.</returns>
    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Matches the fixture identity supplied by the HarfBuzz corpus.")]
    private static string? ResolveFontPath(string corpusFile, string fontReference)
    {
        int version = fontReference.LastIndexOf('@');
        string fontName = version < 0 ? fontReference : fontReference[..version];
        string path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(corpusFile)!, fontName));
        if (!File.Exists(path) && Path.IsPathRooted(fontName))
        {
            path = Path.Combine(TestEnvironment.FontTestDataFullPath, Path.GetFileName(fontName));
        }

        if (!File.Exists(path))
        {
            return null;
        }

        if (version >= 0)
        {
            using FileStream stream = File.OpenRead(path);
            string hash = Convert.ToHexString(SHA1.HashData(stream));
            if (!hash.AsSpan().Equals(fontReference.AsSpan(version + 1), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return path;
    }

    /// <summary>
    /// Compares the glyph IDs, clusters, offsets, and advances with the live OpenType result.
    /// </summary>
    /// <param name="reference">The live HarfBuzz result.</param>
    /// <param name="actual">The Fonts result.</param>
    private static void CompareGlyphs(HBBuffer reference, TextShapingBuffer actual)
    {
        ReadOnlySpan<GlyphInfo> referenceGlyphs = reference.GetGlyphInfoSpan();
        ReadOnlySpan<GlyphPosition> referencePositions = reference.GetGlyphPositionSpan();
        ReadOnlySpan<ShapedGlyph> actualGlyphs = actual.Glyphs;
        Assert.Equal(referenceGlyphs.Length, actualGlyphs.Length);

        for (int i = 0; i < referenceGlyphs.Length; i++)
        {
            GlyphInfo expected = referenceGlyphs[i];
            GlyphPosition position = referencePositions[i];
            ShapedGlyph glyph = actualGlyphs[i];

            Assert.Equal(expected.Codepoint, (uint)glyph.GlyphId);

            // Both engines report UTF-16 indices.
            Assert.Equal((int)expected.Cluster, glyph.StringIndex);
            Assert.Equal(new Vector2(position.XOffset, position.YOffset), glyph.Offset);
            Assert.Equal(new Vector2(position.XAdvance, position.YAdvance), new Vector2(glyph.AdvanceWidth, glyph.AdvanceHeight));
        }
    }

    /// <summary>
    /// Formats every visible field requested by an existing AAT corpus case.
    /// Glyph names come from the font and do not require a shaping call.
    /// </summary>
    /// <param name="buffer">The shaped glyphs to format.</param>
    /// <param name="font">The font that supplies the glyph names.</param>
    /// <param name="options">The flags that select the output fields.</param>
    /// <param name="text">The input text used to map the cluster indices.</param>
    /// <param name="compareClusters">Whether to include cluster indices when the options permit them.</param>
    /// <returns>The glyph output in corpus format.</returns>
    private static string DescribeAat(TextShapingBuffer buffer, HBFont font, string options, string text, bool compareClusters)
    {
        bool useNames = !options.Contains("--no-glyph-names", StringComparison.Ordinal);
        bool usePositions = !options.Contains("--no-positions", StringComparison.Ordinal);
        bool noExtraData = options.Contains("--ned", StringComparison.Ordinal);
        bool useClusters = compareClusters && !noExtraData && !options.Contains("--no-clusters", StringComparison.Ordinal);
        bool useAdvances = !noExtraData && !options.Contains("--no-advances", StringComparison.Ordinal);
        StringBuilder builder = new("[");
        float penX = 0;
        float penY = 0;
        foreach (ShapedGlyph glyph in buffer.Glyphs)
        {
            if (builder.Length > 1)
            {
                builder.Append('|');
            }

            if (useNames)
            {
                builder.Append(font.TryGetGlyphName(glyph.GlyphId, out string? name) ? name : $"gid{glyph.GlyphId}");
            }
            else
            {
                builder.Append(CultureInfo.InvariantCulture, $"{glyph.GlyphId}");
            }

            if (useClusters)
            {
                // hb-shape's corpus syntax numbers Unicode scalars. The
                // managed shaper always reports an index into the UTF-16 input.
                int cluster = 0;
                SpanCodePointEnumerator scalars = text.AsSpan(0, glyph.StringIndex).EnumerateCodePoints();
                while (scalars.MoveNext())
                {
                    cluster++;
                }

                builder.Append(CultureInfo.InvariantCulture, $"={cluster}");
            }

            if (usePositions)
            {
                float x = glyph.Offset.X + (useAdvances ? 0 : penX);
                float y = glyph.Offset.Y + (useAdvances ? 0 : penY);
                if (x != 0 || y != 0)
                {
                    builder.Append(CultureInfo.InvariantCulture, $"@{x},{y}");
                }

                if (useAdvances)
                {
                    builder.Append(CultureInfo.InvariantCulture, $"+{glyph.AdvanceWidth}");
                    if (glyph.AdvanceHeight != 0)
                    {
                        builder.Append(CultureInfo.InvariantCulture, $",{glyph.AdvanceHeight}");
                    }
                }
            }

            penX += glyph.AdvanceWidth;
            penY += glyph.AdvanceHeight;
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Converts corpus offsets and advances into absolute glyph origins and a final pen position.
    /// </summary>
    /// <param name="geometry">The glyph positions in corpus notation.</param>
    /// <returns>The glyph origins and final pen position in font design units.</returns>
    private static string DescribeAatOrigins(string geometry)
    {
        int penX = 0;
        int penY = 0;
        string origins = AatPositionPattern().Replace(geometry, match =>
        {
            int xOffset = match.Groups[1].Success ? int.Parse(match.Groups[1].ValueSpan, CultureInfo.InvariantCulture) : 0;
            int yOffset = match.Groups[2].Success ? int.Parse(match.Groups[2].ValueSpan, CultureInfo.InvariantCulture) : 0;
            int x = penX + xOffset;
            int y = penY + yOffset;
            penX += int.Parse(match.Groups[3].ValueSpan, CultureInfo.InvariantCulture);
            penY += match.Groups[4].Success ? int.Parse(match.Groups[4].ValueSpan, CultureInfo.InvariantCulture) : 0;
            return FormattableString.Invariant($"@{x},{y}");
        });

        return FormattableString.Invariant($"{origins}+{penX},{penY}");
    }

    /// <summary>
    /// Matches a glyph's optional offset and required advance in corpus notation.
    /// </summary>
    /// <returns>The pattern for one glyph's positioning values.</returns>
    [GeneratedRegex(@"(?:@(-?\d+),(-?\d+))?\+(-?\d+)(?:,(-?\d+))?(?=\||\])")]
    private static partial Regex AatPositionPattern();

    /// <summary>
    /// Reads a corpus case's text, written as a comma separated list of scalar
    /// values.
    /// </summary>
    /// <param name="field">The field holding the list.</param>
    /// <param name="text">When this method returns, contains the text.</param>
    /// <returns><see langword="true"/> when the field could be read.</returns>
    private static bool TryReadText(string field, out string text)
    {
        StringBuilder builder = new();
        foreach (string value in field.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string hex = value.Trim();
            if (hex.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex[2..];
            }

            // A case may name a value that is not a character: the corpus carries
            // deliberately malformed input. Anything past the last scalar value, and
            // anything in the surrogate range, is not a character the text can hold,
            // and asking for one throws rather than returning a bad string.
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int scalar)
                || scalar > LastScalarValue
                || (scalar >= FirstSurrogate && scalar <= LastSurrogate))
            {
                text = string.Empty;
                return false;
            }

            builder.Append(char.ConvertFromUtf32(scalar));
        }

        text = builder.ToString();
        return text.Length > 0;
    }

    /// <summary>
    /// Reads the features a case asks for.
    /// </summary>
    /// <param name="options">The case's options.</param>
    /// <returns>The features.</returns>
    private static List<Feature> ReadFeatures(string options)
    {
        List<Feature> features = [];
        string specification = ReadOptionValue(options, "--features");
        if (specification.Length == 0)
        {
            return features;
        }

        foreach (string entry in specification.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Trim().Split('=');
            string name = parts[0].TrimStart('+', '-');
            uint value = parts[0].StartsWith('-') || (parts.Length == 2 && parts[1] == "0") ? 0U : 1U;
            features.Add(new Feature(new HBTag(name[0], name[1], name[2], name[3]), value, 0, uint.MaxValue));
        }

        return features;
    }

    /// <summary>
    /// Determines whether a corpus case asks for shaping controls this API cannot
    /// express on both engines.
    /// </summary>
    /// <param name="options">The corpus command-line options.</param>
    /// <param name="isAat">Whether the case uses the AAT corpus options.</param>
    /// <param name="text">The input text being shaped.</param>
    /// <returns><see langword="true"/> when the case cannot be compared faithfully.</returns>
    private static bool HasUnsupportedOptions(string options, bool isAat, string text)
    {
        if (UnsupportedOptions.Any(o => options.Contains(o, StringComparison.Ordinal))
            || options.Contains("--font-funcs=ft", StringComparison.Ordinal))
        {
            return true;
        }

        if (options.Contains("--cluster-level", StringComparison.Ordinal)
            && (!isAat || !options.Contains("--show-flags", StringComparison.Ordinal)
                || ReadOptionValue(options, "--cluster-level") is not ("0" or "1" or "2" or "3")))
        {
            return true;
        }

        string language = ReadOptionValue(options, "--language");
        string size = ReadOptionValue(options, "--font-size");
        bool removeIgnorables = options.Contains("--remove-default-ignorables", StringComparison.Ordinal);
        if (!isAat && (language.Length > 0 || size.Length > 0 || removeIgnorables))
        {
            return true;
        }

        if (size.Length > 0 && (!int.TryParse(size, CultureInfo.InvariantCulture, out int fontSize) || fontSize <= 0))
        {
            return true;
        }

        if (language.Length > 0)
        {
            try
            {
                _ = CultureInfo.GetCultureInfo(language.Replace('_', '-'));
            }
            catch (CultureNotFoundException)
            {
                return true;
            }
        }

        if (removeIgnorables)
        {
            // A no-ignorable input produces the same shaping request whether or
            // not the native buffer is told to drop default ignorables.
            foreach (CodePoint cp in text.AsSpan().EnumerateCodePoints())
            {
                if (UnicodeUtility.IsDefaultIgnorableCodePoint((uint)cp.Value))
                {
                    return true;
                }
            }
        }

        string direction = ReadOptionValue(options, "--direction");
        if (direction.Length > 0
            && direction is not "l" and not "ltr" and not "r" and not "rtl"
            && !(isAat && direction == "ttb"))
        {
            return true;
        }

        string script = ReadOptionValue(options, "--script");
        if (script.Length > 0 && ReadScriptClass(script) is null)
        {
            return true;
        }

        if (!TryReadVariations(options, out HBVariation[] _, out FontVariation[] _))
        {
            return true;
        }

        string features = ReadOptionValue(options, "--features");
        foreach (string entry in features.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string feature = entry.Trim();
            if (isAat && feature is "-kern" or "kern=0")
            {
                continue;
            }

            // The public API enables whole-run feature tags. Disabling a feature,
            // selecting an alternate value, or limiting its character range would
            // apply a different request to the two engines.
            if (feature.StartsWith('-') || feature.Contains('['))
            {
                return true;
            }

            string[] parts = feature.TrimStart('+').Split('=');
            if (parts[0].Length != 4
                || (parts.Length > 1 && parts[1] != "1"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the design-space variation coordinates requested by a corpus case for
    /// both shaping engines.
    /// </summary>
    /// <param name="options">The corpus command-line options.</param>
    /// <param name="referenceVariations">When this method returns, contains the reference font coordinates.</param>
    /// <param name="variations">When this method returns, contains this library's font coordinates.</param>
    /// <returns><see langword="true"/> when every requested coordinate could be read.</returns>
    private static bool TryReadVariations(string options, out HBVariation[] referenceVariations, out FontVariation[] variations)
    {
        string specification = ReadOptionValue(options, "--variations");
        if (specification.Length == 0)
        {
            referenceVariations = [];
            variations = [];
            return true;
        }

        List<HBVariation> reference = [];
        List<FontVariation> actual = [];
        foreach (string entry in specification.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('=', 2);
            if (parts.Length != 2 || parts[0].Length != 4 || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                referenceVariations = [];
                variations = [];
                return false;
            }

            string tag = parts[0];
            reference.Add(new HBVariation { Tag = new HBTag(tag[0], tag[1], tag[2], tag[3]), Value = value });
            actual.Add(new FontVariation(tag, value));
        }

        referenceVariations = reference.ToArray();
        variations = actual.ToArray();
        return true;
    }

    /// <summary>
    /// Reads an explicitly requested shaping direction.
    /// </summary>
    /// <param name="options">The corpus command-line options.</param>
    /// <returns>The requested direction, or <see langword="null"/> when it is inferred.</returns>
    private static Direction? ReadDirection(string options)
    {
        string direction = ReadOptionValue(options, "--direction");
        return direction switch
        {
            "l" or "ltr" => Direction.LeftToRight,
            "r" or "rtl" => Direction.RightToLeft,
            "ttb" => Direction.TopToBottom,
            "btt" => Direction.BottomToTop,
            _ => null
        };
    }

    /// <summary>
    /// Maps the script spellings used by the current corpus to this library's
    /// shaping script values.
    /// </summary>
    /// <param name="script">The corpus script value.</param>
    /// <returns>The script value, or <see langword="null"/> when no supported script was requested.</returns>
    private static ScriptClass? ReadScriptClass(string script)
        => script switch
        {
            "Qaag" => ScriptClass.MyanmarZawgyi,
            _ => null
        };

    /// <summary>
    /// Reads the value following a corpus command-line option in either its equals
    /// or space-separated form.
    /// </summary>
    /// <param name="options">The complete option field.</param>
    /// <param name="name">The option name, including its leading dashes.</param>
    /// <returns>The unquoted value, or an empty string when the option is absent.</returns>
    private static string ReadOptionValue(string options, string name)
    {
        int index = options.IndexOf(name, StringComparison.Ordinal);
        if (index < 0)
        {
            return string.Empty;
        }

        int valueStart = index + name.Length;
        if (valueStart < options.Length && options[valueStart] == '=')
        {
            valueStart++;
        }
        else
        {
            while (valueStart < options.Length && options[valueStart] == ' ')
            {
                valueStart++;
            }
        }

        int valueEnd = options.IndexOf(' ', valueStart);
        if (valueEnd < 0)
        {
            valueEnd = options.Length;
        }

        return options[valueStart..valueEnd].Trim('"');
    }
}
