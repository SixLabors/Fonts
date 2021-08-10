// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using Xunit;

namespace SixLabors.Fonts.Tests
{
    public class SystemFontCollectionTests
    {
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
            Assert.Equal(family, SystemFonts.Get(family.Name, family.Culture));

            SystemFonts.TryGet(family.Name, family.Culture, out FontFamily family2);

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
    }
}
