// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts;

/// <summary>
/// Provides a collection of fonts.
/// </summary>
public static class SystemFonts
{
    private static readonly Lazy<SystemFontCollection> LazySystemFonts = new(() => new SystemFontCollection(), true);

    /// <summary>
    /// Gets the collection containing the globally installed system fonts.
    /// </summary>
    public static IReadOnlySystemFontCollection Collection => LazySystemFonts.Value;

    /// <summary>
    /// Gets the collection of <see cref="FontFamily"/>s installed on current system.
    /// </summary>
    public static IEnumerable<FontFamily> Families => Collection.Families;

    /// <summary>
    /// Gets the names of font families installed on current system.
    /// </summary>
    /// <param name="checkForUpdates">Whether the operating system should check for updates to the system font collection.</param>
    /// <returns>The installed system font family names.</returns>
    public static string[] GetFamilyNames(bool checkForUpdates = false)
    {
        if (checkForUpdates && Native.SystemFontMatcher.TryGetFamilyFaces(checkForUpdates, out Native.NativeSystemFontFace[]? faces))
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

            foreach (Native.NativeSystemFontFace face in faces)
            {
                names.Add(face.FamilyName);
            }

            return [.. names];
        }

        List<string> collectionNames = [];

        foreach (FontFamily family in Families)
        {
            collectionNames.Add(family.Name);
        }

        return [.. collectionNames];
    }

    /// <summary>
    /// Gets the operating system default font family name.
    /// </summary>
    /// <returns>The default font family name.</returns>
    /// <exception cref="InvalidOperationException">No system font families are available.</exception>
    public static string GetDefaultFamilyName()
    {
        if (TryGetDefaultFamilyName(out string? familyName))
        {
            return familyName;
        }

        foreach (FontFamily family in Families)
        {
            return family.Name;
        }

        throw new InvalidOperationException("No system font families are available.");
    }

    /// <summary>
    /// Tries to get the operating system default font family name.
    /// </summary>
    /// <param name="familyName">The operating system default font family name.</param>
    /// <returns><see langword="true"/> when the operating system returned a default font family name.</returns>
    public static bool TryGetDefaultFamilyName([NotNullWhen(true)] out string? familyName)
        => Native.SystemFontMatcher.TryGetDefaultFamilyName(out familyName);

    /// <inheritdoc cref="IReadOnlyFontCollection.Get(string)"/>
    public static FontFamily Get(string name) => GetByCulture(name, CultureInfo.InvariantCulture);

    /// <inheritdoc cref="IReadOnlyFontCollection.TryGet(string, out FontFamily)" />
    public static bool TryGet(string fontFamily, out FontFamily family)
        => Collection.TryGet(fontFamily, out family);

    /// <summary>
    /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
    /// </summary>
    /// <param name="name">The font family name.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <returns>The new <see cref="Font"/>.</returns>
    public static Font CreateFont(string name, float size)
        => Collection.Get(name).CreateFont(size);

    /// <summary>
    /// Create a new instance of the <see cref="Font"/> for the named font family.
    /// </summary>
    /// <param name="name">The font family name.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <param name="style">The font style.</param>
    /// <returns>The new <see cref="Font"/>.</returns>
    public static Font CreateFont(string name, float size, FontStyle style)
        => Collection.Get(name).CreateFont(size, style);

    /// <inheritdoc cref="IReadOnlyFontCollection.GetByCulture(CultureInfo)"/>
    public static IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
        => Collection.GetByCulture(culture);

    /// <inheritdoc cref="IReadOnlyFontCollection.GetByCulture(string, CultureInfo)" />
    public static FontFamily GetByCulture(string fontFamily, CultureInfo culture)
        => Collection.GetByCulture(fontFamily, culture);

    /// <inheritdoc cref="IReadOnlyFontCollection.TryGetByCulture(string, CultureInfo, out FontFamily)" />
    public static bool TryGetByCulture(string fontFamily, CultureInfo culture, out FontFamily family)
        => Collection.TryGetByCulture(fontFamily, culture, out family);

    /// <inheritdoc cref="IReadOnlySystemFontCollection.TryMatchCharacter(CodePoint, FontStyle, string?, CultureInfo?, out FontMatch)" />
    public static bool TryMatchCharacter(CodePoint codePoint, FontStyle style, string? familyName, CultureInfo? culture, out FontMatch match)
        => Collection.TryMatchCharacter(codePoint, style, familyName, culture, out match);

    /// <summary>
    /// Create a new instance of the <see cref="Font"/> for the named font family with regular styling.
    /// </summary>
    /// <param name="name">The font family name.</param>
    /// <param name="culture">The font culture.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <returns>The new <see cref="Font"/>.</returns>
    public static Font CreateFont(string name, CultureInfo culture, float size)
        => Collection.GetByCulture(name, culture).CreateFont(size);

    /// <summary>
    /// Create a new instance of the <see cref="Font"/> for the named font family.
    /// </summary>
    /// <param name="name">The font family name.</param>
    /// <param name="culture">The font culture.</param>
    /// <param name="size">The size of the font in PT units.</param>
    /// <param name="style">The font style.</param>
    /// <returns>The new <see cref="Font"/>.</returns>
    public static Font CreateFont(string name, CultureInfo culture, float size, FontStyle style)
        => Collection.GetByCulture(name, culture).CreateFont(size, style);
}
