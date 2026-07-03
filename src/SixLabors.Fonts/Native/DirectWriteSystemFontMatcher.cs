// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.Versioning;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Native;

/// <summary>
/// Provides Windows system font fallback matching through DirectWrite.
/// </summary>
/// <remarks>
/// This is the Windows implementation of system fallback. The COM interface identifiers are the
/// DirectWrite interface identifiers declared by <c>DWRITE_DECLARE_INTERFACE</c> in the Windows SDK
/// <c>dwrite.h</c> and <c>dwrite_2.h</c> headers. The placeholder interface members are intentional:
/// COM dispatch is positional, so inherited DirectWrite vtable slots must be preserved even when this
/// type only calls the final fallback and metadata methods.
/// </remarks>
internal static partial class DirectWriteSystemFontMatcher
{
    private const uint SpiGetNonClientMetrics = 0x0029;
    private const int LfFaceSize = 32;
    private static readonly Lazy<DirectWriteObjects?> LazyObjects = new(CreateObjects, isThreadSafe: true);

    /// <summary>
    /// Defines the DirectWrite factory lifetime.
    /// </summary>
    internal enum DirectWriteFactoryType
    {
        /// <summary>
        /// Reuses the shared DirectWrite factory.
        /// </summary>
        Shared = 0
    }

    /// <summary>
    /// Defines the DirectWrite font weight values used by fallback matching.
    /// </summary>
    internal enum DirectWriteFontWeight
    {
        /// <summary>
        /// Normal font weight.
        /// </summary>
        Normal = 400,

        /// <summary>
        /// Semi-bold font weight.
        /// </summary>
        SemiBold = 600,

        /// <summary>
        /// Bold font weight.
        /// </summary>
        Bold = 700
    }

    /// <summary>
    /// Defines the DirectWrite font style values used by fallback matching.
    /// </summary>
    internal enum DirectWriteFontStyle
    {
        /// <summary>
        /// Normal font style.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// Oblique font style.
        /// </summary>
        Oblique = 1,

        /// <summary>
        /// Italic font style.
        /// </summary>
        Italic = 2
    }

    /// <summary>
    /// Defines the DirectWrite font stretch values used by fallback matching.
    /// </summary>
    internal enum DirectWriteFontStretch
    {
        /// <summary>
        /// Normal font stretch.
        /// </summary>
        Normal = 5
    }

    /// <summary>
    /// Defines the paragraph reading direction supplied to DirectWrite.
    /// </summary>
    internal enum DirectWriteReadingDirection
    {
        /// <summary>
        /// Left-to-right reading direction.
        /// </summary>
        LeftToRight = 0
    }

    /// <summary>
    /// Supplies text and locale spans to DirectWrite text analysis APIs.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteTextAnalysisSource</c>,
    /// IID <c>688e1a58-5094-47c8-adc8-fbcea60ae92b</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("688E1A58-5094-47C8-ADC8-FBCEA60AE92B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteTextAnalysisSource
    {
        /// <summary>
        /// Gets a text span at the requested position.
        /// </summary>
        /// <param name="textPosition">The requested UTF-16 text position.</param>
        /// <param name="textString">The returned pointer to the text span.</param>
        /// <param name="textLength">The returned length of the text span.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetTextAtPosition(uint textPosition, out IntPtr textString, out uint textLength);

        /// <summary>
        /// Gets a text span before the requested position.
        /// </summary>
        /// <param name="textPosition">The requested UTF-16 text position.</param>
        /// <param name="textString">The returned pointer to the text span.</param>
        /// <param name="textLength">The returned length of the text span.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetTextBeforePosition(uint textPosition, out IntPtr textString, out uint textLength);

        /// <summary>
        /// Gets the paragraph reading direction.
        /// </summary>
        /// <returns>The paragraph reading direction.</returns>
        [PreserveSig]
        public DirectWriteReadingDirection GetParagraphReadingDirection();

        /// <summary>
        /// Gets the locale name for the requested text position.
        /// </summary>
        /// <param name="textPosition">The requested UTF-16 text position.</param>
        /// <param name="textLength">The returned length covered by the locale.</param>
        /// <param name="localeName">The returned pointer to the locale name.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetLocaleName(uint textPosition, out uint textLength, out IntPtr localeName);

        /// <summary>
        /// Gets the number substitution for the requested text position.
        /// </summary>
        /// <param name="textPosition">The requested UTF-16 text position.</param>
        /// <param name="textLength">The returned length covered by the substitution.</param>
        /// <param name="numberSubstitution">The returned number substitution pointer.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetNumberSubstitution(uint textPosition, out uint textLength, out IntPtr numberSubstitution);
    }

