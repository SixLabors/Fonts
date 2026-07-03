// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;

namespace SixLabors.Fonts.Native;

// ReSharper disable InconsistentNaming
internal static partial class CoreFoundation
{
    private const string CoreFoundationFramework = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation";

    /// <summary>
    /// Defines Core Foundation number storage types used by native font metadata.
    /// </summary>
    internal enum CFNumberType
    {
        /// <summary>
        /// A Core Graphics floating-point value.
        /// </summary>
        CGFloat = 16
    }

    /// <summary>
    /// Returns the number of values currently in an array.
    /// </summary>
    /// <param name="theArray">The array to examine.</param>
    /// <returns>The number of values in <paramref name="theArray"/>.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial long CFArrayGetCount(IntPtr theArray);

    /// <summary>
    /// Retrieves a value at a given index.
    /// </summary>
    /// <param name="theArray">The array to examine.</param>
    /// <param name="idx">The index of the value to retrieve. If the index is outside the index space of <paramref name="theArray"/> (<c>0</c> to <c>N-1</c> inclusive where <c>N</c> is the count of <paramref name="theArray"/>), the behavior is undefined.</param>
    /// <returns>The value at the <paramref name="idx"/> index in <paramref name="theArray"/>. If the return value is a Core Foundation Object, ownership follows The Get Rule.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CFArrayGetValueAtIndex(IntPtr theArray, long idx);

    /// <summary>
    /// Returns the unique identifier of an opaque type to which a Core Foundation object belongs.
    /// </summary>
    /// <param name="cf">The CFType object to examine.</param>
    /// <returns>A value of type <c>CFTypeID</c> that identifies the opaque type of <paramref name="cf"/>.</returns>
    /// <remarks>
    /// This function returns a value that uniquely identifies the opaque type of any Core Foundation object.
    /// You can compare this value with the known <c>CFTypeID</c> identifier obtained with a "GetTypeID" function specific to a type, for example CFDateGetTypeID.
    /// These values might change from release to release or platform to platform.
    /// </remarks>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CFGetTypeID(IntPtr cf);

    /// <summary>
    /// Returns the type identifier for the CFDictionary opaque type.
    /// </summary>
    /// <returns>The type identifier for the CFDictionary opaque type.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CFDictionaryGetTypeID();

    /// <summary>
    /// Retrieves a dictionary value for the specified key without retaining the value.
    /// </summary>
    /// <param name="theDict">The dictionary to inspect.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="value">The matching value.</param>
    /// <returns><see langword="true"/> when the key exists; otherwise, <see langword="false"/>.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool CFDictionaryGetValueIfPresent(IntPtr theDict, IntPtr key, out IntPtr value);

    /// <summary>
    /// Returns the type identifier for the CFNumber opaque type.
    /// </summary>
    /// <returns>The type identifier for the CFNumber opaque type.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CFNumberGetTypeID();

    /// <summary>
    /// Copies a Core Foundation number into a double precision floating-point value.
    /// </summary>
    /// <param name="number">The number to read.</param>
    /// <param name="theType">The requested output type.</param>
    /// <param name="value">The converted value.</param>
    /// <returns><see langword="true"/> when conversion succeeds; otherwise, <see langword="false"/>.</returns>
    [LibraryImport(CoreFoundationFramework, EntryPoint = "CFNumberGetValue")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.I1)]
    public static partial bool CFNumberGetDoubleValue(IntPtr number, CFNumberType theType, out double value);

    /// <summary>
    /// Returns the number (in terms of UTF-16 code pairs) of Unicode characters in a string.
    /// </summary>
    /// <param name="theString">The string to examine.</param>
    /// <returns>The number (in terms of UTF-16 code pairs) of characters stored in <paramref name="theString"/>.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial long CFStringGetLength(IntPtr theString);

    /// <summary>
    /// Copies the UTF-16 characters of a string into a caller-provided buffer.
    /// </summary>
    /// <param name="theString">The string whose contents you wish to access.</param>
    /// <param name="range">The range of characters to copy.</param>
    /// <param name="buffer">The caller-provided UTF-16 character buffer.</param>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial void CFStringGetCharacters(IntPtr theString, CoreText.CFRange range, char* buffer);

    /// <summary>
    /// Creates an immutable string from UTF-16 characters.
    /// </summary>
    /// <param name="allocator">The allocator to use, or <c>NULL</c> for the default allocator.</param>
    /// <param name="characters">The UTF-16 characters.</param>
    /// <param name="numberOfCharacters">The number of characters to read from <paramref name="characters"/>.</param>
    /// <returns>
    /// A retained <c>CFString</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned string.
    /// </returns>
    /// <remarks>
    /// Core Foundation follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial IntPtr CFStringCreateWithCharacters(
        IntPtr allocator,
        char* characters,
        nint numberOfCharacters);

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
    /// <remarks>This function is useful when you need your own copy of a string’s character data as a C string. You also typically call it as a "backup" when a prior call to the <see cref="CFStringGetCStringPtr"/> function fails.</remarks>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    [return: MarshalAs(UnmanagedType.I1)]
    public static unsafe partial bool CFStringGetCString(IntPtr theString, byte* buffer, long bufferSize, CFStringEncoding encoding);

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
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CFStringGetCStringPtr(IntPtr theString, CFStringEncoding encoding);

    /// <summary>
    /// Releases a Core Foundation object.
    /// </summary>
    /// <param name="cf">A CFType object to release. This value must not be <c>NULL</c>.</param>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial void CFRelease(IntPtr cf);

    /// <summary>
    /// Returns the path portion of a given URL.
    /// </summary>
    /// <param name="anURL">The <c>CFURL</c> object whose path you want to obtain.</param>
    /// <param name="pathStyle">The operating system path style to be used to create the path. See <see cref="CFURLPathStyle"/> for a list of possible values.</param>
    /// <returns>The URL's path in the format specified by <paramref name="pathStyle"/>. Ownership follows the create rule. See The Create Rule.</returns>
    /// <remarks>This function returns the URL's path as a file system path for a given path style.</remarks>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial IntPtr CFURLCopyFileSystemPath(IntPtr anURL, CFURLPathStyle pathStyle);

    /// <summary>
    /// Returns the type identifier for the CFURL opaque type.
    /// </summary>
    /// <returns>The type identifier for the CFURL opaque type.</returns>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static partial ulong CFURLGetTypeID();

    /// <summary>
    /// Creates an immutable string from an encoded byte buffer.
    /// </summary>
    /// <param name="allocator">The allocator to use, or <c>NULL</c> for the default allocator.</param>
    /// <param name="bytes">The encoded string bytes.</param>
    /// <param name="numberOfBytes">The number of bytes to read from <paramref name="bytes"/>.</param>
    /// <param name="encoding">The string encoding used by <paramref name="bytes"/>.</param>
    /// <param name="isExternalRepresentation">Whether the bytes contain an external representation.</param>
    /// <returns>
    /// A retained <c>CFString</c>, or <c>NULL</c> on error. The caller is responsible for releasing
    /// the returned string.
    /// </returns>
    /// <remarks>
    /// Core Foundation follows the Create Rule for this API, so a non-<c>NULL</c> result must be
    /// released with <see cref="CFRelease"/>.
    /// </remarks>
    [LibraryImport(CoreFoundationFramework)]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial IntPtr CFStringCreateWithBytes(
        IntPtr allocator,
        byte* bytes,
        nint numberOfBytes,
        CFStringEncoding encoding,
        byte isExternalRepresentation);
}
