// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Native;

/// <summary>
/// Provides Linux system font fallback matching through Fontconfig.
/// </summary>
/// <remarks>
/// This follows the same operating-system fallback shape as Skia's Fontconfig font manager:
/// build a pattern containing the requested family, style, language, and character set; let
/// Fontconfig select a matching installed font; then map the returned family and style back into
/// the managed system font collection.
/// </remarks>
internal static partial class FontConfigSystemFontMatcher
{
    private const string FontConfigLibrary = "libfontconfig.so.1";
    private const string FamilyProperty = "family";
    private const string FileProperty = "file";
    private const string IndexProperty = "index";
    private const string CharSetProperty = "charset";
    private const string LanguageProperty = "lang";
    private const string WeightProperty = "weight";
    private const string SlantProperty = "slant";
    private const string WidthProperty = "width";
    private const int FcMatchPattern = 0;
    private const int FcSetSystem = 0;
    private const int FcResultMatch = 0;
    private const int FcResultNoId = 3;
    private const int FcTypeString = 3;
    private const int FcWeightNormal = 80;
    private const int FcWeightDemiBold = 180;
    private const int FcWeightBold = 200;
    private const int FcSlantRoman = 0;
    private const int FcSlantItalic = 100;
    private const int FcWidthNormal = 100;

    /// <summary>
    /// The first Fontconfig version (2.13.93) that locks internally. Older versions are not
    /// thread safe and calls into them are serialized, matching Skia's FCLocker behavior.
    /// </summary>
    private const int FontConfigThreadSafeVersion = 21393;

    private static readonly Lazy<IntPtr> LazyConfig = new(CreateConfig, isThreadSafe: true);

    /// <summary>
    /// Serializes Fontconfig calls on library versions older than
    /// <see cref="FontConfigThreadSafeVersion"/>.
    /// </summary>
    private static readonly object FontConfigLock = new();

    /// <summary>
    /// Whether the loaded Fontconfig version requires external locking. FcGetVersion has
    /// always been thread safe.
    /// </summary>
    private static readonly Lazy<bool> LazyRequiresLock = new(() => FcGetVersion() < FontConfigThreadSafeVersion, isThreadSafe: true);

