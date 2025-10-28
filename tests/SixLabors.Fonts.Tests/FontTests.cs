// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests;

public class FontTests
{
    [Fact]
    public void FontClass_DefaultFontFamilyThrowsException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new Font(default(FontFamily), 0F));

        Assert.Equal("family", ex.ParamName);
    }

    [Fact]
    public void FontClass_DefaultFontFamilyWithSizeThrowsException()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(
            () => new Font(default(FontFamily), 0F, FontStyle.Regular));

        Assert.Equal("family", ex.ParamName);
    }

    [Fact]
    public void FontClass_NullFontThrowsException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new Font(null, FontStyle.Regular));

        Assert.Equal("prototype", ex.ParamName);
    }

    [Fact]
    public void FontClass_NullWithSizeFontThrowsException()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(
            () => new Font(null, 0f, FontStyle.Regular));

        Assert.Equal("prototype", ex.ParamName);
    }

    [Fact]
    public void FontClassWithPath_SetProperties()
    {
        FontCollection collection = new();
        FontFamily family = collection.Add(TestFonts.CarterOneFile);
        Font font = new(family, 12);

        Assert.Equal("Carter One", font.Name);
        Assert.Equal(12, font.Size);
        Assert.Equal(FontStyle.Regular, font.RequestedStyle);
        Assert.False(font.IsBold);
        Assert.False(font.IsItalic);
        Assert.True(font.TryGetPath(out string path));
        Assert.NotNull(path);
    }

    [Fact]
    public void FontClassNoPath_SetProperties()
    {
        FontCollection collection = new();
        using Stream stream = TestFonts.CarterOneFileData();
        FontFamily family = collection.Add(stream);
        Font font = new(family, 12);

        Assert.Equal("Carter One", font.Name);
        Assert.Equal(12, font.Size);
        Assert.Equal(FontStyle.Regular, font.RequestedStyle);
        Assert.False(font.IsBold);
        Assert.False(font.IsItalic);
        Assert.False(font.TryGetPath(out string path));
        Assert.Null(path);
    }
}
