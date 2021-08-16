// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    internal sealed class SystemFontCollection : IReadOnlyFontCollection, IReadOnlyFontMetricsCollection
    {
        private readonly FontCollection collection = new FontCollection();

        /// <summary>
        /// Gets the default set of locations we probe for System Fonts.
        /// </summary>
        private static readonly IReadOnlyCollection<string> StandardFontLocations
            = new[]
            {
                // windows directories
                "%SYSTEMROOT%\\Fonts",
                "%APPDATA%\\Microsoft\\Windows\\Fonts",
                "%LOCALAPPDATA%\\Microsoft\\Windows\\Fonts",

                // linux directories
                "~/.fonts/",
                "/usr/local/share/fonts/",
                "/usr/share/fonts/",

                // mac directories
                "~/Library/Fonts/",
                "/Library/Fonts/",
                "/Network/Library/Fonts/",
                "/System/Library/Fonts/",
                "/System Folder/Fonts/",
            };

        public SystemFontCollection()
            : this(StandardFontLocations)
        {
        }

        public SystemFontCollection(IEnumerable<string> paths)
        {
            string[] expanded = paths.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
            string[] foundDirectories = expanded.Where(x => Directory.Exists(x)).ToArray();

            // We do this to provide a consistent experience with case sensitive file systems.
            IEnumerable<string> files = foundDirectories
                                .SelectMany(x => Directory.EnumerateFiles(x, "*.*", SearchOption.AllDirectories))
                                .Where(x => Path.GetExtension(x).Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                                || Path.GetExtension(x).Equals(".ttc", StringComparison.OrdinalIgnoreCase));

            foreach (string path in files)
            {
                try
                {
                    if (path.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    {
                        this.collection.AddCollection(path);
                    }
                    else
                    {
                        this.collection.Add(path);
                    }
                }
                catch
                {
                    // We swallow exceptions installing system fonts as we hold no guarantees about permissions etc.
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<FontFamily> Families => this.collection.Families;

        /// <inheritdoc/>
        public FontFamily Get(string name) => this.collection.Get(name);

        /// <inheritdoc/>
        public bool TryGet(string name, out FontFamily family)
            => this.collection.TryGet(name, out family);

#if SUPPORTS_CULTUREINFO_LCID
        /// <inheritdoc/>
        public IEnumerable<FontFamily> GetByCulture(CultureInfo culture)
            => this.collection.GetByCulture(culture);

        /// <inheritdoc/>
        public FontFamily Get(string name, CultureInfo culture)
            => this.collection.Get(name, culture);

        /// <inheritdoc/>
        public bool TryGet(string name, CultureInfo culture, out FontFamily family)
            => this.collection.TryGet(name, culture, out family);
#endif

        /// <inheritdoc/>
        bool IReadOnlyFontMetricsCollection.TryGetMetrics(string name, CultureInfo culture, FontStyle style, [NotNullWhen(true)] out IFontMetrics? metrics)
            => ((IReadOnlyFontMetricsCollection)this.collection).TryGetMetrics(name, culture, style, out metrics);

        /// <inheritdoc/>
        IEnumerable<IFontMetrics> IReadOnlyFontMetricsCollection.GetAllMetrics(string name, CultureInfo culture)
            => ((IReadOnlyFontMetricsCollection)this.collection).GetAllMetrics(name, culture);

        /// <inheritdoc/>
        IEnumerable<FontStyle> IReadOnlyFontMetricsCollection.GetAllStyles(string name, CultureInfo culture)
            => ((IReadOnlyFontMetricsCollection)this.collection).GetAllStyles(name, culture);

        /// <inheritdoc/>
        IEnumerator<IFontMetrics> IReadOnlyFontMetricsCollection.GetEnumerator()
            => ((IReadOnlyFontMetricsCollection)this.collection).GetEnumerator();
    }
}
