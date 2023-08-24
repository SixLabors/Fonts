// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Native;

// ReSharper disable InconsistentNaming
internal static class CoreFoundation
{
    private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";

    /// <summary>
    /// Returns the number of values currently in an array.
    /// </summary>
    /// <param name="theArray">The array to examine.</param>
    /// <returns>The number of values in <paramref name="theArray"/>.</returns>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern long CFArrayGetCount(IntPtr theArray);

    /// <summary>
    /// Returns the type identifier for the CFArray opaque type.
    /// </summary>
    /// <returns>The type identifier for the CFArray opaque type.</returns>
    /// <remarks>CFMutableArray objects have the same type identifier as CFArray objects.</remarks>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern ulong CFArrayGetTypeID();

    /// <summary>
    /// Retrieves a value at a given index.
    /// </summary>
    /// <param name="theArray">The array to examine.</param>
    /// <param name="idx">The index of the value to retrieve. If the index is outside the index space of <paramref name="theArray"/> (<c>0</c> to <c>N-1</c> inclusive where <c>N</c> is the count of <paramref name="theArray"/>), the behavior is undefined.</param>
    /// <returns>The value at the <paramref name="idx"/> index in <paramref name="theArray"/>. If the return value is a Core Foundation Object, ownership follows The Get Rule.</returns>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, long idx);

    /// <summary>
    /// Returns the unique identifier of an opaque type to which a Core Foundation object belongs.
    /// </summary>
    /// <param name="cf">The CFType object to examine.</param>
    /// <returns>A value of type <c>CFTypeID</c> that identifies the opaque type of <paramref name="cf"/>.</returns>
    /// <remarks>
    /// This function returns a value that uniquely identifies the opaque type of any Core Foundation object.
    /// You can compare this value with the known <c>CFTypeID</c> identifier obtained with a “GetTypeID” function specific to a type, for example CFDateGetTypeID.
    /// These values might change from release to release or platform to platform.
    /// </remarks>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern ulong CFGetTypeID(IntPtr cf);

    /// <summary>
    /// Returns the number (in terms of UTF-16 code pairs) of Unicode characters in a string.
    /// </summary>
    /// <param name="theString">The string to examine.</param>
    /// <returns>The number (in terms of UTF-16 code pairs) of characters stored in <paramref name="theString"/>.</returns>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern long CFStringGetLength(IntPtr theString);

    /// <summary>
    /// Copies the character contents of a string to a local C string buffer after converting the characters to a given encoding.
    /// </summary>
    /// <param name="theString">The string whose contents you wish to access.</param>
    /// <param name="buffer">
    /// The C string buffer into which to copy the string. On return, the buffer contains the converted characters. If there is an error in conversion, the buffer contains only partial results.
    /// The buffer must be large enough to contain the converted characters and a NUL terminator. For example, if the string is <c>Toby</c>, the buffer must be at least 5 bytes long.
    /// </param>
    /// <param name="bufferSize">The length of <paramref name="buffer"/> in bytes.</param>
    /// <param name="encoding">The string encoding to which the character contents of <paramref name="theString"/> should be converted. The encoding must specify an 8-bit encoding.</param>
    /// <returns><see langword="true"/> upon success or <see langword="false"/> if the conversion fails or the provided buffer is too small.</returns>
    /// <remarks>This function is useful when you need your own copy of a string’s character data as a C string. You also typically call it as a “backup” when a prior call to the <see cref="CFStringGetCStringPtr"/> function fails.</remarks>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern bool CFStringGetCString(IntPtr theString, byte[] buffer, long bufferSize, CFStringEncoding encoding);

    /// <summary>
    /// Quickly obtains a pointer to a C-string buffer containing the characters of a string in a given encoding.
    /// </summary>
    /// <param name="theString">The string whose contents you wish to access.</param>
    /// <param name="encoding">The string encoding to which the character contents of <paramref name="theString"/> should be converted. The encoding must specify an 8-bit encoding.</param>
    /// <returns>A pointer to a C string or <c>NULL</c> if the internal storage of <paramref name="theString"/> does not allow this to be returned efficiently.</returns>
    /// <remarks>
    /// <para>
    /// This function either returns the requested pointer immediately, with no memory allocations and no copying, in constant time, or returns <c>NULL</c>. If the latter is the result, call an alternative function such as the <see cref="CFStringGetCString"/> function to extract the characters.
    /// </para>
    /// <para>
    /// Whether or not this function returns a valid pointer or <c>NULL</c> depends on many factors, all of which depend on how the string was created and its properties. In addition, the function result might change between different releases and on different platforms. So do not count on receiving a non-<c>NULL</c> result from this function under any circumstances.
    /// </para>
    /// </remarks>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern IntPtr CFStringGetCStringPtr(IntPtr theString, CFStringEncoding encoding);

    /// <summary>
    /// Releases a Core Foundation object.
    /// </summary>
    /// <param name="cf">A CFType object to release. This value must not be <c>NULL</c>.</param>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern void CFRelease(IntPtr cf);

    /// <summary>
    /// Returns the path portion of a given URL.
    /// </summary>
    /// <param name="anURL">The <c>CFURL</c> object whose path you want to obtain.</param>
    /// <param name="pathStyle">The operating system path style to be used to create the path. See <see cref="CFURLPathStyle"/> for a list of possible values.</param>
    /// <returns>The URL's path in the format specified by <paramref name="pathStyle"/>. Ownership follows the create rule. See The Create Rule.</returns>
    /// <remarks>This function returns the URL's path as a file system path for a given path style.</remarks>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern IntPtr CFURLCopyFileSystemPath(IntPtr anURL, CFURLPathStyle pathStyle);

    /// <summary>
    /// Returns the type identifier for the CFURL opaque type.
    /// </summary>
    /// <returns>The type identifier for the CFURL opaque type.</returns>
    [DllImport(CoreFoundationFramework, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "SYSLIB1054:Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time", Justification = ".NET7 Only")]
    public static extern ulong CFURLGetTypeID();
}
