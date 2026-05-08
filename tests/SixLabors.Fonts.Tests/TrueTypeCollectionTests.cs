// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;

namespace SixLabors.Fonts.Tests;

public class TrueTypeCollectionTests
{
    [Fact]
    public void AddViaPathReturnsDescription()
    {
        FontCollection suit = new();
        ReadOnlyMemory<FontFamily> collectionFromPath = suit.AddCollection(TestFonts.SimpleTrueTypeCollection, out ReadOnlyMemory<FontDescription> descriptions);
        FontFamily[] families = collectionFromPath.ToArray();
        FontDescription[] descriptionArray = descriptions.ToArray();

        Assert.Equal(2, descriptions.Length);
        FontFamily openSans = Assert.Single(families, x => x.Name == "Open Sans");
        FontFamily abFont = Assert.Single(families, x => x.Name == "SixLaborsSampleAB");

        Assert.Equal(2, descriptions.Length);
        FontDescription openSansDescription = Assert.Single(descriptionArray, x => x.FontNameInvariantCulture == "Open Sans");
        FontDescription abFontDescription = Assert.Single(descriptionArray, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
    }

    [Fact]
    public void AddViaPathAddFontFileInstances()
    {
        FontCollection sut = new();
        _ = sut.AddCollection(TestFonts.SimpleTrueTypeCollection, out _);

        IEnumerable<FontMetrics> allInstances = sut.Families.SelectMany(x => ((IReadOnlyFontMetricsCollection)sut).GetAllMetrics(x.Name, CultureInfo.InvariantCulture));

        Assert.All(allInstances, i =>
        {
            FileFontMetrics font = Assert.IsType<FileFontMetrics>(i);
        });
    }

    [Fact]
    public void AddViaStreamReturnsDescription()
    {
        FontCollection suit = new();
        ReadOnlyMemory<FontFamily> collectionFromPath = suit.AddCollection(TestFonts.SSimpleTrueTypeCollectionData(), out ReadOnlyMemory<FontDescription> descriptions);
        FontFamily[] families = collectionFromPath.ToArray();
        FontDescription[] descriptionArray = descriptions.ToArray();

        Assert.Equal(2, collectionFromPath.Length);
        FontFamily openSans = Assert.Single(families, x => x.Name == "Open Sans");
        FontFamily abFont = Assert.Single(families, x => x.Name == "SixLaborsSampleAB");

        Assert.Equal(2, descriptions.Length);
        FontDescription openSansDescription = Assert.Single(descriptionArray, x => x.FontNameInvariantCulture == "Open Sans");
        FontDescription abFontDescription = Assert.Single(descriptionArray, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
    }
}
