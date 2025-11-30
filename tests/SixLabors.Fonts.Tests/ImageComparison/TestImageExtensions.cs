// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SixLabors.Fonts.Tests.ImageComparison;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SixLabors.Fonts.Tests.TestUtilities;

public static class TestImageExtensions
{
    private static readonly PngEncoder Encoder = new()
    {
        CompressionLevel = PngCompressionLevel.BestCompression,
        FilterMethod = PngFilterMethod.Adaptive
    };

    public static string DebugSave(
        this Image image,
        string extension = null,
        [CallerMemberName] string test = "",
        params object[] properties)
    {
        string outputDirectory = TestEnvironment.ActualOutputDirectoryFullPath;
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string path = Path.Combine(outputDirectory, $"{test}{FormatTestDetails(properties)}.{extension ?? "png"}");
        image.Save(path, Encoder);

        return path;
    }

    public static void CompareToReference<TPixel>(
        this Image<TPixel> image,
        float percentageTolerance = 0.05F,
        string extension = null,
        [CallerMemberName] string test = "",
        params object[] properties)
        where TPixel : unmanaged, IPixel<TPixel>
    {
        string path = image.DebugSave(extension, test, properties: properties);
        string referencePath = path.Replace(TestEnvironment.ActualOutputDirectoryFullPath, TestEnvironment.ReferenceOutputDirectoryFullPath);

        if (!File.Exists(referencePath))
        {
            throw new FileNotFoundException($"The reference image file was not found: {referencePath}");
        }

        using Image<Rgba64> expected = Image.Load<Rgba64>(referencePath);
        ImageComparer comparer = ImageComparer.TolerantPercentage(percentageTolerance);
        ImageSimilarityReport report = comparer.CompareImagesOrFrames(expected, image);

        if (!report.IsEmpty)
        {
            throw new ImageDifferenceIsOverThresholdException(report);
        }
    }

    private static string FormatTestDetails(params object[] properties)
    {
        if (properties?.Any() != true)
        {
            return "-";
        }

        StringBuilder sb = new();
        return $"_{string.Join("-", properties.Select(FormatTestDetails))}";
    }

    public static string FormatTestDetails(object properties)
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
        else if (properties is Dictionary<string, object> dictionary)
        {
            return FormattableString.Invariant($"_{string.Join("-", dictionary.Select(x => FormattableString.Invariant($"{x.Key}_{x.Value}")))}_");
        }

        Type type = properties.GetType();
        TypeInfo info = type.GetTypeInfo();
        if (info.IsPrimitive || info.IsEnum || type == typeof(decimal))
        {
            return FormattableString.Invariant($"{properties}");
        }

        IEnumerable<PropertyInfo> runtimeProperties = type.GetRuntimeProperties();
        return FormattableString.Invariant($"_{string.Join("-", runtimeProperties.ToDictionary(x => x.Name, x => x.GetValue(properties)).Select(x => FormattableString.Invariant($"{x.Key}_{x.Value}")))}_");
    }
}
