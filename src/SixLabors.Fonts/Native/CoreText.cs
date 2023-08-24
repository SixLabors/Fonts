// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Native;

internal static class CoreText
{
    private const string CoreTextFramework = "/System/Library/Frameworks/CoreText.framework/Versions/A/CoreText";

    /// <summary>
    /// Returns an array of font URLs.
    /// </summary>
    /// <returns>This function returns a retained reference to a <c>CFArray</c> of <c>CFURLRef</c> objects representing the URLs of the available fonts, or <c>NULL</c> on error. The caller is responsible for releasing the array.</returns>
    [DllImport(CoreTextFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CTFontManagerCopyAvailableFontURLs();
}
