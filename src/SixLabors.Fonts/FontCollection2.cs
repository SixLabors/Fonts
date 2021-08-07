// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Represents a collection of font families.
    /// </summary>
    public class FontCollection2 : IFontCollection2
    {
        private readonly HashSet<FileFontMetrics> fileFontMetrics = new HashSet<FileFontMetrics>();
        private readonly HashSet<FontMetrics> fontMetrics = new HashSet<FontMetrics>();

        /// <inheritdoc/>
        public IEnumerable<IFontFamily> Families => this.FamiliesByCultureImpl(CultureInfo.InvariantCulture);

        /// <inheritdoc/>
        public FileFontFamily Add(string path) => throw new NotImplementedException();

        /// <inheritdoc/>
        public FontFamily2 Add(Stream fontStream) => throw new NotImplementedException();

        /// <inheritdoc/>
        public IFontFamily Get(string name)
            => this.GetImpl(name, CultureInfo.InvariantCulture);

        /// <inheritdoc/>
        public bool TryGet(string name, [NotNullWhen(true)] out IFontFamily? family)
            => this.TryGetImpl(name, CultureInfo.InvariantCulture, out family);

        /// <inheritdoc/>
        public IEnumerable<IFontFamily> FamiliesByCulture(CultureInfo culture)
            => this.FamiliesByCultureImpl(culture);

#if SUPPORTS_CULTUREINFO_LCID
        /// <inheritdoc/>
        public IFontFamily Get(string name, CultureInfo culture)
            => this.GetImpl(name, culture);

        /// <inheritdoc/>
        public bool TryGet(string name, CultureInfo culture, [NotNullWhen(true)] out IFontFamily? family)
            => this.TryGetImpl(name, culture, out family);
#endif

        private IFontFamily GetImpl(string name, CultureInfo culture)
        {
            if (this.TryGetImpl(name, culture, out IFontFamily? family))
            {
                return family;
            }

            throw new FontFamilyNotFoundException(name);
        }

        private bool TryGetImpl(string name, CultureInfo culture, [NotNullWhen(true)] out IFontFamily? family)
        {
            Guard.NotNull(name, nameof(name));

            StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);

            var a = this.fileFontMetrics
                .Select(x => new { Name = x.Description.FontFamily(culture), x.Path })
                .FirstOrDefault(x => comparer.Equals(name, x.Name));

            if (a != null)
            {
                family = new FileFontFamily(a.Name, a.Path, this, culture);
                return true;
            }

            string? b = this.fontMetrics
                .Select(x => x.Description.FontFamily(culture))
                .FirstOrDefault(x => comparer.Equals(name, x));

            if (b != null)
            {
                family = new FontFamily2(b, this, culture);
                return true;
            }

            family = null;
            return false;
        }

        private IEnumerable<IFontFamily> FamiliesByCultureImpl(CultureInfo culture)
        {
            var families = new List<IFontFamily>();

            IEnumerable<IFontFamily> a = this.fileFontMetrics
                        .Select(x => new { Name = x.Description.FontFamily(culture), x.Path })
                        .Distinct()
                        .Select(x => new FileFontFamily(x.Name, x.Path, this, culture) as IFontFamily);

            families.AddRange(a);

            IEnumerable<IFontFamily> b = this.fontMetrics
                        .Select(x => x.Description.FontFamily(culture))
                        .Distinct()
                        .Select(x => new FontFamily2(x, this, culture) as IFontFamily);

            families.AddRange(b);

            StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(culture);
            families.Sort((x, y) => comparer.Compare(x.Name, y.Name));
            return families;
        }
    }
}
