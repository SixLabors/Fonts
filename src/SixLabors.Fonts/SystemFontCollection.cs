// Copyright (c) Six Labors and contributors.
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
    internal sealed class SystemFontCollection : IReadOnlyFontCollection
    {
        private readonly FontCollection collection = new FontCollection();

        /// <summary>
        /// Gets the default set of locations we probe for System Fonts.
        /// </summary>
        private static IReadOnlyCollection<string> standardFontLocations = new[]
            {
                // windows directories
                "%SYSTEMROOT%\\Fonts",

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
            : this(standardFontLocations)
        {
        }

        public SystemFontCollection(IEnumerable<string> probPaths)
        {
            string[] expanded = probPaths.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
            string[] foundDirectories = expanded.Where(x => Directory.Exists(x)).ToArray();

            // we do this to provide a consistent experience with case sensitive file systems.
            IEnumerable<string> files = foundDirectories
                                .SelectMany(x => Directory.EnumerateFiles(x, "*.*", SearchOption.AllDirectories))
                                .Where(x => Path.GetExtension(x).Equals(".ttf", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(x).Equals(".ttc", StringComparison.OrdinalIgnoreCase));

            foreach (string path in files)
            {
                try
                {
                    if (path.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    {
                        this.collection.InstallCollection(path);
                    }
                    else
                    {
                        this.collection.Install(path);
                    }
                }
                catch
                {
                    // we swollow exceptions installing system fonts as we hold no garantees about permissions etc.
                }
            }
        }

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public IEnumerable<FontFamily> Families => this.collection.Families;

        /// <inheritdocs />
        public FontFamily Find(string fontFamily) => this.collection.Find(fontFamily);

        /// <inheritdocs />
        public bool TryFind(string fontFamily, [NotNullWhen(true)] out FontFamily? family) => this.collection.TryFind(fontFamily, out family);

#if SUPPORTS_CULTUREINFO_LCID
        public IEnumerable<FontFamily> FamiliesByCulture(CultureInfo culture)
            => this.collection.FamiliesByCulture(culture);

        public FontFamily Find(string fontFamily, CultureInfo culture)
            => this.collection.Find(fontFamily, culture);

        public bool TryFind(string fontFamily, CultureInfo culture, [NotNullWhen(true)] out FontFamily? family)
            => this.collection.TryFind(fontFamily, culture, out family);
#endif
    }
}
