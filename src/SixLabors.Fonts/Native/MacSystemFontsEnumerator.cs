// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static SixLabors.Fonts.Native.CoreFoundation;
using static SixLabors.Fonts.Native.CoreText;

namespace SixLabors.Fonts.Native
{
    /// <summary>
    /// An enumerator that enumerates over available macOS system fonts.
    /// The enumerated strings are the absolute paths to the font files.
    /// </summary>
    /// <remarks>
    /// Internally, it calls the native CoreText's <see cref="CTFontManagerCopyAvailableFontURLs"/> method to retrieve
    /// the list of fonts so using this class must be guarded by <c>RuntimeInformation.IsOSPlatform(OSPlatform.OSX)</c>.
    /// </remarks>
    internal class MacSystemFontsEnumerator : IEnumerable<string>, IEnumerator<string>
    {
        private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

        private readonly IntPtr fontUrls;
        private readonly bool releaseFontUrls;
        private int fontIndex;

        public MacSystemFontsEnumerator()
            : this(CTFontManagerCopyAvailableFontURLs(), releaseFontUrls: true, fontIndex: 0)
        {
        }

        private MacSystemFontsEnumerator(IntPtr fontUrls, bool releaseFontUrls, int fontIndex)
        {
            if (fontUrls == IntPtr.Zero)
            {
                throw new ArgumentException($"The {nameof(fontUrls)} must not be NULL.", nameof(fontUrls));
            }

            this.fontUrls = fontUrls;
            this.releaseFontUrls = releaseFontUrls;
            this.fontIndex = fontIndex;

            this.Current = null!;
        }

        public string Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            Debug.Assert(CFGetTypeID(this.fontUrls) == CFArrayGetTypeID(), "The fontUrls array must be a CFArrayRef");
            if (this.fontIndex < CFArrayGetCount(this.fontUrls))
            {
                IntPtr fontUrl = CFArrayGetValueAtIndex(this.fontUrls, this.fontIndex);
                Debug.Assert(CFGetTypeID(fontUrl) == CFURLGetTypeID(), "The elements of the fontUrls array must be a CFURLRef");
                IntPtr fontPath = CFURLCopyFileSystemPath(fontUrl, CFURLPathStyle.kCFURLPOSIXPathStyle);

#if !NETSTANDARD2_0
                var current = Marshal.PtrToStringUTF8(CFStringGetCStringPtr(fontPath, CFStringEncoding.kCFStringEncodingUTF8));
                if (current is not null)
                {
                    this.Current = current;
                }
                else
#endif
                {
                    var fontPathLength = (int)CFStringGetLength(fontPath);
                    var fontPathBufferSize = (fontPathLength + 1) * 2; // +1 for the NULL byte and *2 for UTF-16
                    var fontPathBuffer = BytePool.Rent(fontPathBufferSize);
                    CFStringGetCString(fontPath, fontPathBuffer, fontPathBufferSize, CFStringEncoding.kCFStringEncodingUTF16LE);
                    this.Current = Encoding.Unicode.GetString(fontPathBuffer, 0, fontPathBufferSize - 2); // -2 for the UTF-16 NULL
                    BytePool.Return(fontPathBuffer);
                }

                CFRelease(fontPath);

                this.fontIndex++;

                return true;
            }

            return false;
        }

        public void Reset() => this.fontIndex = 0;

        public void Dispose()
        {
            if (this.releaseFontUrls)
            {
                CFRelease(this.fontUrls);
            }
        }

        public IEnumerator<string> GetEnumerator() => new MacSystemFontsEnumerator(this.fontUrls, releaseFontUrls: false, this.fontIndex);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
