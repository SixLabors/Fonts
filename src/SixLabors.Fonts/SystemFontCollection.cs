// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    internal sealed class SystemFontCollection : IReadOnlySystemFontCollection, IReadOnlyFontMetricsCollection
    {
        private readonly FontCollection collection;
        private readonly IReadOnlyCollection<string> searchDirectories;

        /// <summary>
        /// Gets the default set of locations we probe for System Fonts.
        /// </summary>
        private static readonly IReadOnlyCollection<string> StandardFontLocations;

        static SystemFontCollection()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                StandardFontLocations = new[]
                {
                    @"%SYSTEMROOT%\Fonts",
                    @"%APPDATA%\Microsoft\Windows\Fonts",
                    @"%LOCALAPPDATA%\Microsoft\Windows\Fonts",
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                StandardFontLocations = new[]
                {
                    "%HOME%/.fonts/",
                    "%HOME%/.local/share/fonts/",
                    "/usr/local/share/fonts/",
                    "/usr/share/fonts/",
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                StandardFontLocations = new[]
                {
                    // As documented on "Mac OS X: Font locations and their purposes"
                    // https://web.archive.org/web/20191015122508/https://support.apple.com/en-us/HT201722
                    "%HOME%/Library/Fonts/",
                    "/Library/Fonts/",
                    "/System/Library/Fonts/",
                    "/Network/Library/Fonts/",
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Android")))
            {
                StandardFontLocations = new[]
                {
                    "/system/fonts/"
                };
            }
            else
            {
                StandardFontLocations = Array.Empty<string>();
            }
        }

        public SystemFontCollection()
        {
            IEnumerable<string> paths;
            Native.MacSystemFontsEnumerator? nativeEnumerator = null;

            bool forceDirectoryEnumeration = AppContext.TryGetSwitch("Switch.SixLabors.Fonts.DoNotUseNativeSystemFontsEnumeration", out bool isEnabled) && isEnabled;
            if (!forceDirectoryEnumeration && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                nativeEnumerator = new Native.MacSystemFontsEnumerator();

                // The CTFontManagerCopyAvailableFontURLs method might return duplicate paths, hence the call to Distinct()
                paths = nativeEnumerator.Distinct();

                this.searchDirectories = Array.Empty<string>();
            }
            else
            {
                string[] expanded = StandardFontLocations.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
                string[] existingDirectories = expanded.Where(x => Directory.Exists(x)).ToArray();

                // We do this to provide a consistent experience with case sensitive file systems.
                paths = existingDirectories
                    .SelectMany(x => Directory.EnumerateFiles(x, "*.*", SearchOption.AllDirectories))
                    .Where(x => Path.GetExtension(x).Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                                || Path.GetExtension(x).Equals(".ttc", StringComparison.OrdinalIgnoreCase)
                                || Path.GetExtension(x).Equals(".otf", StringComparison.OrdinalIgnoreCase));

                this.searchDirectories = existingDirectories;
            }

            this.collection = CreateSystemFontCollection(paths, this.searchDirectories);

            nativeEnumerator?.Dispose();
        }

        /// <inheritdoc/>
        public IEnumerable<FontFamily> Families => this.collection.Families;

        /// <inheritdoc/>
        public IEnumerable<string> SearchDirectories => this.searchDirectories;

        /// <inheritdoc/>
        public FontFamily Get(string name) => this.collection.Get(name);

        /// <inheritdoc/>
        public bool TryGet(string name, out FontFamily family)
            => this.collection.TryGet(name, out family);

        /// <inheritdoc/>
        public IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
            => this.collection.GetByCulture(culture);

        /// <inheritdoc/>
        public FontFamily Get(string name, CultureInfo culture)
            => this.collection.Get(name, culture);

        /// <inheritdoc/>
        public bool TryGet(string name, CultureInfo culture, out FontFamily family)
            => this.collection.TryGet(name, culture, out family);

        /// <inheritdoc/>
        bool IReadOnlyFontMetricsCollection.TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out FontMetrics? metrics)
            => ((IReadOnlyFontMetricsCollection)this.collection).TryGetMetrics(name, culture, style, out metrics);

        /// <inheritdoc/>
        IEnumerable<FontMetrics> IReadOnlyFontMetricsCollection.GetAllMetrics(string name, CultureInfo culture)
            => ((IReadOnlyFontMetricsCollection)this.collection).GetAllMetrics(name, culture);

        /// <inheritdoc/>
        IEnumerable<FontStyle> IReadOnlyFontMetricsCollection.GetAllStyles(string name, CultureInfo culture)
            => ((IReadOnlyFontMetricsCollection)this.collection).GetAllStyles(name, culture);

        /// <inheritdoc/>
        IEnumerator<FontMetrics> IReadOnlyFontMetricsCollection.GetEnumerator()
            => ((IReadOnlyFontMetricsCollection)this.collection).GetEnumerator();

        private static FontCollection CreateSystemFontCollection(IEnumerable<string> paths, IReadOnlyCollection<string> searchDirectories)
        {
            var collection = new FontCollection(searchDirectories);

            foreach (string path in paths)
            {
                try
                {
                    if (path.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    {
                        collection.AddCollection(path);
                    }
                    else
                    {
                        collection.Add(path);
                    }
                }
                catch
                {
                    // We swallow exceptions installing system fonts as we hold no guarantees about permissions etc.
                }
            }

            return collection;
        }
    }
}
