// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.ImageSharp;

namespace SixLabors.Fonts.Tests.ImageComparison;

public class ImageDimensionsMismatchException : ImagesSimilarityException
{
    public ImageDimensionsMismatchException(Size expectedSize, Size actualSize)
        : base($"The image dimensions {actualSize} do not match the expected {expectedSize}!")
    {
        this.ExpectedSize = expectedSize;
        this.ActualSize = actualSize;
    }

    public Size ExpectedSize { get; }

    public Size ActualSize { get; }
}