    /// <summary>
    /// Tries to get the Linux default font family name.
    /// </summary>
    /// <param name="familyName">The Linux default font family name.</param>
    /// <returns><see langword="true"/> if Fontconfig returned a default font family name; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDefaultFamilyName([NotNullWhen(true)] out string? familyName)
    {
        familyName = null;

        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return TryGetDefaultFamilyNameLinux(out familyName);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to enumerate installed font family names through Fontconfig.
    /// </summary>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if Fontconfig returned family names; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyNames([NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;

        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return TryGetFamilyNamesLinux(out familyNames);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to enumerate installed system font faces through Fontconfig.
    /// </summary>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if Fontconfig returned font faces; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyFaces([NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;

        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return TryGetFamilyFacesLinux(out faces);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to match a character through Fontconfig.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if Fontconfig matched a system font; otherwise, <see langword="false"/>.</returns>
    public static bool TryMatchCharacter(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out string? matchedFamilyName,
        out FontStyle matchedStyle)
    {
        matchedFamilyName = null;
        matchedStyle = default;

        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return TryMatchCharacterLinux(codePoint, style, familyName, culture, out matchedFamilyName, out matchedStyle);
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to enumerate installed font family names through Fontconfig on Linux.
    /// </summary>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if Fontconfig returned family names; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("linux")]
    private static bool TryGetFamilyNamesLinux([NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;

        IntPtr config = LazyConfig.Value;
        if (config == IntPtr.Zero)
        {
            return false;
        }

        bool lockTaken = false;

        try
        {
            EnterFontConfigLock(ref lockTaken);
            IntPtr fontSetPointer = FcConfigGetFonts(config, FcSetSystem);
            if (fontSetPointer == IntPtr.Zero)
            {
                return false;
            }

            FontConfigFontSet fontSet = Marshal.PtrToStructure<FontConfigFontSet>(fontSetPointer);
            HashSet<string> names = new(StringComparer.Ordinal);

            for (int i = 0; i < fontSet.FontCount; i++)
            {
                IntPtr pattern = Marshal.ReadIntPtr(fontSet.Fonts, i * IntPtr.Size);

                if (pattern != IntPtr.Zero
                    && FcPatternGetString(pattern, FamilyProperty, id: 0, out IntPtr family) == FcResultMatch
                    && family != IntPtr.Zero)
                {
                    string? familyName = Marshal.PtrToStringUTF8(family);

                    if (!string.IsNullOrEmpty(familyName))
                    {
                        names.Add(familyName);
                    }
                }
            }

            if (names.Count == 0)
            {
                return false;
            }

            familyNames = new string[names.Count];
            names.CopyTo(familyNames);
            return true;
        }
        finally
        {
            ExitFontConfigLock(lockTaken);
        }
    }

    /// <summary>
    /// Tries to enumerate installed system font faces through Fontconfig on Linux.
    /// </summary>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if Fontconfig returned font faces; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("linux")]
    private static bool TryGetFamilyFacesLinux([NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;

        IntPtr config = LazyConfig.Value;
        if (config == IntPtr.Zero)
        {
            return false;
        }

        bool lockTaken = false;

        try
        {
            EnterFontConfigLock(ref lockTaken);
            IntPtr fontSetPointer = FcConfigGetFonts(config, FcSetSystem);
            if (fontSetPointer == IntPtr.Zero)
            {
                return false;
            }

            FontConfigFontSet fontSet = Marshal.PtrToStructure<FontConfigFontSet>(fontSetPointer);
            List<NativeSystemFontFace> results = [];

            for (int i = 0; i < fontSet.FontCount; i++)
            {
                IntPtr pattern = Marshal.ReadIntPtr(fontSet.Fonts, i * IntPtr.Size);

                if (pattern == IntPtr.Zero
                    || FcPatternGetString(pattern, FamilyProperty, id: 0, out IntPtr family) != FcResultMatch
                    || family == IntPtr.Zero
                    || FcPatternGetString(pattern, FileProperty, id: 0, out IntPtr file) != FcResultMatch
                    || file == IntPtr.Zero)
                {
                    continue;
                }

                string? familyName = Marshal.PtrToStringUTF8(family);
                string? path = Marshal.PtrToStringUTF8(file);

                if (string.IsNullOrEmpty(familyName) || string.IsNullOrEmpty(path))
                {
                    continue;
                }

                int faceIndex = FcPatternGetInteger(pattern, IndexProperty, id: 0, out int index) == FcResultMatch
                    ? index
                    : 0;

                FontStyle style = ToFontStyle(pattern);

                results.Add(new NativeSystemFontFace(
                    familyName,
                    path,
                    style,
                    GetStyleScore(pattern, style),
                    faceIndex));
            }

            if (results.Count == 0)
            {
                return false;
            }

            faces = [.. results];
            return true;
        }
        finally
        {
            ExitFontConfigLock(lockTaken);
        }
    }

    /// <summary>
    /// Gets the default Fontconfig font family name.
    /// </summary>
    /// <param name="familyName">The font family name.</param>
    /// <returns><see langword="true"/> if Fontconfig returned a default font family name; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("linux")]
    private static bool TryGetDefaultFamilyNameLinux([NotNullWhen(true)] out string? familyName)
    {
        familyName = null;

        IntPtr config = LazyConfig.Value;
        if (config == IntPtr.Zero)
        {
            return false;
        }

        bool lockTaken = false;
        IntPtr pattern = IntPtr.Zero;
        IntPtr matchedPattern = IntPtr.Zero;

        try
        {
            EnterFontConfigLock(ref lockTaken);
            pattern = FcPatternCreate();

            if (pattern == IntPtr.Zero
                || FcPatternAddInteger(pattern, WeightProperty, FcWeightNormal) == 0
                || FcPatternAddInteger(pattern, SlantProperty, FcSlantRoman) == 0
                || FcPatternAddInteger(pattern, WidthProperty, FcWidthNormal) == 0
                || FcConfigSubstitute(config, pattern, FcMatchPattern) == 0)
            {
                return false;
            }

            FcDefaultSubstitute(pattern);
            matchedPattern = FcFontMatch(config, pattern, out int matchResult);

            if (matchedPattern == IntPtr.Zero || matchResult != FcResultMatch)
            {
                return false;
            }

            if (FcPatternGetString(matchedPattern, FamilyProperty, id: 0, out IntPtr family) != FcResultMatch
                || family == IntPtr.Zero)
            {
                return false;
            }

            familyName = Marshal.PtrToStringUTF8(family);
            return !string.IsNullOrEmpty(familyName);
        }
        finally
        {
            if (matchedPattern != IntPtr.Zero)
            {
                FcPatternDestroy(matchedPattern);
            }

            if (pattern != IntPtr.Zero)
            {
                FcPatternDestroy(pattern);
            }

            ExitFontConfigLock(lockTaken);
        }
    }

    /// <summary>
    /// Tries to match a character through Fontconfig on Linux.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if Fontconfig matched a system font; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("linux")]
    private static bool TryMatchCharacterLinux(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out string? matchedFamilyName,
        out FontStyle matchedStyle)
    {
        matchedFamilyName = null;
        matchedStyle = default;

        IntPtr config = LazyConfig.Value;
        if (config == IntPtr.Zero)
        {
            return false;
        }

        bool lockTaken = false;
        IntPtr pattern = IntPtr.Zero;
        IntPtr charSet = IntPtr.Zero;
        IntPtr langSet = IntPtr.Zero;
        IntPtr matchedPattern = IntPtr.Zero;

        try
        {
            EnterFontConfigLock(ref lockTaken);
            pattern = FcPatternCreate();
            charSet = FcCharSetCreate();

            if (pattern == IntPtr.Zero || charSet == IntPtr.Zero)
            {
                return false;
            }

            // The requested family gets a weak binding so that character coverage outranks
            // family loyalty during matching; with a strong binding the requested family can
            // win the match even when it cannot render the character. Mirrors Skia.
            if (!string.IsNullOrEmpty(familyName)
                && !TryAddWeakFamily(pattern, familyName))
            {
                return false;
            }

            if (FcPatternAddInteger(pattern, WeightProperty, ToFontConfigWeight(style)) == 0
                || FcPatternAddInteger(pattern, SlantProperty, ToFontConfigSlant(style)) == 0
                || FcPatternAddInteger(pattern, WidthProperty, FcWidthNormal) == 0
                || FcCharSetAddChar(charSet, (uint)codePoint.Value) == 0
                || FcPatternAddCharSet(pattern, CharSetProperty, charSet) == 0)
            {
                return false;
            }

            string localeName = GetLocaleName(culture);
            if (!string.IsNullOrEmpty(localeName))
            {
                langSet = FcLangSetCreate();
                if (langSet != IntPtr.Zero
                    && (FcLangSetAdd(langSet, localeName) == 0
                        || FcPatternAddLangSet(pattern, LanguageProperty, langSet) == 0))
                {
                    return false;
                }
            }

            if (FcConfigSubstitute(config, pattern, FcMatchPattern) == 0)
            {
                return false;
            }

            FcDefaultSubstitute(pattern);
            matchedPattern = FcFontMatch(config, pattern, out int matchResult);

            if (matchedPattern == IntPtr.Zero || matchResult != FcResultMatch)
            {
                return false;
            }

            // Fontconfig always returns some font; only accept it when it can actually
            // render the requested character. Mirrors Skia's FontContainsCharacter.
            if (!FontContainsCharacter(matchedPattern, (uint)codePoint.Value))
            {
                return false;
            }

            if (FcPatternGetString(matchedPattern, FamilyProperty, id: 0, out IntPtr family) != FcResultMatch
                || family == IntPtr.Zero)
            {
                return false;
            }

            matchedFamilyName = Marshal.PtrToStringUTF8(family);
            matchedStyle = ToFontStyle(matchedPattern);
            return !string.IsNullOrEmpty(matchedFamilyName);
        }
        finally
        {
            if (matchedPattern != IntPtr.Zero)
            {
                FcPatternDestroy(matchedPattern);
            }

            if (langSet != IntPtr.Zero)
            {
                FcLangSetDestroy(langSet);
            }

            if (charSet != IntPtr.Zero)
            {
                FcCharSetDestroy(charSet);
            }

            if (pattern != IntPtr.Zero)
            {
                FcPatternDestroy(pattern);
            }

            ExitFontConfigLock(lockTaken);
        }
    }

    /// <summary>
    /// Creates the process-wide Fontconfig configuration used by fallback matching.
    /// </summary>
    /// <returns>The Fontconfig configuration, or <c>NULL</c> when unavailable.</returns>
    private static IntPtr CreateConfig()
    {
        if (!OperatingSystem.IsLinux())
        {
            return IntPtr.Zero;
        }

        bool lockTaken = false;

        try
        {
            EnterFontConfigLock(ref lockTaken);
            return FcInitLoadConfigAndFonts();
        }
        catch (DllNotFoundException)
        {
            return IntPtr.Zero;
        }
        catch (EntryPointNotFoundException)
        {
            return IntPtr.Zero;
        }
        finally
        {
            ExitFontConfigLock(lockTaken);
        }
    }

    /// <summary>
    /// Enters the Fontconfig serialization lock when the loaded library version requires it.
    /// </summary>
    /// <param name="lockTaken">Set to <see langword="true"/> when the lock was entered.</param>
    private static void EnterFontConfigLock(ref bool lockTaken)
    {
        if (LazyRequiresLock.Value)
        {
            Monitor.Enter(FontConfigLock, ref lockTaken);
        }
    }

    /// <summary>
    /// Exits the Fontconfig serialization lock when it was entered.
    /// </summary>
    /// <param name="lockTaken">Whether the lock was entered.</param>
    private static void ExitFontConfigLock(bool lockTaken)
    {
        if (lockTaken)
        {
            Monitor.Exit(FontConfigLock);
        }
    }

    /// <summary>
    /// Adds a family name to a pattern with a weak binding, so that character coverage
    /// outranks family loyalty during matching. Fontconfig copies the value.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="familyName">The family name.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    private static bool TryAddWeakFamily(IntPtr pattern, string familyName)
    {
        IntPtr utf8 = Marshal.StringToCoTaskMemUTF8(familyName);

        try
        {
            FontConfigValue value = new()
            {
                Type = FcTypeString,
                Value = utf8
            };

            return FcPatternAddWeak(pattern, FamilyProperty, value, append: 0) != 0;
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    /// <summary>
    /// Gets whether a matched font pattern can render the requested character, checking
    /// every character set on the pattern.
    /// </summary>
    /// <param name="font">The matched font pattern.</param>
    /// <param name="character">The Unicode code point.</param>
    /// <returns><see langword="true"/> if a character set contains the code point.</returns>
    private static bool FontContainsCharacter(IntPtr font, uint character)
    {
        for (int charSetId = 0; ; charSetId++)
        {
            int result = FcPatternGetCharSet(font, CharSetProperty, charSetId, out IntPtr charSet);
            if (result == FcResultNoId)
            {
                return false;
            }

            if (result != FcResultMatch)
            {
                continue;
            }

            if (charSet != IntPtr.Zero && FcCharSetHasChar(charSet, character) != 0)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Gets the Fontconfig locale name to use for fallback matching.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <returns>The locale name.</returns>
    private static string GetLocaleName(CultureInfo? culture)
    {
        string localeName = culture?.Name ?? CultureInfo.CurrentCulture.Name;

        return localeName;
    }

    /// <summary>
    /// Gets the Fontconfig weight for a Fonts style.
    /// </summary>
    /// <param name="style">The Fonts style.</param>
    /// <returns>The Fontconfig weight.</returns>
    private static int ToFontConfigWeight(FontStyle style)
        => (style & FontStyle.Bold) == FontStyle.Bold ? FcWeightBold : FcWeightNormal;

    /// <summary>
    /// Gets the Fontconfig slant for a Fonts style.
    /// </summary>
    /// <param name="style">The Fonts style.</param>
    /// <returns>The Fontconfig slant.</returns>
    private static int ToFontConfigSlant(FontStyle style)
        => (style & FontStyle.Italic) == FontStyle.Italic ? FcSlantItalic : FcSlantRoman;

    /// <summary>
    /// Gets the Fonts style for a Fontconfig pattern.
    /// </summary>
    /// <param name="pattern">The Fontconfig pattern.</param>
    /// <returns>The Fonts style.</returns>
    private static FontStyle ToFontStyle(IntPtr pattern)
    {
        FontStyle result = FontStyle.Regular;

        if (FcPatternGetInteger(pattern, WeightProperty, id: 0, out int weight) == FcResultMatch
            && weight >= FcWeightDemiBold)
        {
            result |= FontStyle.Bold;
        }

        if (FcPatternGetInteger(pattern, SlantProperty, id: 0, out int slant) == FcResultMatch
            && slant != FcSlantRoman)
        {
            result |= FontStyle.Italic;
        }

        return result;
    }

    /// <summary>
    /// Gets a preference score for a Fontconfig face inside a Fonts style bucket.
    /// </summary>
    /// <param name="pattern">The Fontconfig pattern.</param>
    /// <param name="style">The Fonts style bucket.</param>
    /// <returns>The face preference score.</returns>
    private static int GetStyleScore(IntPtr pattern, FontStyle style)
    {
        int score = 0;

        if (FcPatternGetInteger(pattern, WeightProperty, id: 0, out int weight) == FcResultMatch)
        {
            int targetWeight = (style & FontStyle.Bold) == FontStyle.Bold ? FcWeightBold : FcWeightNormal;
            score += Math.Abs(weight - targetWeight);
        }

        if (FcPatternGetInteger(pattern, SlantProperty, id: 0, out int slant) == FcResultMatch)
        {
            score += Math.Abs(slant - ToFontConfigSlant(style));
        }

        return score;
    }

    /// <summary>
    /// Initializes Fontconfig and loads the system font configuration.
    /// </summary>
    /// <returns>The loaded Fontconfig configuration.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcInitLoadConfigAndFonts();

    /// <summary>
    /// Gets a Fontconfig font set owned by the configuration.
    /// </summary>
    /// <param name="config">The Fontconfig configuration.</param>
    /// <param name="setName">The requested font set.</param>
    /// <returns>The font set pointer.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcConfigGetFonts(IntPtr config, int setName);

    /// <summary>
    /// Creates an empty font pattern.
    /// </summary>
    /// <returns>The created pattern.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcPatternCreate();

    /// <summary>
    /// Destroys a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern to destroy.</param>
    [LibraryImport(FontConfigLibrary)]
    private static partial void FcPatternDestroy(IntPtr pattern);

    /// <summary>
    /// Gets the Fontconfig library version as a single integer
    /// (<c>major * 10000 + minor * 100 + revision</c>).
    /// </summary>
    /// <returns>The Fontconfig version.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial int FcGetVersion();

    /// <summary>
    /// Adds a value to a font pattern with a weak binding. Weakly bound values rank below
    /// strongly bound ones during matching, letting other criteria such as character
    /// coverage take precedence.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="value">The value to add. Fontconfig stores a copy.</param>
    /// <param name="append">Non-zero to append the value after existing values.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternAddWeak(IntPtr pattern, string name, FontConfigValue value, int append);

    /// <summary>
    /// Adds an integer value to a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternAddInteger(IntPtr pattern, string name, int value);

    /// <summary>
    /// Adds a character set to a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="charSet">The character set.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternAddCharSet(IntPtr pattern, string name, IntPtr charSet);

    /// <summary>
    /// Adds a language set to a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="langSet">The language set.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternAddLangSet(IntPtr pattern, string name, IntPtr langSet);

    /// <summary>
    /// Gets a string value from a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="id">The value index.</param>
    /// <param name="value">The string value.</param>
    /// <returns>The Fontconfig result.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternGetString(IntPtr pattern, string name, int id, out IntPtr value);

    /// <summary>
    /// Gets an integer value from a font pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="id">The value index.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>The Fontconfig result.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternGetInteger(IntPtr pattern, string name, int id, out int value);

    /// <summary>
    /// Creates an empty character set.
    /// </summary>
    /// <returns>The created character set.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcCharSetCreate();

    /// <summary>
    /// Destroys a character set.
    /// </summary>
    /// <param name="charSet">The character set to destroy.</param>
    [LibraryImport(FontConfigLibrary)]
    private static partial void FcCharSetDestroy(IntPtr charSet);

    /// <summary>
    /// Adds a Unicode code point to a character set.
    /// </summary>
    /// <param name="charSet">The character set.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial int FcCharSetAddChar(IntPtr charSet, uint codePoint);

    /// <summary>
    /// Gets whether a character set contains a Unicode code point.
    /// </summary>
    /// <param name="charSet">The character set.</param>
    /// <param name="codePoint">The Unicode code point.</param>
    /// <returns>Non-zero when the code point is present; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial int FcCharSetHasChar(IntPtr charSet, uint codePoint);

    /// <summary>
    /// Gets a character set from a font pattern without transferring ownership.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="name">The object name.</param>
    /// <param name="id">The value index.</param>
    /// <param name="charSet">The character set, owned by the pattern.</param>
    /// <returns>The Fontconfig result.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcPatternGetCharSet(IntPtr pattern, string name, int id, out IntPtr charSet);

    /// <summary>
    /// Creates an empty language set.
    /// </summary>
    /// <returns>The created language set.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcLangSetCreate();

    /// <summary>
    /// Destroys a language set.
    /// </summary>
    /// <param name="langSet">The language set to destroy.</param>
    [LibraryImport(FontConfigLibrary)]
    private static partial void FcLangSetDestroy(IntPtr langSet);

    /// <summary>
    /// Adds a language tag to a language set.
    /// </summary>
    /// <param name="langSet">The language set.</param>
    /// <param name="language">The language tag.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary, StringMarshalling = StringMarshalling.Utf8)]
    private static partial int FcLangSetAdd(IntPtr langSet, string language);

    /// <summary>
    /// Performs Fontconfig substitutions against a pattern.
    /// </summary>
    /// <param name="config">The Fontconfig configuration.</param>
    /// <param name="pattern">The pattern.</param>
    /// <param name="kind">The match kind.</param>
    /// <returns>Non-zero on success; otherwise, zero.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial int FcConfigSubstitute(IntPtr config, IntPtr pattern, int kind);

    /// <summary>
    /// Performs default substitutions against a pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    [LibraryImport(FontConfigLibrary)]
    private static partial void FcDefaultSubstitute(IntPtr pattern);

    /// <summary>
    /// Finds the best matching font for a pattern.
    /// </summary>
    /// <param name="config">The Fontconfig configuration.</param>
    /// <param name="pattern">The pattern.</param>
    /// <param name="result">The match result.</param>
    /// <returns>The matched pattern.</returns>
    [LibraryImport(FontConfigLibrary)]
    private static partial IntPtr FcFontMatch(IntPtr config, IntPtr pattern, out int result);

    /// <summary>
    /// Represents the Fontconfig <c>FcValue</c> layout: an <c>FcType</c> discriminant followed
    /// by a pointer-sized union. The union starts at offset 8 on 64-bit platforms because it
    /// is pointer aligned.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FontConfigValue
    {
        /// <summary>
        /// The <c>FcType</c> discriminant of <see cref="Value"/>.
        /// </summary>
        public int Type;

        /// <summary>
        /// The union payload; a native string pointer for <c>FcTypeString</c>.
        /// </summary>
        public IntPtr Value;
    }

    /// <summary>
    /// Represents the Fontconfig <c>FcFontSet</c> layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FontConfigFontSet
    {
        /// <summary>
        /// Gets the number of font patterns in the set.
        /// </summary>
        public readonly int FontCount;

        /// <summary>
        /// Gets the allocated font pattern capacity.
        /// </summary>
        public readonly int Size;

        /// <summary>
        /// Gets the native array of font pattern pointers.
        /// </summary>
        public readonly IntPtr Fonts;
    }
}
