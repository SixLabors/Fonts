// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class FontCollectionTests
    {
        [Fact]
        public void AddViaPathReturnsDescription()
        {
            var sut = new FontCollection();

            FontFamily family = sut.Add(TestFonts.CarterOneFile, out FontDescription description);
            Assert.NotNull(description);
            Assert.Equal("Carter One", description.FontFamilyInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
            Assert.Equal(FontStyle.Regular, description.Style);
        }

        [Fact]
        public void AddViaPathAddFontFileInstances()
        {
            var sut = new FontCollection();
            FontFamily family = sut.Add(TestFonts.CarterOneFile, out FontDescription descriptions);

            IEnumerable<IFontMetrics> allInstances = ((IReadOnlyFontMetricsCollection)sut).GetAllMetrics(family.Name, CultureInfo.InvariantCulture);

            Assert.All(allInstances, i =>
            {
                FileFontMetrics font = Assert.IsType<FileFontMetrics>(i);
            });
        }

        [Fact]
        public void AddViaStreamReturnsDescription()
        {
            var sut = new FontCollection();
            using Stream s = TestFonts.CarterOneFileData();
            FontFamily family = sut.Add(s, out FontDescription description);
            Assert.NotNull(description);
            Assert.Equal("Carter One", description.FontFamilyInvariantCulture);
            Assert.Equal("Regular", description.FontSubFamilyNameInvariantCulture);
            Assert.Equal(FontStyle.Regular, description.Style);
        }

        [Fact]
        public void NotFoundThrowsCorrectException()
        {
            const string invalid = "qwerty";
            FontFamilyNotFoundException ex = Assert.Throws<FontFamilyNotFoundException>(
                () => new FontCollection().Get(invalid));

            Assert.Equal(invalid, ex.FontFamily);
        }

        [Fact]
        public void CanAddSystemFonts()
        {
            var collection = new FontCollection();

            Assert.False(collection.Families.Any());

            collection.AddSystemFontCollection();

            Assert.True(collection.Families.Any());
            Assert.Equal(collection.Families.Count(), SystemFonts.Families.Count());
        }
    }
}
