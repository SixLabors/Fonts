// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Reflection;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Tests.ImageComparison;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.Fonts.Tests.TestUtilities;

public static class TestImageExtensions
{
    public static string DebugSave(
        this Image image,
        string extension = null,
        [CallerMemberName] string test = "",
        object properties = null)
    {
        string outputDirectory = TestEnvironment.ActualOutputDirectoryFullPath;
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string path = Path.Combine(outputDirectory, $"{test}{FormatTestDetails(properties)}.{extension ?? "png"}");
        image.Save(path);

        return path;
    }

    public static void CompareToReference<TPixel>(
        this Image<TPixel> image,
        float percentageTolerance = 0F,
        string extension = null,
        [CallerMemberName] string test = "",
        object properties = null)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        string path = image.DebugSave(extension, test, properties: properties);
        string referencePath = path.Replace(TestEnvironment.ActualOutputDirectoryFullPath, TestEnvironment.ReferenceOutputDirectoryFullPath);

        if (!File.Exists(referencePath))
        {
            throw new FileNotFoundException($"The reference image file was not found: {referencePath}");
        }

        using Image<Rgba64> expected = Image.Load<Rgba64>(referencePath);
        TolerantImageComparer comparer = new(percentageTolerance / 100F);
        ImageSimilarityReport report = comparer.CompareImagesOrFrames(expected, image);

        if (!report.IsEmpty)
        {
            throw new ImageDifferenceIsOverThresholdException(report);
        }
    }

    private static string FormatTestDetails(object properties)
    {
        if (properties is null)
        {
            return "-";
        }

        if (properties is FormattableString fs)
        {
            return FormattableString.Invariant(fs);
        }
        else if (properties is string s)
        {
            return FormattableString.Invariant($"-{s}-");
        }

        IEnumerable<PropertyInfo> runtimeProperties = properties.GetType().GetRuntimeProperties();

        return FormattableString.Invariant($"_{string.Join(
            "-",
            runtimeProperties.ToDictionary(x => x.Name, x => x.GetValue(properties))
                .Select(x => FormattableString.Invariant($"{x.Key}_{x.Value}")))}_");
    }
}

