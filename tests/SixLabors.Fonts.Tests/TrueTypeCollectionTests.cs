// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class TrueTypeCollectionTests
    {
        [Fact]
        public void InstallViaPathReturnsDecription()
        {
            var suit = new FontCollection();
            IEnumerable<FontFamily> collectionFromPath = suit.InstallCollection(TestFonts.SimpleTrueTypeCollection, out IEnumerable<FontDescription> descriptions);

            Assert.Equal(2, descriptions.Count());
            FontFamily openSans = Assert.Single(collectionFromPath, x => x.Name == "Open Sans");
            FontFamily abFont = Assert.Single(collectionFromPath, x => x.Name == "SixLaborsSampleAB");

            Assert.Equal(2, descriptions.Count());
            FontDescription openSansDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "Open Sans");
            FontDescription abFontDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
        }

        [Fact]
        public void InstallViaPathInstallFontFileInstances()
        {
            var sut = new FontCollection();
            IEnumerable<FontFamily> collectionFromPath = sut.InstallCollection(TestFonts.SimpleTrueTypeCollection, out IEnumerable<FontDescription> descriptions);

            IEnumerable<IFontMetrics> allInstances = sut.Families.SelectMany(x => sut.FindAll(x.Name, CultureInfo.InvariantCulture));

            Assert.All(allInstances, i =>
            {
                FileFontMetrics font = Assert.IsType<FileFontMetrics>(i);
            });
        }

        [Fact]
        public void InstallViaStreamhReturnsDecription()
        {
            var suit = new FontCollection();
            IEnumerable<FontFamily> collectionFromPath = suit.InstallCollection(TestFonts.SSimpleTrueTypeCollectionData(), out IEnumerable<FontDescription> descriptions);

            Assert.Equal(2, collectionFromPath.Count());
            FontFamily openSans = Assert.Single(collectionFromPath, x => x.Name == "Open Sans");
            FontFamily abFont = Assert.Single(collectionFromPath, x => x.Name == "SixLaborsSampleAB");

            Assert.Equal(2, descriptions.Count());
            FontDescription openSansDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "Open Sans");
            FontDescription abFontDescription = Assert.Single(descriptions, x => x.FontNameInvariantCulture == "SixLaborsSampleAB regular");
        }
    }
}
