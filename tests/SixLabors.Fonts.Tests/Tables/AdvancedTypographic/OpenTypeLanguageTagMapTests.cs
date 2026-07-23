// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using SixLabors.Fonts.Tables.AdvancedTypographic;

namespace SixLabors.Fonts.Tests.Tables.AdvancedTypographic;

public class OpenTypeLanguageTagMapTests
{
    [Theory]
    [InlineData("tr", "TRK ")]
    [InlineData("tr-TR", "TRK ")]
    [InlineData("de", "DEU ")]
    [InlineData("de-AT", "DEU ")]
    [InlineData("sr", "SRB ")]
    [InlineData("az", "AZE ")]
    [InlineData("hi", "HIN ")]
    [InlineData("fa", "FAR ")]
    [InlineData("el", "ELL ")]
    [InlineData("nn", "NYN ")]
    [InlineData("ro", "ROM ")]
    public void ResolvesRegistryTag(string cultureName, string expected)
    {
        Assert.True(OpenTypeLanguageTagMap.TryGetTags(new CultureInfo(cultureName), out Tag[] tags));
        Assert.Equal(Tag.Parse(expected), tags[0]);
    }

    [Theory]
    [InlineData("zh", "ZHS ")]
    [InlineData("zh-CN", "ZHS ")]
    [InlineData("zh-Hans", "ZHS ")]
    [InlineData("zh-Hans-SG", "ZHS ")]
    [InlineData("zh-Hant", "ZHT ")]
    [InlineData("zh-TW", "ZHT ")]
    [InlineData("zh-HK", "ZHH ")]
    [InlineData("zh-MO", "ZHH ")]
    [InlineData("zh-Hant-HK", "ZHH ")]
    public void ResolvesChineseByScriptAndRegion(string cultureName, string expected)
    {
        // Chinese language system tags encode script and region: simplified, traditional,
        // and the Hong Kong and Macao conventions must each win for their cultures.
        Assert.True(OpenTypeLanguageTagMap.TryGetTags(new CultureInfo(cultureName), out Tag[] tags));
        Assert.Equal(Tag.Parse(expected), tags[0]);
    }

    [Fact]
    public void ChineseHongKongFallsBackToTraditional()
    {
        Assert.True(OpenTypeLanguageTagMap.TryGetTags(new CultureInfo("zh-HK"), out Tag[] tags));

        // A font without ZHH should still get traditional forms before simplified ones.
        Assert.Equal(Tag.Parse("ZHH "), tags[0]);
        Assert.Equal(Tag.Parse("ZHT "), tags[1]);
    }

    [Fact]
    public void ResolvesVariantSubtag()
    {
        // Polytonic Greek is registered by BCP 47 variant subtag rather than ISO code and
        // must outrank the plain Greek mapping. The runtime requires a region between the
        // language and variant subtags and uppercases the variant in the culture name.
        Assert.True(OpenTypeLanguageTagMap.TryGetTags(new CultureInfo("el-GR-polyton"), out Tag[] tags));
        Assert.Equal(Tag.Parse("PGR "), tags[0]);
        Assert.Contains(Tag.Parse("ELL "), tags);
    }

    [Fact]
    public void ResolvesMacrolanguageSublanguage()
    {
        // Algerian Arabic has no registry row of its own; the IANA registry keys it to the
        // Arabic macrolanguage.
        Assert.True(OpenTypeLanguageTagMap.TryGetTags(new CultureInfo("aao"), out Tag[] tags));
        Assert.Equal(Tag.Parse("ARA "), tags[0]);
    }

    [Fact]
    public void InvariantCultureResolvesNoTags()
    {
        // The invariant culture expresses no language preference: the default language
        // system applies.
        Assert.False(OpenTypeLanguageTagMap.TryGetTags(CultureInfo.InvariantCulture, out Tag[] tags));
        Assert.Empty(tags);
    }

    [Fact]
    public void NullCultureResolvesNoTags()
    {
        Assert.False(OpenTypeLanguageTagMap.TryGetTags(null, out Tag[] tags));
        Assert.Empty(tags);
    }
}
