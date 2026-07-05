// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Native;

/// <summary>
/// Dispatches system character matching to the current operating system font service.
/// </summary>
internal static class SystemFontMatcher
{
    /// <summary>
    /// Tries to get the operating system default font family name.
    /// </summary>
    /// <param name="familyName">The operating system default font family name.</param>
    /// <returns><see langword="true"/> if the operating system returned a default font family name; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDefaultFamilyName([NotNullWhen(true)] out string? familyName)
    {
        if (OperatingSystem.IsWindows())
        {
            return DirectWriteSystemFontMatcher.TryGetDefaultFamilyName(out familyName);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CoreTextSystemFontMatcher.TryGetDefaultFamilyName(out familyName);
        }

        if (OperatingSystem.IsLinux())
        {
            return FontConfigSystemFontMatcher.TryGetDefaultFamilyName(out familyName);
        }

        familyName = null;
        return false;
    }

    /// <summary>
    /// Tries to enumerate installed system font family names.
    /// </summary>
    /// <param name="checkForUpdates">Whether the platform should check for updates to the system font collection.</param>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if the operating system returned family names; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyNames(bool checkForUpdates, [NotNullWhen(true)] out string[]? familyNames)
    {
        if (OperatingSystem.IsWindows())
        {
            return DirectWriteSystemFontMatcher.TryGetFamilyNames(checkForUpdates, out familyNames);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CoreTextSystemFontMatcher.TryGetFamilyNames(out familyNames);
        }

        if (OperatingSystem.IsLinux())
        {
            return FontConfigSystemFontMatcher.TryGetFamilyNames(out familyNames);
        }

        familyNames = null;
        return false;
    }

    /// <summary>
    /// Tries to enumerate installed system font faces resolved to platform family names.
    /// </summary>
    /// <param name="checkForUpdates">Whether the platform should check for updates to the system font collection.</param>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if the operating system returned font faces; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyFaces(bool checkForUpdates, [NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        if (OperatingSystem.IsWindows())
        {
            return DirectWriteSystemFontMatcher.TryGetFamilyFaces(checkForUpdates, out faces);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CoreTextSystemFontMatcher.TryGetFamilyFaces(out faces);
        }

        if (OperatingSystem.IsLinux())
        {
            return FontConfigSystemFontMatcher.TryGetFamilyFaces(out faces);
        }

        faces = null;
        return false;
    }

    /// <summary>
    /// Tries to match a character to an installed system font.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested font family name, or <see langword="null"/>.</param>
    /// <param name="culture">The culture used for matching, or <see langword="null"/>.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if a matching system font was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryMatchCharacter(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out string? matchedFamilyName,
        out FontStyle matchedStyle)
    {
        if (OperatingSystem.IsWindows())
        {
            return DirectWriteSystemFontMatcher.TryMatchCharacter(
                codePoint,
                style,
                familyName,
                culture,
                out matchedFamilyName,
                out matchedStyle);
        }

        if (OperatingSystem.IsMacOS())
        {
            return CoreTextSystemFontMatcher.TryMatchCharacter(
                codePoint,
                style,
                familyName,
                culture,
                out matchedFamilyName,
                out matchedStyle);
        }

        if (OperatingSystem.IsLinux())
        {
            return FontConfigSystemFontMatcher.TryMatchCharacter(
                codePoint,
                style,
                familyName,
                culture,
                out matchedFamilyName,
                out matchedStyle);
        }

        matchedFamilyName = null;
        matchedStyle = default;
        return false;
    }
}
