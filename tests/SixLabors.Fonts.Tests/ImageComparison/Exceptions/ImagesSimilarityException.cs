// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tests.ImageComparison;

using System;

public class ImagesSimilarityException : Exception
{
    public ImagesSimilarityException(string message)
        : base(message)
    {
    }
}
