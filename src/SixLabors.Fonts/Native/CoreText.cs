// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Native;

// ReSharper disable InconsistentNaming

/// <summary>
/// Provides native CoreText entry points used by the macOS system font implementation.
/// </summary>
/// <remarks>
/// These declarations bind to the system CoreText framework at
/// <c>/System/Library/Frameworks/CoreText.framework/Versions/A/CoreText</c> and must only be
/// called from macOS-specific paths.
/// </remarks>
internal static partial class CoreText
{
    private const string CoreTextFramework = "/System/Library/Frameworks/CoreText.framework/Versions/A/CoreText";
    private static readonly Lazy<IntPtr> LazyCoreTextLibrary = new(() => NativeLibrary.Load(CoreTextFramework), isThreadSafe: true);

    /// <summary>
    /// Defines the CoreText user-interface font family to create.
    /// </summary>
    internal enum CTFontUIFontType
    {
        /// <summary>
        /// The default system user-interface font.
        /// </summary>
        System = 2
    }

    /// <summary>
    /// Returns an array of font family names.
    /// </summary>
    /// <returns>
    /// A retained <c>CFArray</c> of <c>CFStringRef</c> objects representing the available font
    /// family names, or <c>NULL</c> on error. The caller is responsible for releasing the returned array.
    /// </returns>
    /// <remarks>
    /// CoreText follows Core Foundation's Copy Rule for this API, so a non-<c>NULL</c> result
    /// must be released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontManagerCopyAvailableFontFamilyNames();

    /// <summary>
    /// Creates descriptors for the fonts contained in a font URL.
    /// </summary>
    /// <param name="fileUrl">The font file URL.</param>
    /// <returns>
    /// A retained <c>CFArray</c> of <c>CTFontDescriptor</c> objects, or <c>NULL</c> on error.
    /// The caller is responsible for releasing the returned array.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontManagerCreateFontDescriptorsFromURL(IntPtr fileUrl);

    /// <summary>
    /// Creates a collection containing the available fonts.
    /// </summary>
    /// <param name="options">Collection options, or <c>NULL</c>.</param>
    /// <returns>
    /// A retained <c>CTFontCollection</c>, or <c>NULL</c> on error. The caller is responsible for
    /// releasing the returned collection.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCollectionCreateFromAvailableFonts(IntPtr options);

    /// <summary>
    /// Creates descriptors for fonts matching a collection.
    /// </summary>
    /// <param name="collection">The font collection.</param>
    /// <returns>
    /// A retained <c>CFArray</c> of <c>CTFontDescriptor</c> objects, or <c>NULL</c> on error.
    /// The caller is responsible for releasing the returned array.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCollectionCreateMatchingFontDescriptors(IntPtr collection);

    /// <summary>
    /// Copies an attribute value from a font descriptor.
    /// </summary>
    /// <param name="descriptor">The font descriptor.</param>
    /// <param name="attribute">The descriptor attribute key.</param>
    /// <returns>
    /// A retained Core Foundation object, or <c>NULL</c> when the attribute is not present.
    /// The caller is responsible for releasing the returned object.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Copy Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontDescriptorCopyAttribute(IntPtr descriptor, IntPtr attribute);

    /// <summary>
    /// Creates a font reference from a font descriptor.
    /// </summary>
    /// <param name="descriptor">The font descriptor.</param>
    /// <param name="size">The font size.</param>
    /// <param name="matrix">The font matrix, or <c>NULL</c>.</param>
    /// <returns>
    /// A retained <c>CTFont</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned font.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCreateWithFontDescriptor(IntPtr descriptor, double size, IntPtr matrix);

    /// <summary>
    /// Creates a font reference from a font or family name.
    /// </summary>
    /// <param name="name">The font or family name.</param>
    /// <param name="size">The font size.</param>
    /// <param name="matrix">The font matrix, or <c>NULL</c>.</param>
    /// <returns>
    /// A retained <c>CTFont</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned font.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCreateWithName(IntPtr name, double size, IntPtr matrix);