    /// <summary>
    /// Represents the DirectWrite factory interface needed to retrieve system font fallback.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite_2.h</c>, <c>IDWriteFactory2</c>,
    /// IID <c>0439fc60-ca44-4994-8dee-3a9af7b732ec</c>. The members before
    /// <see cref="GetSystemFontFallback"/> preserve inherited <c>IDWriteFactory</c> and
    /// <c>IDWriteFactory1</c> vtable slots.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("0439FC60-CA44-4994-8DEE-3A9AF7B732EC")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFactory2
    {
        /// <summary>
        /// Gets the system font collection.
        /// </summary>
        /// <param name="fontCollection">The returned system font collection.</param>
        /// <param name="checkForUpdates">Whether DirectWrite should check for updates to the system font collection.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetSystemFontCollection(
            [MarshalAs(UnmanagedType.Interface)] out IDWriteFontCollection? fontCollection,
            [MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateCustomFontCollection</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateCustomFontCollection();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::RegisterFontCollectionLoader</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int RegisterFontCollectionLoader();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::UnregisterFontCollectionLoader</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int UnregisterFontCollectionLoader();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateFontFileReference</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateFontFileReference();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateCustomFontFileReference</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateCustomFontFileReference();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateFontFace</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateFontFace();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateRenderingParams</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateRenderingParams();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateMonitorRenderingParams</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateMonitorRenderingParams();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateCustomRenderingParams</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateCustomRenderingParams();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::RegisterFontFileLoader</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int RegisterFontFileLoader();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::UnregisterFontFileLoader</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int UnregisterFontFileLoader();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateTextFormat</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateTextFormat();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateTypography</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateTypography();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::GetGdiInterop</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetGdiInterop();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateTextLayout</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateTextLayout();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateGdiCompatibleTextLayout</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateGdiCompatibleTextLayout();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateEllipsisTrimmingSign</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateEllipsisTrimmingSign();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateTextAnalyzer</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateTextAnalyzer();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateNumberSubstitution</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateNumberSubstitution();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory::CreateGlyphRunAnalysis</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateGlyphRunAnalysis();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory1::GetEudcFontCollection</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetEudcFontCollection();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFactory1::CreateCustomRenderingParams</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateCustomRenderingParams1();

        /// <summary>
        /// Gets the system font fallback interface.
        /// </summary>
        /// <param name="fontFallback">The returned system font fallback interface.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetSystemFontFallback([MarshalAs(UnmanagedType.Interface)] out IDWriteFontFallback? fontFallback);
    }

    /// <summary>
    /// Represents a DirectWrite font collection.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteFontCollection</c>,
    /// IID <c>a84cee02-3eea-4eee-a827-87c1a02a0fcc</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("A84CEE02-3EEA-4EEE-A827-87C1A02A0FCC")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFontCollection
    {
        /// <summary>
        /// Gets the number of font families in the collection.
        /// </summary>
        /// <returns>The number of font families.</returns>
        [PreserveSig]
        public uint GetFontFamilyCount();

        /// <summary>
        /// Gets a font family by index.
        /// </summary>
        /// <param name="index">The family index.</param>
        /// <param name="fontFamily">The returned font family.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFontFamily(uint index, [MarshalAs(UnmanagedType.Interface)] out IDWriteFontFamily? fontFamily);

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontCollection::FindFamilyName</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int FindFamilyName();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontCollection::GetFontFromFontFace</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFontFromFontFace();
    }

    /// <summary>
    /// Represents a DirectWrite local font file loader.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteLocalFontFileLoader</c>,
    /// IID <c>b2d9f3ec-c9fe-4a11-a2ec-d86208f7c0a2</c>. The first method
    /// preserves the inherited <c>IDWriteFontFileLoader</c> vtable slot.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("B2D9F3EC-C9FE-4A11-A2EC-D86208F7C0A2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteLocalFontFileLoader
    {
        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontFileLoader::CreateStreamFromKey</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateStreamFromKey();

        /// <summary>
        /// Gets the local font file path length from a DirectWrite font file reference key.
        /// </summary>
        /// <param name="fontFileReferenceKey">The font file reference key.</param>
        /// <param name="fontFileReferenceKeySize">The font file reference key size, in bytes.</param>
        /// <param name="filePathLength">The file path length, excluding the null terminator.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFilePathLengthFromKey(IntPtr fontFileReferenceKey, uint fontFileReferenceKeySize, out uint filePathLength);

        /// <summary>
        /// Gets the local font file path from a DirectWrite font file reference key.
        /// </summary>
        /// <param name="fontFileReferenceKey">The font file reference key.</param>
        /// <param name="fontFileReferenceKeySize">The font file reference key size, in bytes.</param>
        /// <param name="filePath">The output file path buffer.</param>
        /// <param name="filePathSize">The output file path buffer size, including the null terminator.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public unsafe int GetFilePathFromKey(IntPtr fontFileReferenceKey, uint fontFileReferenceKeySize, char* filePath, uint filePathSize);
    }

    /// <summary>
    /// Represents a DirectWrite font file.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteFontFile</c>,
    /// IID <c>739d886a-cef5-47dc-8769-1a8b41bebbb0</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("739D886A-CEF5-47DC-8769-1A8B41BEBBB0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFontFile
    {
        /// <summary>
        /// Gets the font file reference key.
        /// </summary>
        /// <param name="fontFileReferenceKey">The returned font file reference key pointer.</param>
        /// <param name="fontFileReferenceKeySize">The returned font file reference key size, in bytes.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetReferenceKey(out IntPtr fontFileReferenceKey, out uint fontFileReferenceKeySize);

        /// <summary>
        /// Gets the local font file loader associated with the font file.
        /// </summary>
        /// <param name="fontFileLoader">The returned local font file loader.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetLoader([MarshalAs(UnmanagedType.Interface)] out IDWriteLocalFontFileLoader? fontFileLoader);
    }

    /// <summary>
    /// Represents a DirectWrite font face.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteFontFace</c>,
    /// IID <c>5f49804d-7024-4d43-bfa9-d25984f53849</c>. Only the file and index
    /// methods are called; the preceding face type slot is preserved.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("5F49804D-7024-4D43-BFA9-D25984F53849")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFontFace
    {
        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontFace::GetType</c> vtable slot.
        /// </summary>
        /// <returns>The font face type.</returns>
        [PreserveSig]
        public int GetType();

        /// <summary>
        /// Gets the font files representing this font face.
        /// </summary>
        /// <param name="numberOfFiles">The number of file entries.</param>
        /// <param name="fontFiles">The output font files.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFiles(
            ref uint numberOfFiles,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Interface, SizeParamIndex = 0)] IDWriteFontFile?[]? fontFiles);

        /// <summary>
        /// Gets the zero-based face index within the font file.
        /// </summary>
        /// <returns>The face index.</returns>
        [PreserveSig]
        public uint GetIndex();
    }

    /// <summary>
    /// Represents the DirectWrite system font fallback interface.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite_2.h</c>, <c>IDWriteFontFallback</c>,
    /// IID <c>efa008f9-f7a1-48bf-b05c-f224713cc0ff</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("EFA008F9-F7A1-48BF-B05C-F224713CC0FF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFontFallback
    {
        /// <summary>
        /// Maps text to a system fallback font.
        /// </summary>
        /// <param name="analysisSource">The text analysis source.</param>
        /// <param name="textPosition">The starting UTF-16 text position.</param>
        /// <param name="textLength">The number of UTF-16 code units to map.</param>
        /// <param name="baseFontCollection">The base font collection pointer.</param>
        /// <param name="baseFamilyName">The requested base family name.</param>
        /// <param name="baseWeight">The requested base font weight.</param>
        /// <param name="baseStyle">The requested base font style.</param>
        /// <param name="baseStretch">The requested base font stretch.</param>
        /// <param name="mappedLength">The returned mapped text length.</param>
        /// <param name="mappedFont">The returned mapped font.</param>
        /// <param name="scale">The returned font scale.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int MapCharacters(
            IDWriteTextAnalysisSource analysisSource,
            uint textPosition,
            uint textLength,
            IntPtr baseFontCollection,
            [MarshalAs(UnmanagedType.LPWStr)] string? baseFamilyName,
            DirectWriteFontWeight baseWeight,
            DirectWriteFontStyle baseStyle,
            DirectWriteFontStretch baseStretch,
            out uint mappedLength,
            [MarshalAs(UnmanagedType.Interface)] out IDWriteFont? mappedFont,
            out float scale);
    }

    /// <summary>
    /// Represents a DirectWrite font.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteFont</c>,
    /// IID <c>acd16696-8c14-4f5d-877e-fe3fc1d32737</c>. Members after
    /// <see cref="GetStyle"/> are retained to preserve the DirectWrite vtable shape.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("ACD16696-8C14-4F5D-877E-FE3FC1D32737")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFont
    {
        /// <summary>
        /// Gets the containing font family.
        /// </summary>
        /// <param name="fontFamily">The returned font family.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFontFamily([MarshalAs(UnmanagedType.Interface)] out IDWriteFontFamily? fontFamily);

        /// <summary>
        /// Gets the font weight.
        /// </summary>
        /// <returns>The font weight.</returns>
        [PreserveSig]
        public DirectWriteFontWeight GetWeight();

        /// <summary>
        /// Gets the font stretch.
        /// </summary>
        /// <returns>The font stretch.</returns>
        [PreserveSig]
        public DirectWriteFontStretch GetStretch();

        /// <summary>
        /// Gets the font style.
        /// </summary>
        /// <returns>The font style.</returns>
        [PreserveSig]
        public DirectWriteFontStyle GetStyle();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::IsSymbolFont</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int IsSymbolFont();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::GetFaceNames</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFaceNames();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::GetInformationalStrings</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetInformationalStrings();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::GetSimulations</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetSimulations();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::GetMetrics</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetMetrics();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::HasCharacter</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int HasCharacter();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFont::CreateFontFace</c> vtable slot.
        /// </summary>
        /// <param name="fontFace">The returned font face.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int CreateFontFace([MarshalAs(UnmanagedType.Interface)] out IDWriteFontFace? fontFace);
    }

    /// <summary>
    /// Represents a DirectWrite font family.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteFontFamily</c>,
    /// IID <c>da20d8ef-812a-4c43-9802-62ec4abd7add</c>. The first three methods
    /// preserve inherited <c>IDWriteFontList</c> vtable slots.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("DA20D8EF-812A-4C43-9802-62EC4ABD7ADD")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteFontFamily
    {
        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontList::GetFontCollection</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFontCollection();

        /// <summary>
        /// Gets the number of fonts in the family.
        /// </summary>
        /// <returns>The number of fonts in the family.</returns>
        [PreserveSig]
        public uint GetFontCount();

        /// <summary>
        /// Gets a font by index.
        /// </summary>
        /// <param name="index">The font index.</param>
        /// <param name="font">The returned font.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFont(uint index, [MarshalAs(UnmanagedType.Interface)] out IDWriteFont? font);

        /// <summary>
        /// Gets the localized family names.
        /// </summary>
        /// <param name="names">The returned localized family names.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFamilyNames([MarshalAs(UnmanagedType.Interface)] out IDWriteLocalizedStrings? names);

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontFamily::GetFirstMatchingFont</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetFirstMatchingFont();

        /// <summary>
        /// Preserves the DirectWrite <c>IDWriteFontFamily::GetMatchingFonts</c> vtable slot.
        /// </summary>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetMatchingFonts();
    }

    /// <summary>
    /// Represents DirectWrite localized strings.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>IDWriteLocalizedStrings</c>,
    /// IID <c>08256209-099a-4b34-b86d-c22b110e7771</c>.
    /// </remarks>
    [GeneratedComInterface]
    [Guid("08256209-099A-4B34-B86D-C22B110E7771")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IDWriteLocalizedStrings
    {
        /// <summary>
        /// Gets the number of localized strings.
        /// </summary>
        /// <returns>The number of localized strings.</returns>
        [PreserveSig]
        public uint GetCount();

        /// <summary>
        /// Finds a localized string by locale name.
        /// </summary>
        /// <param name="localeName">The locale name to find.</param>
        /// <param name="index">The returned string index.</param>
        /// <param name="exists">The returned existence flag.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int FindLocaleName(
            [MarshalAs(UnmanagedType.LPWStr)] string localeName,
            out uint index,
            out int exists);

        /// <summary>
        /// Gets the locale name length.
        /// </summary>
        /// <param name="index">The string index.</param>
        /// <param name="length">The returned locale name length.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetLocaleNameLength(uint index, out uint length);

        /// <summary>
        /// Gets the locale name.
        /// </summary>
        /// <param name="index">The string index.</param>
        /// <param name="localeName">The returned locale name.</param>
        /// <param name="size">The returned buffer size.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetLocaleName(
            uint index,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 2)] char[] localeName,
            uint size);

        /// <summary>
        /// Gets the localized string length.
        /// </summary>
        /// <param name="index">The string index.</param>
        /// <param name="length">The returned localized string length.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public int GetStringLength(uint index, out uint length);

        /// <summary>
        /// Gets the localized string.
        /// </summary>
        /// <param name="index">The string index.</param>
        /// <param name="value">The returned localized string.</param>
        /// <param name="size">The returned buffer size.</param>
        /// <returns>The operation result.</returns>
        [PreserveSig]
        public unsafe int GetString(
            uint index,
            char* value,
            uint size);
    }

    /// <summary>
    /// Tries to get the Windows default font family name.
    /// </summary>
    /// <param name="familyName">The Windows default font family name.</param>
    /// <returns><see langword="true"/> if Windows returned a default font family name; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetDefaultFamilyName([NotNullWhen(true)] out string? familyName)
    {
        familyName = null;

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            return TryGetDefaultFamilyNameWindows(out familyName);
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
    /// Tries to match a character to a Windows system font.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if DirectWrite matched a system font; otherwise, <see langword="false"/>.</returns>
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

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return TryMatchCharacterWindows(codePoint, style, familyName, culture, out matchedFamilyName, out matchedStyle);
    }

    /// <summary>
    /// Tries to enumerate installed font family names through DirectWrite.
    /// </summary>
    /// <param name="checkForUpdates">Whether DirectWrite should check for updates to the system font collection.</param>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if DirectWrite returned family names; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyNames(bool checkForUpdates, [NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return TryGetFamilyNamesWindows(checkForUpdates, out familyNames);
    }

    /// <summary>
    /// Tries to enumerate installed font faces through DirectWrite.
    /// </summary>
    /// <param name="checkForUpdates">Whether DirectWrite should check for updates to the system font collection.</param>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if DirectWrite returned font faces; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetFamilyFaces(bool checkForUpdates, [NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return TryGetFamilyFacesWindows(checkForUpdates, out faces);
    }

    /// <summary>
    /// Tries to match a character through DirectWrite.
    /// </summary>
    /// <param name="codePoint">The code point to match.</param>
    /// <param name="style">The requested font style.</param>
    /// <param name="familyName">The requested family name.</param>
    /// <param name="culture">The requested culture.</param>
    /// <param name="matchedFamilyName">The matched family name.</param>
    /// <param name="matchedStyle">The matched style.</param>
    /// <returns><see langword="true"/> if DirectWrite matched a system font; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static unsafe bool TryMatchCharacterWindows(
        CodePoint codePoint,
        FontStyle style,
        string? familyName,
        CultureInfo? culture,
        out string? matchedFamilyName,
        out FontStyle matchedStyle)
    {
        matchedFamilyName = null;
        matchedStyle = default;

        DirectWriteObjects? objects = LazyObjects.Value;
        if (objects is null)
        {
            return false;
        }

        Span<char> text = stackalloc char[2];
        text = text[..GetUtf16(codePoint, text)];
        string localeName = GetLocaleName(culture);
        string? baseFamilyName = string.IsNullOrEmpty(familyName) ? null : familyName;

        fixed (char* textPointer = text)
        {
            fixed (char* localeNamePointer = localeName)
            {
                TextAnalysisSource source = new(textPointer, (uint)text.Length, localeNamePointer);

                int result = objects.FontFallback.MapCharacters(
                    source,
                    textPosition: 0,
                    textLength: (uint)text.Length,
                    baseFontCollection: IntPtr.Zero,
                    baseFamilyName,
                    ToDirectWriteFontWeight(style),
                    ToDirectWriteFontStyle(style),
                    DirectWriteFontStretch.Normal,
                    out _,
                    out IDWriteFont? mappedFont,
                    out _);

                if (result < 0 || mappedFont is null)
                {
                    return false;
                }

                try
                {
                    if (!TryGetFamilyName(mappedFont, localeName, out matchedFamilyName))
                    {
                        return false;
                    }

                    matchedStyle = ToFontStyle(mappedFont.GetWeight(), mappedFont.GetStyle());
                    return true;
                }
                finally
                {
                    DisposeComWrapper(mappedFont);
                }
            }
        }
    }

    /// <summary>
    /// Tries to enumerate installed font family names through DirectWrite on Windows.
    /// </summary>
    /// <param name="checkForUpdates">Whether DirectWrite should check for updates to the system font collection.</param>
    /// <param name="familyNames">The installed font family names.</param>
    /// <returns><see langword="true"/> if DirectWrite returned family names; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool TryGetFamilyNamesWindows(bool checkForUpdates, [NotNullWhen(true)] out string[]? familyNames)
    {
        familyNames = null;

        DirectWriteObjects? objects = LazyObjects.Value;
        if (objects is null)
        {
            return false;
        }

        IDWriteFontCollection? collection = null;

        try
        {
            if (objects.Factory.GetSystemFontCollection(out collection, checkForUpdates) < 0 || collection is null)
            {
                return false;
            }

            int count = checked((int)collection.GetFontFamilyCount());
            string[] names = new string[count];
            string localeName = CultureInfo.CurrentUICulture.Name;
            int nameCount = 0;

            for (int i = 0; i < count; i++)
            {
                IDWriteFontFamily? family = null;
                IDWriteLocalizedStrings? localizedNames = null;

                try
                {
                    if (collection.GetFontFamily((uint)i, out family) < 0 || family is null)
                    {
                        continue;
                    }

                    if (family.GetFamilyNames(out localizedNames) < 0 || localizedNames is null)
                    {
                        continue;
                    }

                    if ((TryGetLocalizedString(localizedNames, localeName, out string? familyName)
                            || TryGetString(localizedNames, index: 0, out familyName))
                        && familyName is { Length: > 0 })
                    {
                        names[nameCount++] = familyName;
                    }
                }
                finally
                {
                    DisposeComWrapper(localizedNames);
                    DisposeComWrapper(family);
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
            DisposeComWrapper(collection);
        }
    }

    /// <summary>
    /// Tries to enumerate installed font faces through DirectWrite on Windows.
    /// </summary>
    /// <param name="checkForUpdates">Whether DirectWrite should check for updates to the system font collection.</param>
    /// <param name="faces">The installed system font faces.</param>
    /// <returns><see langword="true"/> if DirectWrite returned font faces; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool TryGetFamilyFacesWindows(bool checkForUpdates, [NotNullWhen(true)] out NativeSystemFontFace[]? faces)
    {
        faces = null;

        DirectWriteObjects? objects = LazyObjects.Value;
        if (objects is null)
        {
            return false;
        }

        IDWriteFontCollection? collection = null;

        try
        {
            if (objects.Factory.GetSystemFontCollection(out collection, checkForUpdates) < 0 || collection is null)
            {
                return false;
            }

            uint familyCount = collection.GetFontFamilyCount();
            string localeName = CultureInfo.CurrentUICulture.Name;
            List<NativeSystemFontFace> results = [];

            for (uint familyIndex = 0; familyIndex < familyCount; familyIndex++)
            {
                IDWriteFontFamily? family = null;
                IDWriteLocalizedStrings? localizedNames = null;

                try
                {
                    if (collection.GetFontFamily(familyIndex, out family) < 0 || family is null)
                    {
                        continue;
                    }

                    if (family.GetFamilyNames(out localizedNames) < 0 || localizedNames is null)
                    {
                        continue;
                    }

                    if ((!TryGetLocalizedString(localizedNames, localeName, out string? familyName)
                            && !TryGetString(localizedNames, index: 0, out familyName))
                        || familyName is not { Length: > 0 })
                    {
                        continue;
                    }

                    uint fontCount = family.GetFontCount();

                    for (uint fontIndex = 0; fontIndex < fontCount; fontIndex++)
                    {
                        IDWriteFont? font = null;

                        try
                        {
                            if (family.GetFont(fontIndex, out font) < 0 || font is null)
                            {
                                continue;
                            }

                            if (TryGetFontFacePath(font, out string? path, out int faceIndex))
                            {
                                DirectWriteFontWeight weight = font.GetWeight();
                                FontStyle faceStyle = ToFontStyle(weight, font.GetStyle());

                                results.Add(new NativeSystemFontFace(
                                    familyName,
                                    path,
                                    faceStyle,
                                    GetStyleScore(weight, faceStyle),
                                    faceIndex));
                            }
                        }
                        finally
                        {
                            DisposeComWrapper(font);
                        }
                    }
                }
                finally
                {
                    DisposeComWrapper(localizedNames);
                    DisposeComWrapper(family);
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
            DisposeComWrapper(collection);
        }
    }

    /// <summary>
    /// Gets the Windows message font family name.
    /// </summary>
    /// <param name="familyName">The font family name.</param>
    /// <returns><see langword="true"/> if Windows returned a message font family name; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool TryGetDefaultFamilyNameWindows([NotNullWhen(true)] out string? familyName)
    {
        NonClientMetrics metrics = new()
        {
            Size = Marshal.SizeOf<NonClientMetrics>()
        };

        if (SystemParametersInfo(SpiGetNonClientMetrics, (uint)metrics.Size, ref metrics, 0))
        {
            familyName = GetFaceName(metrics.MessageFont);
            return familyName.Length > 0;
        }

        familyName = null;
        return false;
    }

    /// <summary>
    /// Gets the local font file path and face index for a DirectWrite font.
    /// </summary>
    /// <param name="font">The DirectWrite font.</param>
    /// <param name="path">The local font file path.</param>
    /// <param name="faceIndex">The zero-based face index within the font file.</param>
    /// <returns><see langword="true"/> if a local font file path was found; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool TryGetFontFacePath(IDWriteFont font, [NotNullWhen(true)] out string? path, out int faceIndex)
    {
        path = null;
        faceIndex = 0;
        IDWriteFontFace? fontFace = null;

        try
        {
            if (font.CreateFontFace(out fontFace) < 0 || fontFace is null)
            {
                return false;
            }

            uint fileCount = 0;
            if (fontFace.GetFiles(ref fileCount, null) < 0 || fileCount == 0)
            {
                return false;
            }

            IDWriteFontFile?[] fontFiles = new IDWriteFontFile?[fileCount];
            uint requestedFileCount = fileCount;

            if (fontFace.GetFiles(ref requestedFileCount, fontFiles) < 0 || requestedFileCount == 0)
            {
                return false;
            }

            faceIndex = checked((int)fontFace.GetIndex());

            try
            {
                for (int i = 0; i < requestedFileCount; i++)
                {
                    IDWriteFontFile? fontFile = fontFiles[i];
                    if (fontFile is not null && TryGetFontFilePath(fontFile, out path))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                for (int i = 0; i < fontFiles.Length; i++)
                {
                    DisposeComWrapper(fontFiles[i]);
                }
            }

            return false;
        }
        finally
        {
            DisposeComWrapper(fontFace);
        }
    }

    /// <summary>
    /// Gets the local path for a DirectWrite font file.
    /// </summary>
    /// <param name="fontFile">The DirectWrite font file.</param>
    /// <param name="path">The local font file path.</param>
    /// <returns><see langword="true"/> if a local font file path was found; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static unsafe bool TryGetFontFilePath(IDWriteFontFile fontFile, [NotNullWhen(true)] out string? path)
    {
        path = null;

        if (fontFile.GetReferenceKey(out IntPtr referenceKey, out uint referenceKeySize) < 0 || referenceKey == IntPtr.Zero)
        {
            return false;
        }

        if (fontFile.GetLoader(out IDWriteLocalFontFileLoader? loader) < 0 || loader is null)
        {
            return false;
        }

        try
        {
            if (loader.GetFilePathLengthFromKey(referenceKey, referenceKeySize, out uint pathLength) < 0)
            {
                return false;
            }

            using Buffer<char> buffer = new(checked((int)pathLength + 1));
            Span<char> pathBuffer = buffer.GetSpan()[..((int)pathLength + 1)];

            fixed (char* pathPointer = pathBuffer)
            {
                if (loader.GetFilePathFromKey(referenceKey, referenceKeySize, pathPointer, (uint)pathBuffer.Length) < 0)
                {
                    return false;
                }

                path = new string(pathPointer, startIndex: 0, length: (int)pathLength);
                return path.Length > 0;
            }
        }
        finally
        {
            DisposeComWrapper(loader);
        }
    }

    /// <summary>
    /// Creates the process-wide DirectWrite objects used by fallback matching.
    /// </summary>
    /// <returns>The DirectWrite objects, or <see langword="null"/> when unavailable.</returns>
    private static DirectWriteObjects? CreateObjects()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        return CreateObjectsWindows();
    }

    /// <summary>
    /// Creates the process-wide DirectWrite objects used by fallback matching on Windows.
    /// </summary>
    /// <returns>The DirectWrite objects, or <see langword="null"/> when unavailable.</returns>
    [SupportedOSPlatform("windows")]
    private static DirectWriteObjects? CreateObjectsWindows()
    {
        Guid factoryId = new("0439FC60-CA44-4994-8DEE-3A9AF7B732EC");
        int result = DWriteCreateFactory(DirectWriteFactoryType.Shared, ref factoryId, out IDWriteFactory2? factory);

        if (result < 0 || factory is null)
        {
            return null;
        }

        result = factory.GetSystemFontFallback(out IDWriteFontFallback? fontFallback);
        if (result < 0 || fontFallback is null)
        {
            DisposeComWrapper(factory);
            return null;
        }

        return new DirectWriteObjects(factory, fontFallback);
    }

    /// <summary>
    /// Gets the DirectWrite locale name to use for fallback matching.
    /// </summary>
    /// <param name="culture">The requested culture.</param>
    /// <returns>The locale name.</returns>
    private static string GetLocaleName(CultureInfo? culture)
    {
        string localeName = culture?.Name ?? CultureInfo.CurrentCulture.Name;

        return localeName;
    }

    /// <summary>
    /// Gets the DirectWrite font weight for a Fonts style.
    /// </summary>
    /// <param name="style">The Fonts style.</param>
    /// <returns>The DirectWrite font weight.</returns>
    private static DirectWriteFontWeight ToDirectWriteFontWeight(FontStyle style)
        => (style & FontStyle.Bold) == FontStyle.Bold ? DirectWriteFontWeight.Bold : DirectWriteFontWeight.Normal;

    /// <summary>
    /// Gets the DirectWrite font style for a Fonts style.
    /// </summary>
    /// <param name="style">The Fonts style.</param>
    /// <returns>The DirectWrite font style.</returns>
    private static DirectWriteFontStyle ToDirectWriteFontStyle(FontStyle style)
        => (style & FontStyle.Italic) == FontStyle.Italic ? DirectWriteFontStyle.Italic : DirectWriteFontStyle.Normal;

    /// <summary>
    /// Gets the Fonts style for a DirectWrite weight and style.
    /// </summary>
    /// <param name="weight">The DirectWrite font weight.</param>
    /// <param name="style">The DirectWrite font style.</param>
    /// <returns>The Fonts style.</returns>
    private static FontStyle ToFontStyle(DirectWriteFontWeight weight, DirectWriteFontStyle style)
    {
        FontStyle result = weight >= DirectWriteFontWeight.SemiBold ? FontStyle.Bold : FontStyle.Regular;

        if (style is DirectWriteFontStyle.Italic or DirectWriteFontStyle.Oblique)
        {
            result |= FontStyle.Italic;
        }

        return result;
    }

    /// <summary>
    /// Gets a preference score for a DirectWrite face inside a Fonts style bucket.
    /// </summary>
    /// <param name="weight">The DirectWrite font weight.</param>
    /// <param name="style">The Fonts style bucket.</param>
    /// <returns>The face preference score.</returns>
    private static int GetStyleScore(DirectWriteFontWeight weight, FontStyle style)
    {
        int targetWeight = (style & FontStyle.Bold) == FontStyle.Bold
            ? (int)DirectWriteFontWeight.Bold
            : (int)DirectWriteFontWeight.Normal;

        return Math.Abs((int)weight - targetWeight);
    }

    /// <summary>
    /// Gets the family name returned by DirectWrite for a font.
    /// </summary>
    /// <param name="font">The font.</param>
    /// <param name="localeName">The preferred locale name.</param>
    /// <param name="familyName">The family name.</param>
    /// <returns><see langword="true"/> if a family name was found; otherwise, <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool TryGetFamilyName(IDWriteFont font, string localeName, out string? familyName)
    {
        familyName = null;
        IDWriteFontFamily? fontFamily = null;
        IDWriteLocalizedStrings? familyNames = null;

        try
        {
            if (font.GetFontFamily(out fontFamily) < 0 || fontFamily is null)
            {
                return false;
            }

            if (fontFamily.GetFamilyNames(out familyNames) < 0 || familyNames is null)
            {
                return false;
            }

            return TryGetLocalizedString(familyNames, localeName, out familyName)
                || TryGetString(familyNames, index: 0, out familyName);
        }
        finally
        {
            DisposeComWrapper(familyNames);
            DisposeComWrapper(fontFamily);
        }
    }

    /// <summary>
    /// Gets a localized string for the requested locale.
    /// </summary>
    /// <param name="strings">The DirectWrite localized strings.</param>
    /// <param name="localeName">The locale name.</param>
    /// <param name="value">The localized string.</param>
    /// <returns><see langword="true"/> if a localized string was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetLocalizedString(IDWriteLocalizedStrings strings, string localeName, out string? value)
    {
        value = null;

        if (strings.FindLocaleName(localeName, out uint index, out int exists) < 0 || exists == 0)
        {
            return false;
        }

        return TryGetString(strings, index, out value);
    }

    /// <summary>
    /// Gets a localized string by index.
    /// </summary>
    /// <param name="strings">The DirectWrite localized strings.</param>
    /// <param name="index">The string index.</param>
    /// <param name="value">The localized string.</param>
    /// <returns><see langword="true"/> if a localized string was found; otherwise, <see langword="false"/>.</returns>
    private static unsafe bool TryGetString(IDWriteLocalizedStrings strings, uint index, out string? value)
    {
        value = null;

        if (index >= strings.GetCount() || strings.GetStringLength(index, out uint length) < 0)
        {
            return false;
        }

        using Buffer<char> buffer = new((int)length + 1);
        Span<char> text = buffer.GetSpan()[..((int)length + 1)];

        fixed (char* textPointer = text)
        {
            if (strings.GetString(index, textPointer, (uint)text.Length) < 0)
            {
                return false;
            }

            value = new string(textPointer, startIndex: 0, length: (int)length);
            return value.Length > 0;
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
    /// Gets the face name from a Windows <c>LOGFONTW</c>.
    /// </summary>
    /// <param name="font">The font descriptor.</param>
    /// <returns>The face name.</returns>
    private static unsafe string GetFaceName(LogFont font)
    {
        char* faceName = font.FaceName;
        int length = 0;

        while (length < LfFaceSize && faceName[length] != '\0')
        {
            length++;
        }

        return new string(faceName, startIndex: 0, length);
    }

    /// <summary>
    /// Disposes a source-generated COM wrapper obtained from DirectWrite.
    /// </summary>
    /// <param name="value">The COM wrapper to dispose.</param>
    [SupportedOSPlatform("windows")]
    private static void DisposeComWrapper(object? value)
    {
        // Source-generated COM wrappers are backed by ComWrappers rather than classic
        // runtime-callable wrappers, so Marshal.ReleaseComObject is not valid here.
        if (value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Creates a DirectWrite factory.
    /// </summary>
    /// <remarks>
    /// Windows SDK source: <c>dwrite.h</c>, <c>DWriteCreateFactory</c>. The requested
    /// interface identifier is <c>IDWriteFactory2</c> from <c>dwrite_2.h</c>.
    /// </remarks>
    /// <param name="factoryType">The factory type.</param>
    /// <param name="iid">The requested factory interface identifier.</param>
    /// <param name="factory">The returned factory interface.</param>
    /// <returns>The operation result.</returns>
    [LibraryImport("dwrite.dll", EntryPoint = "DWriteCreateFactory")]
    private static partial int DWriteCreateFactory(
        DirectWriteFactoryType factoryType,
        ref Guid iid,
        [MarshalAs(UnmanagedType.Interface)] out IDWriteFactory2? factory);

    /// <summary>
    /// Retrieves Windows system-wide parameters.
    /// </summary>
    /// <param name="action">The system parameter action.</param>
    /// <param name="parameter">The action parameter.</param>
    /// <param name="metrics">The non-client metrics.</param>
    /// <param name="update">The update flags.</param>
    /// <returns><see langword="true"/> if the system parameter was retrieved.</returns>
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint parameter, ref NonClientMetrics metrics, uint update);

    /// <summary>
    /// Contains Windows non-client area metrics.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NonClientMetrics
    {
        public int Size;
        public int BorderWidth;
        public int ScrollWidth;
        public int ScrollHeight;
        public int CaptionWidth;
        public int CaptionHeight;
        public LogFont CaptionFont;
        public int SmallCaptionWidth;
        public int SmallCaptionHeight;
        public LogFont SmallCaptionFont;
        public int MenuWidth;
        public int MenuHeight;
        public LogFont MenuFont;
        public LogFont StatusFont;
        public LogFont MessageFont;
        public int PaddedBorderWidth;
    }

    /// <summary>
    /// Contains Windows logical font attributes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct LogFont
    {
        public int Height;
        public int Width;
        public int Escapement;
        public int Orientation;
        public int Weight;
        public byte Italic;
        public byte Underline;
        public byte StrikeOut;
        public byte CharSet;
        public byte OutPrecision;
        public byte ClipPrecision;
        public byte Quality;
        public byte PitchAndFamily;
        public fixed char FaceName[LfFaceSize];
    }

    /// <summary>
    /// Holds the DirectWrite interfaces reused for fallback matching.
    /// </summary>
    private sealed class DirectWriteObjects
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectWriteObjects"/> class.
        /// </summary>
        /// <param name="factory">The DirectWrite factory.</param>
        /// <param name="fontFallback">The DirectWrite font fallback interface.</param>
        public DirectWriteObjects(IDWriteFactory2 factory, IDWriteFontFallback fontFallback)
        {
            this.Factory = factory;
            this.FontFallback = fontFallback;
        }

        /// <summary>
        /// Gets the DirectWrite factory.
        /// </summary>
        public IDWriteFactory2 Factory { get; }

        /// <summary>
        /// Gets the DirectWrite font fallback interface.
        /// </summary>
        public IDWriteFontFallback FontFallback { get; }
    }

    /// <summary>
    /// Supplies a single code point to DirectWrite text analysis.
    /// </summary>
    /// <remarks>
    /// DirectWrite fallback consumes text through <c>IDWriteTextAnalysisSource</c> rather than
    /// directly accepting a scalar value, so this class exposes the one code point and its locale
    /// through the callback interface expected by <c>IDWriteFontFallback.MapCharacters</c>.
    /// </remarks>
    [GeneratedComClass]
    internal sealed partial class TextAnalysisSource : IDWriteTextAnalysisSource
    {
        private const int Success = 0;

        private readonly IntPtr textPointer;
        private readonly IntPtr localeNamePointer;
        private readonly uint textLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextAnalysisSource"/> class.
        /// </summary>
        /// <param name="text">The UTF-16 text to analyze.</param>
        /// <param name="textLength">The UTF-16 text length.</param>
        /// <param name="localeName">The locale name for the text.</param>
        public unsafe TextAnalysisSource(char* text, uint textLength, char* localeName)
        {
            this.textPointer = (IntPtr)text;
            this.localeNamePointer = (IntPtr)localeName;
            this.textLength = textLength;
        }

        /// <inheritdoc/>
        public int GetTextAtPosition(uint textPosition, out IntPtr textString, out uint textLength)
        {
            if (textPosition >= this.textLength)
            {
                textString = IntPtr.Zero;
                textLength = 0;
                return Success;
            }

            textString = IntPtr.Add(this.textPointer, (int)(textPosition * sizeof(char)));
            textLength = this.textLength - textPosition;
            return Success;
        }

        /// <inheritdoc/>
        public int GetTextBeforePosition(uint textPosition, out IntPtr textString, out uint textLength)
        {
            if (textPosition == 0 || textPosition > this.textLength)
            {
                textString = IntPtr.Zero;
                textLength = 0;
                return Success;
            }

            textString = this.textPointer;
            textLength = textPosition;
            return Success;
        }

        /// <inheritdoc/>
        public DirectWriteReadingDirection GetParagraphReadingDirection()
            => DirectWriteReadingDirection.LeftToRight;

        /// <inheritdoc/>
        public int GetLocaleName(uint textPosition, out uint textLength, out IntPtr localeName)
        {
            if (textPosition >= this.textLength)
            {
                textLength = 0;
                localeName = IntPtr.Zero;
                return Success;
            }

            textLength = this.textLength - textPosition;
            localeName = this.localeNamePointer;
            return Success;
        }

        /// <inheritdoc/>
        public int GetNumberSubstitution(uint textPosition, out uint textLength, out IntPtr numberSubstitution)
        {
            textLength = textPosition < this.textLength ? this.textLength - textPosition : 0;
            numberSubstitution = IntPtr.Zero;
            return Success;
        }
    }
}
