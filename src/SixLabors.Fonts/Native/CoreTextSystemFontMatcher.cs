// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Versioning;
using SixLabors.Fonts.Unicode;
using static SixLabors.Fonts.Native.CoreFoundation;
using static SixLabors.Fonts.Native.CoreText;

namespace SixLabors.Fonts.Native;

/// <summary>
/// Provides macOS system font fallback matching through CoreText.
/// </summary>
/// <remarks>
/// This follows the same operating-system fallback shape as Skia's CoreText font manager:
/// create a base font, ask CoreText for the font that can render the requested string, then
/// use the returned font's family and traits to map back into the managed system font collection.
/// </remarks>
internal static class CoreTextSystemFontMatcher
{
    private const uint CTFontItalicTrait = 1U << 0;
    private const uint CTFontBoldTrait = 1U << 1;
    private const double CTFontRegularWeight = 0D;
    private const double CTFontBoldWeight = .4D;
    private const double CTFontStyleScoreScale = 1000D;

    /// <summary>
    /// Tries to get the macOS default font family name.
    /// </summary>
    /// <param name="familyName">The macOS default font family name.</param>
    /// <returns><see langword="true"/> if CoreText returned a default font family name; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDefaultFamilyName([NotNullWhen(true)] out string? familyName)
    {
        familyName = null;

        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            return TryGetDefaultFamilyNameMacOS(out familyName);
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
    /// Tries to enumerate installed font family names through CoreText.
    /// </summary>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if CoreText returned family names; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyNames([NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;

        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            return TryGetFamilyNamesMacOS(out familyNames);
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
    /// Tries to enumerate installed system font faces through CoreText.
    /// </summary>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if CoreText returned font faces; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyFaces([NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;

        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            return TryGetFamilyFacesMacOS(out faces);
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
    /// Tries to match a character through CoreText.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if CoreText matched a system font; otherwise, <see langword="false"/>.</returns>
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

        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        try
        {
            return TryMatchCharacterMacOS(codePoint, style, familyName, culture, out matchedFamilyName, out matchedStyle);
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
    /// Tries to enumerate installed font family names through CoreText on macOS.
    /// </summary>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if CoreText returned family names; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("macos")]
    private static bool TryGetFamilyNamesMacOS([NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;
        IntPtr familyNamesArray = CTFontManagerCopyAvailableFontFamilyNames();

        try
        {
            if (familyNamesArray == IntPtr.Zero)
            {
                return false;
            }

            int count = checked((int)CFArrayGetCount(familyNamesArray));
            string[] names = new string[count];
            int nameCount = 0;

            for (int i = 0; i < count; i++)
            {
                IntPtr familyNameString = CFArrayGetValueAtIndex(familyNamesArray, i);

                if (familyNameString != IntPtr.Zero
                    && TryGetString(familyNameString, out string? familyName)
                    && familyName is { Length: > 0 })
                {
                    names[nameCount++] = familyName;
                }
            }

            if (nameCount == 0)
            {
                return false;
            }

            if (nameCount != names.Length)
            {
                Array.Resize(ref names, nameCount);
            }

            familyNames = names;
            return true;
        }
        finally
        {
            ReleaseIfNeeded(familyNamesArray);
        }
    }

    /// <summary>
    /// Tries to enumerate installed system font faces through CoreText on macOS.
    /// </summary>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if CoreText returned font faces; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("macos")]
    private static bool TryGetFamilyFacesMacOS([NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;
        IntPtr fontNameAttribute = GetStringConstant("kCTFontNameAttribute");
        IntPtr familyNameAttribute = GetStringConstant("kCTFontFamilyNameAttribute");
        IntPtr urlAttribute = GetStringConstant("kCTFontURLAttribute");
        IntPtr traitsAttribute = GetStringConstant("kCTFontTraitsAttribute");
        IntPtr weightTrait = GetStringConstant("kCTFontWeightTrait");
        IntPtr symbolicTrait = GetStringConstant("kCTFontSymbolicTrait");

        if (fontNameAttribute == IntPtr.Zero
            || familyNameAttribute == IntPtr.Zero
            || urlAttribute == IntPtr.Zero
            || traitsAttribute == IntPtr.Zero
            || weightTrait == IntPtr.Zero
            || symbolicTrait == IntPtr.Zero)
        {
            return false;
        }

        IntPtr collection = CTFontCollectionCreateFromAvailableFonts(IntPtr.Zero);
        IntPtr descriptors = IntPtr.Zero;

        try
        {
            if (collection == IntPtr.Zero)
            {
                return false;
            }

            descriptors = CTFontCollectionCreateMatchingFontDescriptors(collection);
            if (descriptors == IntPtr.Zero)
            {
                return false;
            }

            int count = checked((int)CFArrayGetCount(descriptors));
            List<NativeSystemFontFace> results = new(count);
            Dictionary<string, Dictionary<string, int>> faceIndexesByPath = new(StringComparer.Ordinal);

            for (int i = 0; i < count; i++)
            {
                IntPtr descriptor = CFArrayGetValueAtIndex(descriptors, i);

                if (descriptor == IntPtr.Zero)
                {
                    continue;
                }

                IntPtr url = CTFontDescriptorCopyAttribute(descriptor, urlAttribute);

                try
                {
                    if (url == IntPtr.Zero
                        || CFGetTypeID(url) != CFURLGetTypeID()
                        || !TryGetUrlPath(url, out string? path)
                        || !TryGetDescriptorStringAttribute(descriptor, familyNameAttribute, out string? familyName)
                        || !TryGetDescriptorStringAttribute(descriptor, fontNameAttribute, out string? fontName)
                        || !TryGetFaceIndex(url, path, fontName, fontNameAttribute, faceIndexesByPath, out int faceIndex))
                    {
                        continue;
                    }

                    GetDescriptorTraits(descriptor, traitsAttribute, weightTrait, symbolicTrait, out double weight, out uint? symbolicTraits);

                    // The traits dictionary exposes the symbolic traits without instantiating
                    // a font; instantiate one only for descriptors that do not carry them.
                    if (symbolicTraits is null)
                    {
                        IntPtr font = CTFontCreateWithFontDescriptor(descriptor, size: 0, matrix: IntPtr.Zero);

                        try
                        {
                            if (font != IntPtr.Zero)
                            {
                                symbolicTraits = CTFontGetSymbolicTraits(font);
                            }
                        }
                        finally
                        {
                            ReleaseIfNeeded(font);
                        }
                    }

                    if (symbolicTraits is null)
                    {
                        continue;
                    }

                    FontStyle style = ToFontStyle(symbolicTraits.Value);

                    results.Add(new NativeSystemFontFace(
                        familyName,
                        path,
                        style,
                        GetStyleScore(weight, style),
                        faceIndex));
                }
                finally
                {
                    ReleaseIfNeeded(url);
                }
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
            ReleaseIfNeeded(descriptors);
            ReleaseIfNeeded(collection);
        }
    }

    /// <summary>
    /// Gets the CoreText system user-interface font family name.
    /// </summary>
    /// <param name="familyName">The font family name.</param>
    /// <returns><see langword="true"/> if CoreText returned a system user-interface font family name; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("macos")]
    private static bool TryGetDefaultFamilyNameMacOS([NotNullWhen(true)] out string? familyName)
    {
        familyName = null;
        IntPtr font = CTFontCreateUIFontForLanguage(CTFontUIFontType.System, size: 0, language: IntPtr.Zero);

        try
        {
            return font != IntPtr.Zero && TryGetFamilyName(font, out familyName);
        }
        finally
        {
            ReleaseIfNeeded(font);
        }
    }

    /// <summary>
    /// Tries to match a character through CoreText on macOS.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if CoreText matched a system font; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("macos")]
    private static bool TryMatchCharacterMacOS(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out string? matchedFamilyName,
        out FontStyle matchedStyle)
    {
        matchedFamilyName = null;
        matchedStyle = default;

        Span<char> text = stackalloc char[2];
        text = text[..GetUtf16(codePoint, text)];
        string localeName = GetLocaleName(culture);
        IntPtr textString = CreateString(text);
        IntPtr localeString = CreateString(localeName);
        IntPtr familyString = string.IsNullOrEmpty(familyName)
            ? IntPtr.Zero
            : CreateString(familyName);
        IntPtr baseFont = IntPtr.Zero;
        IntPtr fallbackFont = IntPtr.Zero;

        try
        {
            if (textString == IntPtr.Zero)
            {
                return false;
            }

            if (familyString != IntPtr.Zero)
            {
                baseFont = CTFontCreateWithName(familyString, size: 0, matrix: IntPtr.Zero);
            }

            if (baseFont == IntPtr.Zero)
            {
                baseFont = CTFontCreateUIFontForLanguage(
                    CTFontUIFontType.System,
                    size: 0,
                    localeString);
            }

            if (baseFont == IntPtr.Zero)
            {
                return false;
            }

            IntPtr styledBaseFont = CreateFontWithStyle(baseFont, style);

            if (styledBaseFont != IntPtr.Zero)
            {
                ReleaseIfNeeded(baseFont);
                baseFont = styledBaseFont;
            }

            fallbackFont = CTFontCreateForStringWithLanguage(
                baseFont,
                textString,
                new CFRange(location: 0, length: text.Length),
                localeString);

            if (fallbackFont == IntPtr.Zero || !HasGlyphs(fallbackFont, text))
            {
                return false;
            }

            // CoreText fallback returns a concrete face. Re-resolving the fallback family with
            // the requested traits preserves the caller's bold/italic intent, matching Skia's
            // CoreText fallback path.
            if (TryGetFamilyName(fallbackFont, out string? fallbackFamilyName)
                && fallbackFamilyName is { Length: > 0 }
                && fallbackFamilyName[0] != '.')
            {
                IntPtr fallbackFamilyString = CreateString(fallbackFamilyName);
                IntPtr styledFallbackBaseFont = IntPtr.Zero;
                IntPtr styledFallbackFont = IntPtr.Zero;

                try
                {
                    if (fallbackFamilyString != IntPtr.Zero)
                    {
                        styledFallbackBaseFont = CTFontCreateWithName(fallbackFamilyString, size: 0, matrix: IntPtr.Zero);
                    }

                    if (styledFallbackBaseFont != IntPtr.Zero)
                    {
                        styledFallbackFont = CreateFontWithStyle(styledFallbackBaseFont, style);

                        if (styledFallbackFont != IntPtr.Zero && HasGlyphs(styledFallbackFont, text))
                        {
                            ReleaseIfNeeded(fallbackFont);
                            fallbackFont = styledFallbackFont;
                            styledFallbackFont = IntPtr.Zero;
                        }
                    }
                }
                finally
                {
                    ReleaseIfNeeded(styledFallbackFont);
                    ReleaseIfNeeded(styledFallbackBaseFont);
                    ReleaseIfNeeded(fallbackFamilyString);
                }
            }

            if (!TryGetFamilyName(fallbackFont, out matchedFamilyName))
            {
                return false;
            }

            matchedStyle = ToFontStyle(CTFontGetSymbolicTraits(fallbackFont));
            return true;
        }
        finally
        {
            ReleaseIfNeeded(fallbackFont);
            ReleaseIfNeeded(baseFont);
            ReleaseIfNeeded(familyString);
            ReleaseIfNeeded(localeString);
            ReleaseIfNeeded(textString);
        }
    }

    /// <summary>
    /// Gets a string descriptor attribute.
    /// </summary>
    /// <param name="descriptor">The CoreText font descriptor.</param>
    /// <param name="attribute">The descriptor attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns><see langword="true"/> if the attribute was found and read; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetDescriptorStringAttribute(IntPtr descriptor, IntPtr attribute, [NotNullWhen(true)] out string? value)
    {
        value = null;
        IntPtr attributeValue = CTFontDescriptorCopyAttribute(descriptor, attribute);

        try
        {
            return attributeValue != IntPtr.Zero
                && TryGetString(attributeValue, out value);
        }
        finally
        {
            ReleaseIfNeeded(attributeValue);
        }
    }

    /// <summary>
    /// Gets a file-system path from a Core Foundation URL.
    /// </summary>
    /// <param name="url">The Core Foundation URL.</param>
    /// <param name="path">The file-system path.</param>
    /// <returns><see langword="true"/> if the path was read; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetUrlPath(IntPtr url, [NotNullWhen(true)] out string? path)
    {
        path = null;
        IntPtr pathString = CFURLCopyFileSystemPath(url, CFURLPathStyle.kCFURLPOSIXPathStyle);

        try
        {
            return pathString != IntPtr.Zero
                && TryGetString(pathString, out path);
        }
        finally
        {
            ReleaseIfNeeded(pathString);
        }
    }

    /// <summary>
    /// Gets the font collection face index for a descriptor URL and PostScript name.
    /// </summary>
    /// <param name="url">The Core Foundation URL.</param>
    /// <param name="path">The file-system path.</param>
    /// <param name="fontName">The CoreText font name.</param>
    /// <param name="fontNameAttribute">The font-name descriptor attribute key.</param>
    /// <param name="faceIndexesByPath">The cached face indexes by path and font name.</param>
    /// <param name="faceIndex">The zero-based face index within the font file.</param>
    /// <returns><see langword="true"/> if the face index was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetFaceIndex(
        IntPtr url,
        string path,
        string fontName,
        IntPtr fontNameAttribute,
        Dictionary<string, Dictionary<string, int>> faceIndexesByPath,
        out int faceIndex)
    {
        if (!faceIndexesByPath.TryGetValue(path, out Dictionary<string, int>? faceIndexes))
        {
            IntPtr descriptors = CTFontManagerCreateFontDescriptorsFromURL(url);

            try
            {
                if (descriptors == IntPtr.Zero)
                {
                    faceIndexes = [];
                }
                else
                {
                    int count = checked((int)CFArrayGetCount(descriptors));
                    faceIndexes = new Dictionary<string, int>(count, StringComparer.Ordinal);

                    // CoreText returns descriptors from one font file in collection order, which
                    // is the index expected by the managed TTC loader.
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr descriptor = CFArrayGetValueAtIndex(descriptors, i);

                        if (descriptor != IntPtr.Zero
                            && TryGetDescriptorStringAttribute(descriptor, fontNameAttribute, out string? currentFontName))
                        {
                            _ = faceIndexes.TryAdd(currentFontName, i);
                        }
                    }
                }
            }
            finally
            {
                ReleaseIfNeeded(descriptors);
            }

            faceIndexesByPath.Add(path, faceIndexes);
        }

        return faceIndexes.TryGetValue(fontName, out faceIndex);
    }

    /// <summary>
    /// Gets the weight and symbolic traits from a descriptor's traits dictionary with a
    /// single attribute copy.
    /// </summary>
    /// <param name="descriptor">The CoreText font descriptor.</param>
    /// <param name="traitsAttribute">The traits attribute key.</param>
    /// <param name="weightTrait">The weight trait key.</param>
    /// <param name="symbolicTrait">The symbolic trait key.</param>
    /// <param name="weight">The CoreText weight value, or the regular weight when the descriptor does not expose one.</param>
    /// <param name="symbolicTraits">The symbolic traits, or <see langword="null"/> when the descriptor does not expose them.</param>
    private static void GetDescriptorTraits(
        IntPtr descriptor,
        IntPtr traitsAttribute,
        IntPtr weightTrait,
        IntPtr symbolicTrait,
        out double weight,
        out uint? symbolicTraits)
    {
        weight = CTFontRegularWeight;
        symbolicTraits = null;

        IntPtr traits = CTFontDescriptorCopyAttribute(descriptor, traitsAttribute);

        try
        {
            if (traits == IntPtr.Zero || CFGetTypeID(traits) != CFDictionaryGetTypeID())
            {
                return;
            }

            if (CFDictionaryGetValueIfPresent(traits, weightTrait, out IntPtr weightNumber)
                && weightNumber != IntPtr.Zero
                && CFGetTypeID(weightNumber) == CFNumberGetTypeID()
                && CFNumberGetDoubleValue(weightNumber, CFNumberType.CGFloat, out double weightValue))
            {
                weight = weightValue;
            }

            if (CFDictionaryGetValueIfPresent(traits, symbolicTrait, out IntPtr symbolicNumber)
                && symbolicNumber != IntPtr.Zero
                && CFGetTypeID(symbolicNumber) == CFNumberGetTypeID()
                && CFNumberGetIntValue(symbolicNumber, CFNumberType.SInt32, out int symbolicValue))
            {
                symbolicTraits = unchecked((uint)symbolicValue);
            }
        }
        finally
        {
            ReleaseIfNeeded(traits);
        }
    }

    /// <summary>
    /// Gets a preference score for a CoreText face inside a Fonts style bucket.
    /// </summary>
    /// <param name="weight">The CoreText weight value.</param>
    /// <param name="style">The Fonts style bucket.</param>
    /// <returns>The face preference score.</returns>
    private static int GetStyleScore(double weight, FontStyle style)
    {
        double targetWeight = (style & FontStyle.Bold) == FontStyle.Bold
            ? CTFontBoldWeight
            : CTFontRegularWeight;

        return (int)(Math.Abs(weight - targetWeight) * CTFontStyleScoreScale);
    }

    /// <summary>
    /// Gets the CoreText locale name to use for fallback matching.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <returns>The locale name.</returns>
    private static string GetLocaleName(CultureInfo? culture)
    {
        string localeName = culture?.Name ?? CultureInfo.CurrentCulture.Name;

        return localeName;
    }

    /// <summary>
    /// Creates a Core Foundation string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The retained Core Foundation string.</returns>
    private static unsafe IntPtr CreateString(string value)
    {
        fixed (char* valuePointer = value)
        {
            return CFStringCreateWithCharacters(IntPtr.Zero, valuePointer, value.Length);
        }
    }

    /// <summary>
    /// Creates a Core Foundation string.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>The retained Core Foundation string.</returns>
    private static unsafe IntPtr CreateString(ReadOnlySpan<char> value)
    {
        fixed (char* valuePointer = value)
        {
            return CFStringCreateWithCharacters(IntPtr.Zero, valuePointer, value.Length);
        }
    }

    /// <summary>
    /// Encodes a code point into UTF-16.
    /// </summary>
    /// <param name="codePoint">The code point to encode.</param>
    /// <param name="buffer">The UTF-16 output buffer.</param>
    /// <returns>The number of UTF-16 code units written.</returns>
    private static int GetUtf16(CodePoint codePoint, Span<char> buffer)
    {
        if (codePoint.IsBmp)
        {
            buffer[0] = (char)codePoint.Value;
            return 1;
        }

        int value = codePoint.Value - 0x10000;
        buffer[0] = (char)((value >> 10) + 0xD800);
        buffer[1] = (char)((value & 0x3FF) + 0xDC00);
        return 2;
    }

    /// <summary>
    /// Gets whether a font maps every UTF-16 code unit in a string to a glyph.
    /// </summary>
    /// <param name="font">The CoreText font.</param>
    /// <param name="text">The text to test.</param>
    /// <returns><see langword="true"/> if every UTF-16 code unit maps to a glyph; otherwise, <see langword="false"/>.</returns>
    private static unsafe bool HasGlyphs(IntPtr font, ReadOnlySpan<char> text)
    {
        Span<ushort> glyphs = stackalloc ushort[text.Length];

        fixed (char* charactersPointer = text)
        {
            fixed (ushort* glyphsPointer = glyphs)
            {
                return CTFontGetGlyphsForCharacters(font, (ushort*)charactersPointer, glyphsPointer, text.Length);
            }
        }
    }

    /// <summary>
    /// Gets a font family name.
    /// </summary>
    /// <param name="font">The CoreText font.</param>
    /// <param name="familyName">The family name.</param>
    /// <returns><see langword="true"/> if the family name was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetFamilyName(IntPtr font, out string? familyName)
    {
        familyName = null;
        IntPtr familyNameString = CTFontCopyFamilyName(font);

        try
        {
            return familyNameString != IntPtr.Zero
                && TryGetString(familyNameString, out familyName);
        }
        finally
        {
            ReleaseIfNeeded(familyNameString);
        }
    }

    /// <summary>
    /// Gets a managed string from a Core Foundation string.
    /// </summary>
    /// <param name="value">The Core Foundation string.</param>
    /// <param name="result">The managed string.</param>
    /// <returns><see langword="true"/> if the string was read; otherwise, <see langword="false"/>.</returns>
    private static unsafe bool TryGetString(IntPtr value, [NotNullWhen(true)] out string? result)
    {
        result = null;

        int length = (int)CFStringGetLength(value);

        if (length == 0)
        {
            return false;
        }

        result = string.Create(
            length,
            value,
            static (buffer, source) =>
            {
                fixed (char* bufferPointer = buffer)
                {
                    CFStringGetCharacters(source, new CFRange(location: 0, length: buffer.Length), bufferPointer);
                }
            });

        return true;
    }

    /// <summary>
    /// Creates a CoreText font copy with the requested Fonts style.
    /// </summary>
    /// <param name="font">The source CoreText font.</param>
    /// <param name="style">The requested Fonts style.</param>
    /// <returns>The retained CoreText font copy, or <see cref="IntPtr.Zero"/> if CoreText cannot create one.</returns>
    private static IntPtr CreateFontWithStyle(IntPtr font, FontStyle style)
    {
        const uint symbolicTraitMask = CTFontBoldTrait | CTFontItalicTrait;
        uint symbolicTraits = ToCoreTextTraits(style);

        return CTFontCreateCopyWithSymbolicTraits(
            font,
            size: 0,
            matrix: IntPtr.Zero,
            symbolicTraits,
            symbolicTraitMask);
    }

    /// <summary>
    /// Gets the CoreText symbolic traits for a Fonts style.
    /// </summary>
    /// <param name="style">The Fonts style.</param>
    /// <returns>The CoreText symbolic traits.</returns>
    private static uint ToCoreTextTraits(FontStyle style)
    {
        uint result = 0;

        if ((style & FontStyle.Bold) == FontStyle.Bold)
        {
            result |= CTFontBoldTrait;
        }

        if ((style & FontStyle.Italic) == FontStyle.Italic)
        {
            result |= CTFontItalicTrait;
        }

        return result;
    }

    /// <summary>
    /// Gets the Fonts style for CoreText symbolic traits.
    /// </summary>
    /// <param name="traits">The CoreText symbolic traits.</param>
    /// <returns>The Fonts style.</returns>
    private static FontStyle ToFontStyle(uint traits)
    {
        FontStyle result = (traits & CTFontBoldTrait) != 0 ? FontStyle.Bold : FontStyle.Regular;

        if ((traits & CTFontItalicTrait) != 0)
        {
            result |= FontStyle.Italic;
        }

        return result;
    }

    /// <summary>
    /// Releases a Core Foundation object when it exists.
    /// </summary>
    /// <param name="value">The Core Foundation object.</param>
    private static void ReleaseIfNeeded(IntPtr value)
    {
        if (value != IntPtr.Zero)
        {
            CFRelease(value);
        }
    }
}
