// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Globalization;
using System.Numerics;

namespace SixLabors.Fonts.Tests;

public class FontRectangleTests
{
    [Fact]
    public void DefaultConstructorTest()
        => Assert.Equal(default, FontRectangle.Empty);

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void NonDefaultConstructorTest(float x, float y, float width, float height)
    {
        FontRectangle rect1 = new(x, y, width, height);
        Vector2 p = new(x, y);
        Vector2 s = new(width, height);
        FontRectangle rect2 = new(p, s);

        Assert.Equal(rect1, rect2);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void FromLTRBTest(float left, float top, float right, float bottom)
    {
        FontRectangle expected = new(left, top, right - left, bottom - top);
        FontRectangle actual = FontRectangle.FromLTRB(left, top, right, bottom);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void DimensionsTest(float x, float y, float width, float height)
    {
        FontRectangle rect = new(x, y, width, height);
        Vector2 p = new(x, y);
        Vector2 s = new(width, height);

        Assert.Equal(p, rect.Location);
        Assert.Equal(s, rect.Size);
        Assert.Equal(x, rect.X);
        Assert.Equal(y, rect.Y);
        Assert.Equal(width, rect.Width);
        Assert.Equal(height, rect.Height);
        Assert.Equal(x, rect.Left);
        Assert.Equal(y, rect.Top);
        Assert.Equal(x + width, rect.Right);
        Assert.Equal(y + height, rect.Bottom);
    }

    [Fact]
    public void IsEmptyTest()
    {
        Assert.True(FontRectangle.Empty.IsEmpty);
        Assert.True(default(FontRectangle).IsEmpty);
        Assert.True(new FontRectangle(1, -2, -10, 10).IsEmpty);
        Assert.True(new FontRectangle(1, -2, 10, -10).IsEmpty);
        Assert.True(new FontRectangle(1, -2, 0, 0).IsEmpty);

        Assert.False(new FontRectangle(0, 0, 10, 10).IsEmpty);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(float.MaxValue, float.MinValue)]
    public void LocationSetTest(float x, float y)
    {
        Vector2 point = new(x, y);
        FontRectangle rect = new(point.X, point.Y, 10, 10);
        Assert.Equal(point, rect.Location);
        Assert.Equal(point.X, rect.X);
        Assert.Equal(point.Y, rect.Y);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(float.MaxValue, float.MinValue)]
    public void SizeSetTest(float x, float y)
    {
        Vector2 size = new(x, y);
        FontRectangle rect = new(10, 10, size.X, size.Y);
        Assert.Equal(size, rect.Size);
        Assert.Equal(size.X, rect.Width);
        Assert.Equal(size.Y, rect.Height);
    }

    [Theory]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void EqualityTest(float x, float y, float width, float height)
    {
        FontRectangle rect1 = new(x, y, width, height);
        FontRectangle rect2 = new(width, height, x, y);

        Assert.True(rect1 != rect2);
        Assert.False(rect1 == rect2);
        Assert.False(rect1.Equals(rect2));
        Assert.False(rect1.Equals((object)rect2));
    }

    [Fact]
    public void GetHashCodeTest()
    {
        FontRectangle rect1 = new(10, 10, 10, 10);
        FontRectangle rect2 = new(10, 10, 10, 10);
        Assert.Equal(rect1.GetHashCode(), rect2.GetHashCode());
        Assert.NotEqual(rect1.GetHashCode(), new FontRectangle(20, 10, 10, 10).GetHashCode());
        Assert.NotEqual(rect1.GetHashCode(), new FontRectangle(10, 20, 10, 10).GetHashCode());
        Assert.NotEqual(rect1.GetHashCode(), new FontRectangle(10, 10, 20, 10).GetHashCode());
        Assert.NotEqual(rect1.GetHashCode(), new FontRectangle(10, 10, 10, 20).GetHashCode());
    }

    [Theory]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void ContainsTest(float x, float y, float width, float height)
    {
        FontRectangle rect = new(x, y, width, height);
        float x1 = (x + width) / 2;
        float y1 = (y + height) / 2;
        Vector2 p = new(x1, y1);
        FontRectangle r = new(x1, y1, width / 2, height / 2);

        Assert.False(rect.Contains(x1, y1));
        Assert.False(rect.Contains(p));
        Assert.False(rect.Contains(r));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue / 2, float.MinValue / 2, float.MinValue / 2, float.MaxValue / 2)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void InflateTest(float x, float y, float width, float height)
    {
        FontRectangle rect = new(x, y, width, height);
        FontRectangle inflatedRect = new(x - width, y - height, width + (2 * width), height + (2 * height));

        rect = rect.Inflate(width, height);
        Assert.Equal(inflatedRect, rect);

        Vector2 s = new(x, y);
        inflatedRect = FontRectangle.Inflate(rect, x, y);

        rect = rect.Inflate(s);
        Assert.Equal(inflatedRect, rect);
    }

    [Theory]
    [InlineData(float.MaxValue, float.MinValue, float.MaxValue / 2, float.MinValue / 2)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void IntersectTest(float x, float y, float width, float height)
    {
        FontRectangle rect1 = new(x, y, width, height);
        FontRectangle rect2 = new(y, x, width, height);
        FontRectangle expectedRect = FontRectangle.Intersect(rect1, rect2);
        rect1 = rect1.Intersect(rect2);
        Assert.Equal(expectedRect, rect1);
        Assert.False(rect1.IntersectsWith(expectedRect));
    }

    [Fact]
    public void IntersectIntersectingRectsTest()
    {
        FontRectangle rect1 = new(0, 0, 5, 5);
        FontRectangle rect2 = new(1, 1, 3, 3F);
        FontRectangle expected = new(1, 1, 3, 3F);

        Assert.Equal(expected, FontRectangle.Intersect(rect1, rect2));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void UnionTest(float x, float y, float width, float height)
    {
        FontRectangle a = new(x, y, width, height);
        FontRectangle b = new(width, height, x, y);

        float x1 = Math.Min(a.X, b.X);
        float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
        float y1 = Math.Min(a.Y, b.Y);
        float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

        FontRectangle expectedRectangle = new(x1, y1, x2 - x1, y2 - y1);

        Assert.Equal(expectedRectangle, FontRectangle.Union(a, b));
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, 0, 0, float.MaxValue)]
    [InlineData(0, float.MinValue, float.MaxValue, 0)]
    public void OffsetTest(float x, float y, float width, float height)
    {
        FontRectangle r1 = new(x, y, width, height);
        FontRectangle expectedRect = new(x + width, y + height, width, height);
        Vector2 p = new(width, height);

        r1 = r1.Offset(p);
        Assert.Equal(expectedRect, r1);

        expectedRect = expectedRect.Offset(p);
        r1 = r1.Offset(width, height);
        Assert.Equal(expectedRect, r1);
    }

    [Fact]
    public void ToStringTest()
    {
        FontRectangle r = new(5, 5.1F, 1.3F, 1F);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, "FontRectangle [ X={0}, Y={1}, Width={2}, Height={3} ]", r.X, r.Y, r.Width, r.Height), r.ToString());
    }

    [Theory]
    [InlineData(float.MinValue, float.MaxValue, float.MaxValue, float.MaxValue)]
    [InlineData(float.MinValue, float.MaxValue, float.MaxValue, float.MinValue)]
    [InlineData(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MinValue, float.MaxValue, float.MinValue, float.MinValue)]
    [InlineData(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue)]
    [InlineData(float.MinValue, float.MinValue, float.MaxValue, float.MinValue)]
    [InlineData(float.MinValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MinValue, float.MinValue, float.MinValue, float.MinValue)]
    [InlineData(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue)]
    [InlineData(float.MaxValue, float.MaxValue, float.MaxValue, float.MinValue)]
    [InlineData(float.MaxValue, float.MaxValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue)]
    [InlineData(float.MaxValue, float.MinValue, float.MaxValue, float.MaxValue)]
    [InlineData(float.MaxValue, float.MinValue, float.MaxValue, float.MinValue)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue)]
    [InlineData(float.MaxValue, float.MinValue, float.MinValue, float.MinValue)]
    [InlineData(0, 0, 0, 0)]
    public void DeconstructTest(float x, float y, float width, float height)
    {
        (float dx, float dy, float dw, float dh) = new FontRectangle(x, y, width, height);

        Assert.Equal(x, dx);
        Assert.Equal(y, dy);
        Assert.Equal(width, dw);
        Assert.Equal(height, dh);
    }
}
