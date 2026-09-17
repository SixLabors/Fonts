// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Text;
using HarfBuzzSharp;
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
/// The corpus is a pinned submodule fetched by the generator project, so the inputs
/// and the reference version move only when the pin does. It holds the reference
/// implementation's own cases and the OpenType lookup test suite it carries, and
/// both are shaped.
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
/// The corpus also contains fonts this harness cannot compare: shaping-only
/// fixtures that are not complete, renderable OpenType fonts, and fonts whose
/// expected output uses unsupported Apple layout tables. Those fonts are listed
/// explicitly in <see cref="UnsupportedFontFiles"/> and are not emitted as tests.
/// </para>
/// </remarks>
public class HarfBuzzCorpusTests
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
        "--font-size",
        "--remove-default-ignorables",
        "--preserve-default-ignorables",
        "--cluster-level",
        "--bot",
        "--eot",
        "--shaper",
        "--language",
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

        // This deliberately damaged fixture reaches truncated glyph data for some inputs.
        "HarfBust.ttf",

        // HarfBuzz's "ot" shaper applies this font's Apple morx and kerx tables,
        // while this library does not implement Apple Advanced Typography.
        "LucidaGrande.ttc"
    };

    /// <summary>
    /// Gets every corpus case this library is expected to match.
    /// </summary>
    /// <returns>The font, the options, and the characters of each case.</returns>
    public static TheoryData<string, string, string> CorpusCases()
    {
        TheoryData<string, string, string> data = [];

        foreach (string file in EnumerateCorpusFiles())
        {
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

                if (HasUnsupportedOptions(parts[1]))
                {
                    continue;
                }

                string fontPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, parts[0]));
                if (!File.Exists(fontPath))
                {
                    continue;
                }

                // Shaping-only and damaged fixtures cannot satisfy the Font contract,
                // so they are excluded explicitly instead of hiding loader exceptions.
                if (UnsupportedFontFiles.Contains(Path.GetFileName(fontPath)))
                {
                    continue;
                }

                if (!TryReadText(parts[2], out string _))
                {
                    continue;
                }

                data.Add(fontPath, parts[1], parts[2]);
            }
        }

        return data;
    }

    /// <summary>
    /// Shapes one corpus case and requires the glyphs to be the reference
    /// implementation's, exactly.
    /// </summary>
    /// <param name="fontPath">The font to shape with.</param>
    /// <param name="options">The case's options, which may name features.</param>
    /// <param name="codePoints">The characters to shape.</param>
    [Theory]
    [MemberData(nameof(CorpusCases))]
    public void ShapesAsReferenceDoes(string fontPath, string options, string codePoints)
    {
        Assert.True(TryReadText(codePoints, out string text));

        List<Feature> features = ReadFeatures(options);

        using Blob blob = Blob.FromFile(fontPath);
        using HBFace face = new(blob, 0);
        using HBFont referenceFont = new(face);
        referenceFont.SetFunctionsOpenType();

        // The corpus positions are expressed at the face's em scale. Giving both
        // engines that size preserves direct, exact comparisons of their outputs.
        int shapingSize = (int)face.UnitsPerEm;
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

        buffer.GuessSegmentProperties();
        referenceFont.Shape(buffer, features, OpenTypeShaper);

        string expected = Describe(buffer);

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
        shapingBuffer.TextDirection = buffer.Direction == Direction.RightToLeft
            ? TextDirection.RightToLeft
            : TextDirection.LeftToRight;
        shapingBuffer.Script = ReadScriptClass(requestedScript);

        TextShaper.ShapeRun(font, shapingBuffer, featureTags);

        string actual = Describe(shapingBuffer);

        Assert.Equal(expected, actual);

        ReadOnlySpan<GlyphInfo> referenceGlyphs = buffer.GetGlyphInfoSpan();
        ReadOnlySpan<ShapedGlyph> actualGlyphs = shapingBuffer.Glyphs;
        for (int i = 0; i < referenceGlyphs.Length; i++)
        {
            // AddUtf16 reports char indices, which is the same contract exposed by
            // ShapedGlyph.StringIndex.
            Assert.Equal((int)referenceGlyphs[i].Cluster, actualGlyphs[i].StringIndex);
        }
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

        Assert.NotEmpty(EnumerateCorpusFiles());
    }

    /// <summary>
    /// Gets the roots of the corpus within the pinned submodule: the reference
    /// implementation's own cases, and the OpenType lookup test suite it carries.
    /// </summary>
    private static string[] CorpusRoots
    {
        get
        {
            string data = Path.Combine(TestEnvironment.SolutionDirectoryFullPath, "tests", "harfbuzz", "test", "shape", "data");
            return [Path.Combine(data, "in-house"), Path.Combine(data, "aots")];
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

            foreach (string file in Directory.EnumerateFiles(directory, "*.tests"))
            {
                // A file testing Apple Advanced Typography exclusively is left out whole:
                // none of its cases are in scope for an OpenType engine.
                if (!Path.GetFileName(file).StartsWith("aat", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(file);
                }
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }

    /// <summary>
    /// Describes the reference implementation's shaped glyphs.
    /// </summary>
    /// <param name="buffer">The shaped buffer.</param>
    /// <returns>One entry per glyph.</returns>
    private static string Describe(HBBuffer buffer)
    {
        ReadOnlySpan<GlyphInfo> infos = buffer.GetGlyphInfoSpan();
        ReadOnlySpan<GlyphPosition> positions = buffer.GetGlyphPositionSpan();

        StringBuilder builder = new();
        for (int i = 0; i < infos.Length; i++)
        {
            Append(builder, infos[i].Codepoint, positions[i].XOffset, positions[i].YOffset, positions[i].XAdvance);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Describes this library's shaped glyphs.
    /// </summary>
    /// <param name="buffer">The shaped buffer.</param>
    /// <returns>One entry per glyph.</returns>
    private static string Describe(TextShapingBuffer buffer)
    {
        ReadOnlySpan<ShapedGlyph> glyphs = buffer.Glyphs;

        StringBuilder builder = new();
        for (int i = 0; i < glyphs.Length; i++)
        {
            ShapedGlyph glyph = glyphs[i];
            Append(builder, glyph.GlyphId, glyph.Offset.X, glyph.Offset.Y, glyph.AdvanceWidth);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Appends one glyph in the notation the corpus itself uses, so a failure reads
    /// against the corpus file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The advance along the run's axis is compared; the advance across it is not.
    /// The reference leaves the cross-axis advance at zero for a horizontal run,
    /// while this library reports the font's line advance there, so comparing it
    /// would report a difference of convention on every glyph. Every corpus case is
    /// horizontal, so the axis compared is the horizontal one.
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder.</param>
    /// <param name="glyphId">The glyph.</param>
    /// <param name="xOffset">The horizontal offset.</param>
    /// <param name="yOffset">The vertical offset.</param>
    /// <param name="advance">The advance along the run's axis.</param>
    private static void Append(StringBuilder builder, uint glyphId, float xOffset, float yOffset, float advance)
    {
        if (builder.Length > 0)
        {
            builder.Append('|');
        }

        builder.Append(CultureInfo.InvariantCulture, $"{glyphId}");

        if (xOffset != 0 || yOffset != 0)
        {
            builder.Append(CultureInfo.InvariantCulture, $"@{xOffset},{yOffset}");
        }

        builder.Append(CultureInfo.InvariantCulture, $"+{advance}");
    }

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
            string name = entry.TrimStart('+').Split('=')[0];

            features.Add(new Feature(new HBTag(name[0], name[1], name[2], name[3]), 1, 0, uint.MaxValue));
        }

        return features;
    }

    /// <summary>
    /// Determines whether a corpus case asks for shaping controls this API cannot
    /// express on both engines.
    /// </summary>
    /// <param name="options">The corpus command-line options.</param>
    /// <returns><see langword="true"/> when the case cannot be compared faithfully.</returns>
    private static bool HasUnsupportedOptions(string options)
    {
        if (UnsupportedOptions.Any(o => options.Contains(o, StringComparison.Ordinal))
            || options.Contains("--font-funcs=ft", StringComparison.Ordinal))
        {
            return true;
        }

        string direction = ReadOptionValue(options, "--direction");
        if (direction.Length > 0
            && direction is not "l" and not "ltr" and not "r" and not "rtl")
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
    /// Reads an explicitly requested horizontal direction.
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
