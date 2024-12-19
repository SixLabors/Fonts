// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Reflection;
using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Tests;

internal static class TestEnvironment
{
    private static readonly FileInfo TestAssemblyFile = new(typeof(TestEnvironment).GetTypeInfo().Assembly.Location);

    private const string SixLaborsSolutionFileName = "SixLabors.Fonts.sln";

    private const string ActualOutputDirectoryRelativePath = @"tests\Images\ActualOutput";

    private const string ReferenceOutputDirectoryRelativePath = @"tests\Images\ReferenceOutput";

    private const string UnicodeTestDataRelativePath = @"tests\UnicodeTestData\";

    private static readonly Lazy<string> SolutionDirectoryFullPathLazy = new(GetSolutionDirectoryFullPathImpl);

    internal static string SolutionDirectoryFullPath => SolutionDirectoryFullPathLazy.Value;

    /// <summary>
    /// Gets the correct full path to the Unicode TestData directory.
    /// </summary>
    internal static string UnicodeTestDataFullPath => GetFullPath(UnicodeTestDataRelativePath);

    /// <summary>
    /// Gets the correct full path to the Actual Output directory. (To be written to by the test cases.)
    /// </summary>
    internal static string ActualOutputDirectoryFullPath => GetFullPath(ActualOutputDirectoryRelativePath);

    /// <summary>
    /// Gets the correct full path to the Expected Output directory. (To compare the test results to.)
    /// </summary>
    internal static string ReferenceOutputDirectoryFullPath => GetFullPath(ReferenceOutputDirectoryRelativePath);

    internal static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    internal static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    internal static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    internal static bool Is64BitProcess => Environment.Is64BitProcess;

    internal static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;

    internal static Architecture ProcessArchitecture => RuntimeInformation.ProcessArchitecture;


    /// <summary>
    /// Gets a value indicating whether test execution runs on CI.
    /// </summary>
#if ENV_CI
    internal static bool RunsOnCI => true;
#else
    internal static bool RunsOnCI => false;
#endif

    private static string GetSolutionDirectoryFullPathImpl()
    {
        DirectoryInfo directory = TestAssemblyFile.Directory;

        while (directory?.EnumerateFiles(SixLaborsSolutionFileName).Any() == false)
        {
            try
            {
                directory = directory.Parent;
            }
            catch (Exception ex)
            {
                throw new DirectoryNotFoundException(
                    $"Unable to find  solution directory from {TestAssemblyFile} because of {ex.GetType().Name}!",
                    ex);
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException($"Unable to find  solution directory from {TestAssemblyFile}!");
            }
        }

        return directory.FullName;
    }

    private static string GetFullPath(string relativePath) =>
        Path.Combine(SolutionDirectoryFullPath, relativePath)
        .Replace('\\', Path.DirectorySeparatorChar);
}
