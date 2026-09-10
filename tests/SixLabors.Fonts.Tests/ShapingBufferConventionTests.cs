// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text.RegularExpressions;

namespace SixLabors.Fonts.Tests;

/// <summary>
/// Source conventions for <see cref="ShapingBuffer"/> element access. Glyph records are
/// large structs stored in flat buffer storage: binding an element to a local without
/// <see langword="ref"/> silently copies it and discards subsequent writes, and
/// assigning a buffer element to an existing <see langword="ref"/> local stores through
/// the reference instead of rebinding it. Both mistakes compile cleanly and corrupt
/// shaping output at runtime, so these tests fail the build instead.
/// </summary>
public class ShapingBufferConventionTests
{
    /// <summary>
    /// Matches value-copy bindings of buffer elements, for example
    /// <c>GlyphShapingData x = buffer[i];</c>. Legal forms are
    /// <c>ref GlyphShapingData x = ref buffer[i];</c> or mutation through the indexer
    /// expression itself.
    /// </summary>
    private static readonly Regex CopyBinding = new(
        @"(?<!ref )GlyphShapingData\s+\w+\s*=\s*\w[\w.]*\[",
        RegexOptions.Compiled);

    /// <summary>
    /// Matches phase dispatch by buffer type test, which became meaningless when the
    /// substitution and positioning collections merged; the buffer's role property is
    /// the correct dispatch.
    /// </summary>
    private static readonly Regex TypeTestDispatch = new(
        @"is\s+(not\s+)?ShapingBuffer\b",
        RegexOptions.Compiled);

    public static TheoryData<string> SourceFiles()
    {
        TheoryData<string> data = [];
        foreach (string path in Directory.EnumerateFiles(GetSourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            // The buffer's own storage management copies records deliberately.
            if (Path.GetFileName(path) == "ShapingBuffer.cs")
            {
                continue;
            }

            data.Add(path);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SourceFiles))]
    public void BufferElementBindingsTakeRef(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            Assert.False(
                CopyBinding.IsMatch(line),
                $"Value-copy binding of a buffer element in {path}: '{line.Trim()}'. Bind with 'ref ... = ref ...[...]' or mutate through the indexer expression.");
        }
    }

    [Theory]
    [MemberData(nameof(SourceFiles))]
    public void NoPhaseDispatchByBufferType(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            Assert.False(
                TypeTestDispatch.IsMatch(line),
                $"Phase dispatch by buffer type test in {path}: '{line.Trim()}'. Use ShapingBuffer.Role instead.");
        }
    }

    private static string GetSourceRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SixLabors.Fonts.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new IOException("Unable to locate the repository root.");
        }

        return Path.Combine(directory.FullName, "src", "SixLabors.Fonts");
    }
}