    /// <summary>
    /// Creates a copy of a font with symbolic traits applied.
    /// </summary>
    /// <param name="font">The font to copy.</param>
    /// <param name="size">The font size.</param>
    /// <param name="matrix">The font matrix, or <c>NULL</c>.</param>
    /// <param name="symbolicTraitValue">The symbolic trait values to set.</param>
    /// <param name="symbolicTraitMask">The symbolic traits to replace.</param>
    /// <returns>
    /// A retained <c>CTFont</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned font.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCreateCopyWithSymbolicTraits(
        IntPtr font,
        double size,
        IntPtr matrix,
        uint symbolicTraitValue,
        uint symbolicTraitMask);

    /// <summary>
    /// Creates a user-interface font reference.
    /// </summary>
    /// <param name="uiType">The user-interface font type.</param>
    /// <param name="size">The font size.</param>
    /// <param name="language">The requested language, or <c>NULL</c>.</param>
    /// <returns>
    /// A retained <c>CTFont</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned font.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCreateUIFontForLanguage(CTFontUIFontType uiType, double size, IntPtr language);

    /// <summary>
    /// Creates the best matching font for a string.
    /// </summary>
    /// <param name="currentFont">The current font used as the matching context.</param>
    /// <param name="stringRef">The string to match.</param>
    /// <param name="range">The string range to match.</param>
    /// <param name="language">The requested language, or <c>NULL</c>.</param>
    /// <returns>
    /// A retained <c>CTFont</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned font.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCreateForStringWithLanguage(
        IntPtr currentFont,
        IntPtr stringRef,
        CFRange range,
        IntPtr language);

    /// <summary>
    /// Copies a font's family name.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <returns>
    /// A retained <c>CFString</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned string.
    /// </returns>
    /// <remarks>
    /// CoreText follows the Copy Rule for this API, so a non-<c>NULL</c> result must be released
    /// with <see cref="CoreFoundation.CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CTFontCopyFamilyName(IntPtr font);

    /// <summary>
    /// Gets the symbolic traits for a font.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <returns>The symbolic traits.</returns>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial uint CTFontGetSymbolicTraits(IntPtr font);

    /// <summary>
    /// Gets glyph identifiers for UTF-16 characters in a font.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="characters">The UTF-16 characters.</param>
    /// <param name="glyphs">The glyph output buffer.</param>
    /// <param name="count">The number of UTF-16 characters.</param>
    /// <returns><see langword="true"/> if every character has a glyph; otherwise, <see langword="false"/>.</returns>
    [LibraryImport(CoreTextFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool CTFontGetGlyphsForCharacters(
        IntPtr font,
        ushort* characters,
        ushort* glyphs,
        nint count);

    /// <summary>
    /// Gets a CoreText exported <c>CFStringRef</c> constant.
    /// </summary>
    /// <param name="name">The exported symbol name.</param>
    /// <returns>The constant value, or <see cref="IntPtr.Zero"/> when the symbol is unavailable.</returns>
    public static IntPtr GetStringConstant(string name)
    {
        if (!NativeLibrary.TryGetExport(LazyCoreTextLibrary.Value, name, out IntPtr symbol))
        {
            return IntPtr.Zero;
        }

        return Marshal.ReadIntPtr(symbol);
    }

    /// <summary>
    /// Represents a Core Foundation range.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct CFRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CFRange"/> struct.
        /// </summary>
        /// <param name="location">The starting location.</param>
        /// <param name="length">The range length.</param>
        public CFRange(nint location, nint length)
        {
            this.Location = location;
            this.Length = length;
        }

        /// <summary>
        /// Gets the starting location.
        /// </summary>
        public nint Location { get; }

        /// <summary>
        /// Gets the range length.
        /// </summary>
        public nint Length { get; }
    }
}
