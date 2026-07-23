// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Resolves a <see cref="CultureInfo"/> to the OpenType language system tags used to
/// select a language system within a script's feature table.
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/languagetags"/>
/// </summary>
/// <remarks>
/// Candidates are ordered most specific first; callers select the first tag the font's
/// script table declares and fall back to the default language system otherwise.
/// The registry data half of this class is generated; see the UnicodeTrieGenerator
/// project.
/// </remarks>
internal sealed partial class OpenTypeLanguageTagMap
{
    /// <summary>
    /// The lazily-initialized ISO 639 code to language system tag map.
    /// </summary>
    private static readonly Lazy<Dictionary<string, Tag[]>> LazyMap = new(CreateIsoLanguageMap, isThreadSafe: true);

    /// <summary>
    /// Maps BCP 47 variant subtags to the language system tags the registry defines by
    /// cross reference rather than by ISO 639 code.
    /// </summary>
    private static readonly Dictionary<string, Tag> VariantTagMap = new(StringComparer.Ordinal)
    {
        { "fonipa", Tag.Parse("IPPH") }, // Phonetic transcription, IPA conventions
        { "fonnapa", Tag.Parse("APPH") }, // Phonetic transcription, Americanist conventions
        { "polyton", Tag.Parse("PGR ") }, // Polytonic Greek
        { "provenc", Tag.Parse("PRO ") }, // Provencal
        { "geok", Tag.Parse("KGE ") }, // Khutsuri Georgian
        { "syre", Tag.Parse("SYRE") }, // Syriac, Estrangela script variant
        { "syrj", Tag.Parse("SYRJ") }, // Syriac, Western script variant
        { "syrn", Tag.Parse("SYRN") }, // Syriac, Eastern script variant
        { "arevmda", Tag.Parse("HYE ") }, // Western Armenian
    };

    private static readonly Tag ZhsTag = Tag.Parse("ZHS ");

    private static readonly Tag ZhtTag = Tag.Parse("ZHT ");

    private static readonly Tag ZhhTag = Tag.Parse("ZHH ");

    /// <summary>
    /// Prevents a default instance of the <see cref="OpenTypeLanguageTagMap"/> class
    /// from being created.
    /// </summary>
    private OpenTypeLanguageTagMap()
    {
    }

    /// <summary>
    /// Resolves the candidate OpenType language system tags for the supplied culture,
    /// most specific first.
    /// </summary>
    /// <param name="culture">The culture to resolve.</param>
    /// <param name="tags">
    /// When this method returns, contains the candidate tags if any resolved; otherwise
    /// an empty array. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if any tags resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryGetTags(CultureInfo? culture, out Tag[] tags)
    {
        if (culture is null || string.IsNullOrEmpty(culture.Name))
        {
            // The invariant culture expresses no language preference: the default
            // language system applies.
            tags = [];
            return false;
        }

        List<Tag> candidates = [];
        string[] subtags = culture.Name.ToLowerInvariant().Split('-');

        // BCP 47 variant subtags override the language mapping entirely: the registry
        // defines these tags by variant, not by ISO code.
        for (int i = 1; i < subtags.Length; i++)
        {
            if (VariantTagMap.TryGetValue(subtags[i], out Tag variantTag))
            {
                AddDistinct(candidates, variantTag);
            }
        }

        string threeLetter = culture.ThreeLetterISOLanguageName.ToLowerInvariant();

        if (subtags[0] == "zh" || threeLetter is "zho" or "cmn")
        {
            // Chinese language system tags encode script and region rather than
            // language: Hong Kong and Macao conventions first where they apply, then
            // traditional for Hant or Taiwan, then simplified, with the remaining
            // registry tags as fallbacks below.
            bool traditional = false;
            bool hongKong = false;
            for (int i = 1; i < subtags.Length; i++)
            {
                traditional |= subtags[i] is "hant" or "tw";
                hongKong |= subtags[i] is "hk" or "mo";
            }

            if (hongKong)
            {
                AddDistinct(candidates, ZhhTag);
                AddDistinct(candidates, ZhtTag);
            }
            else if (traditional)
            {
                AddDistinct(candidates, ZhtTag);
            }
            else
            {
                AddDistinct(candidates, ZhsTag);
            }
        }

        // The registry lists ISO 639-3 codes; older rows also carry two letter forms.
        Dictionary<string, Tag[]> map = LazyMap.Value;
        if (map.TryGetValue(threeLetter, out Tag[]? mapped)
            || map.TryGetValue(culture.TwoLetterISOLanguageName.ToLowerInvariant(), out mapped))
        {
            foreach (Tag tag in mapped)
            {
                AddDistinct(candidates, tag);
            }
        }

        tags = [.. candidates];
        return tags.Length > 0;
    }

    private static void AddDistinct(List<Tag> candidates, Tag tag)
    {
        if (!candidates.Contains(tag))
        {
            candidates.Add(tag);
        }
    }
}
