// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections;
using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tests;

public class SystemFontCollectionTests
{
    private static readonly SystemFontCollection SysFontCollection = new();

    [Fact]
    public void SystemFonts_IsPopulated()
    {
        Assert.True(SystemFonts.Collection.Families.Any());
        Assert.Equal(SystemFonts.Collection.Families, SystemFonts.Families);
    }

    [Fact]
    public void SystemFonts_CanGetFont()
    {
        FontFamily family = SystemFonts.Families.First();

        Assert.False(family == default);
        Assert.Equal(family, SystemFonts.Get(family.Name));

        SystemFonts.TryGet(family.Name, out FontFamily family2);
        Assert.Equal(family, family2);
    }

    [Fact]
    public void SystemFonts_CanGetFont_ByCulture()
    {
        FontFamily family = SystemFonts.Families.First();

        Assert.False(family == default);
        Assert.Equal(family, SystemFonts.GetByCulture(family.Name, family.Culture));

        SystemFonts.TryGetByCulture(family.Name, family.Culture, out FontFamily family2);

        Assert.Equal(family, family2);
        Assert.Contains(family, SystemFonts.GetByCulture(family.Culture));
    }

    [Fact]
    public void SystemFonts_CanCreateFont()
    {
        FontFamily family = SystemFonts.Families.First();
        Font font = SystemFonts.CreateFont(family.Name, 12F);

        Assert.NotNull(font);

        font = SystemFonts.CreateFont(family.Name, 12F, FontStyle.Regular);
        Assert.NotNull(font);
    }

    [Fact]
    public void SystemFonts_CanCreateFont_WithCulture()
    {
        FontFamily family = SystemFonts.Families.First();
        Font font = SystemFonts.CreateFont(family.Name, family.Culture, 12F);

        Assert.NotNull(font);

        font = SystemFonts.CreateFont(family.Name, family.Culture, 12F, FontStyle.Regular);
        Assert.NotNull(font);
    }

    [Fact]
    public void CanEnumerateSystemFontMetrics()
    {
        IEnumerator<FontMetrics> enumerator = ((IReadOnlyFontMetricsCollection)SysFontCollection).GetEnumerator();

        int count = 0;
        while (enumerator.MoveNext())
        {
            count++;
            Assert.NotNull(enumerator.Current);
        }

        Assert.True(count > 0);
    }

    [Fact]
    public void CanEnumerateNonGenericSystemFontMetrics()
    {
        IEnumerator enumerator = ((IReadOnlyFontMetricsCollection)SysFontCollection).GetEnumerator();

        int count = 0;
        while (enumerator.MoveNext())
        {
            count++;
            Assert.NotNull(enumerator.Current);
        }

        Assert.True(count > 0);
    }

    [Fact]
    public void CanGetAllMetricsByCulture()
    {
        IReadOnlyFontMetricsCollection collection = SysFontCollection;
        FontFamily family = SysFontCollection.Families.First();
        IEnumerable<FontMetrics> metrics = collection.GetAllMetrics(family.Name, family.Culture);

        Assert.True(metrics.Any());

        foreach (FontMetrics item in metrics)
        {
            Assert.True(family.TryGetMetrics(item.Description.Style, out FontMetrics familyMetrics));
            Assert.True(collection.TryGetMetrics(
                family.Name,
                family.Culture,
                item.Description.Style,
                out FontMetrics fontMetrics));

            Assert.Equal(familyMetrics, fontMetrics);
        }
    }

    [Fact]
    public void CanGetAllStylesByCulture()
    {
        FontFamily family = SysFontCollection.Families.First();
        ReadOnlyMemory<FontStyle> styles = ((IReadOnlyFontMetricsCollection)SysFontCollection).GetAllStyles(family.Name, family.Culture);

        Assert.False(styles.IsEmpty);
        Assert.Equal(family.GetAvailableStyles().ToArray(), styles.ToArray());
    }

    [Fact]
    [AppContextSwitch("Switch.SixLabors.Fonts.DoNotUseNativeSystemFontsEnumeration", true)]
    public void SystemFonts_FontFamilyNotFound_ThrowsWithSearchDirectories()
    {
        static void Action() => new SystemFontCollection().Get("AFontThatDoesNotExist");

        FontFamilyNotFoundException exception = Assert.Throws<FontFamilyNotFoundException>(Action);
        Assert.Equal("AFontThatDoesNotExist", exception.FontFamily);
        Assert.Contains("AFontThatDoesNotExist", exception.Message);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Contains(@"Windows\Fonts", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(exception.SearchDirectories, e => e.Contains(@"Windows\Fonts", StringComparison.OrdinalIgnoreCase));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Assert.Contains(@"/share/fonts/", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(exception.SearchDirectories, e => e.Contains("share/fonts", StringComparison.OrdinalIgnoreCase));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Contains(@"/Library/Fonts/", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(exception.SearchDirectories, e => e.Contains("Library/Fonts", StringComparison.OrdinalIgnoreCase));
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Android")))
        {
            Assert.Contains(@"/system/fonts/", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(exception.SearchDirectories, e => e.Contains("/system/fonts/", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void SystemFonts_FontFamilyNotFound_ThrowsWithoutSearchDirectories()
    {
        static void Action() => new SystemFontCollection().Get("AFontThatDoesNotExist");

        FontFamilyNotFoundException exception = Assert.Throws<FontFamilyNotFoundException>(Action);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.DoesNotContain("/Library/Fonts/", exception.Message);
            Assert.Empty(exception.SearchDirectories);
        }
    }
}
