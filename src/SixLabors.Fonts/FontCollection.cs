// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public sealed class FontCollection : IFontCollection
    {
        private readonly HashSet<IFontInstance> instances = new HashSet<IFontInstance>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FontCollection"/> class.
        /// </summary>
        public FontCollection()
        {
        }

        /// <inheritdoc />
        public IEnumerable<FontFamily> Families => this.FamiliesByCultureInternal(CultureInfo.CurrentCulture);

#if SUPPORTS_CULTUREINFO_LCID
        /// <inheritdoc />
        public IEnumerable<FontFamily> FamiliesByCulture(CultureInfo culture) => this.FamiliesByCultureInternal(culture);
#endif

        private IEnumerable<FontFamily> FamiliesByCultureInternal(CultureInfo culture)
                => this.instances
                        .Select(x => x.Description.FontFamily(culture))
                        .Distinct()
                        .Select(x => new FontFamily(x, this, culture))
                        .ToArray();

        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path)
            => this.InstallInternal(path, CultureInfo.CurrentCulture);

        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path, out FontDescription fontDescription)
            => this.InstallInternal(path, CultureInfo.CurrentCulture, out fontDescription);

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream)
            => this.InstallInternal(fontStream, CultureInfo.InvariantCulture);

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream, out FontDescription fontDescription)
            => this.InstallInternal(fontStream, CultureInfo.InvariantCulture, out fontDescription);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath)
            => this.InstallCollectionInternal(fontCollectionPath, CultureInfo.InvariantCulture);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath, out IEnumerable<FontDescription> fontDescriptions)
            => this.InstallCollectionInternal(fontCollectionPath, CultureInfo.InvariantCulture, out fontDescriptions);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionStream">The font stream.</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(Stream fontCollectionStream, out IEnumerable<FontDescription> fontDescriptions)
            => this.InstallCollectionInternal(fontCollectionStream, CultureInfo.InvariantCulture, out fontDescriptions);

#if SUPPORTS_CULTUREINFO_LCID
        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path, CultureInfo culture)
            => this.InstallInternal(path, culture);

        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path, CultureInfo culture, out FontDescription fontDescription)
            => this.InstallInternal(path, culture, out fontDescription);

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream, CultureInfo culture)
            => this.InstallInternal(fontStream, culture);

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <param name="culture">The culture of the retuend font family</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream, CultureInfo culture, out FontDescription fontDescription)
            => this.InstallInternal(fontStream, culture, out fontDescription);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        /// <param name="culture">The culture of the retuend font families</param>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath, CultureInfo culture)
            => this.InstallCollectionInternal(fontCollectionPath, culture);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <param name="culture">The culture of the retuend font families</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath, CultureInfo culture, out IEnumerable<FontDescription> fontDescriptions)
            => this.InstallCollectionInternal(fontCollectionPath, culture, out fontDescriptions);

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionStream">The font stream.</param>
        /// <param name="culture">The culture of the retuend font families</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(Stream fontCollectionStream, CultureInfo culture, out IEnumerable<FontDescription> fontDescriptions)
            => this.InstallCollectionInternal(fontCollectionStream, culture, out fontDescriptions);
#endif

        private FontFamily InstallInternal(string path, CultureInfo culture)
        {
            return this.InstallInternal(path, culture, out _);
        }

        private FontFamily InstallInternal(string path, CultureInfo culture, out FontDescription fontDescription)
        {
            var instance = new FileFontInstance(path);
            fontDescription = instance.Description;
            return this.Install(instance, culture);
        }

        private FontFamily InstallInternal(Stream fontStream, CultureInfo culture)
        {
            return this.InstallInternal(fontStream, culture, out _);
        }

        private FontFamily InstallInternal(Stream fontStream, CultureInfo culture, out FontDescription fontDescription)
        {
            var instance = FontInstance.LoadFont(fontStream);
            fontDescription = instance.Description;

            return this.Install(instance, culture);
        }

        private IEnumerable<FontFamily> InstallCollectionInternal(string fontCollectionPath, CultureInfo culture)
        {
            return this.InstallCollectionInternal(fontCollectionPath, culture, out _);
        }

        private IEnumerable<FontFamily> InstallCollectionInternal(string fontCollectionPath, CultureInfo culture, out IEnumerable<FontDescription> fontDescriptions)
        {
            FileFontInstance[] fonts = FileFontInstance.LoadFontCollection(fontCollectionPath);

            var description = new FontDescription[fonts.Length];
            var families = new HashSet<FontFamily>();
            for (int i = 0; i < fonts.Length; i++)
            {
                description[i] = fonts[i].Description;
                FontFamily family = this.Install(fonts[i], culture);
                families.Add(family);
            }

            fontDescriptions = description;
            return families;
        }

        private IEnumerable<FontFamily> InstallCollectionInternal(Stream fontCollectionStream, CultureInfo culture, out IEnumerable<FontDescription> fontDescriptions)
        {
            long startPos = fontCollectionStream.Position;
            var reader = new BinaryReader(fontCollectionStream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var result = new List<FontDescription>((int)ttcHeader.NumFonts);
            var installedFamilies = new HashSet<FontFamily>();
            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                fontCollectionStream.Position = startPos + ttcHeader.OffsetTable[i];
                var instance = FontInstance.LoadFont(fontCollectionStream);
                installedFamilies.Add(this.Install(instance, culture));
                FontDescription fontDescription = instance.Description;
                result.Add(fontDescription);
            }

            fontDescriptions = result;
            return installedFamilies;
        }

#if SUPPORTS_CULTUREINFO_LCID
        /// <inheritdoc />
        public FontFamily Find(string fontFamily, CultureInfo culture)
            => this.FindInternal(fontFamily, culture);

        /// <inheritdoc />
        public bool TryFind(string fontFamily, CultureInfo culture, [NotNullWhen(true)]out FontFamily? family)
            => this.TryFindInternal(fontFamily, culture, out family);
#endif

        private FontFamily FindInternal(string fontFamily, CultureInfo culture)
        {
            if (this.TryFindInternal(fontFamily, culture, out var result))
            {
                return result;
            }

            throw new FontFamilyNotFoundException(fontFamily);
        }

        private bool TryFindInternal(string fontFamily, CultureInfo culture, [NotNullWhen(true)]out FontFamily? family)
        {
            var comparer = StringComparerHelpers.GetCaseInsenativeStringComparer(culture);
            family = null!; // make the compiler shutup

            var familyName = this.instances
                .Select(x => x.Description.FontFamily(culture))
                .FirstOrDefault(x => comparer.Equals(x, fontFamily));
            if (familyName == null)
            {
                return false;
            }

            family = new FontFamily(familyName, this, culture);
            return true;
        }

        /// <inheritdoc />
        public FontFamily Find(string fontFamily)
            => this.FindInternal(fontFamily, CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public bool TryFind(string fontFamily, [NotNullWhen(true)]out FontFamily? family)
            => this.TryFindInternal(fontFamily, CultureInfo.InvariantCulture, out family);

        internal IEnumerable<FontStyle> AvailableStyles(string fontFamily, CultureInfo culture)
        {
            return this.FindAll(fontFamily, culture).Select(x => x.Description.Style).ToArray();
        }

        internal FontFamily Install(IFontInstance instance, CultureInfo culture)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.Description == null)
            {
                throw new ArgumentException("IFontInstance must have a Description.", nameof(instance));
            }

            lock (this.instances)
            {
                this.instances.Add(instance);
            }

            return new FontFamily(instance.Description.FontFamily(culture), this, culture);
        }

        internal IFontInstance? Find(string fontFamily, CultureInfo culture, FontStyle style)
        {
            return this.FindAll(fontFamily, culture)
                        .FirstOrDefault(x => x.Description.Style == style);
        }

        internal IEnumerable<IFontInstance> FindAll(string name, CultureInfo culture)
        {
            var comparer = StringComparerHelpers.GetCaseInsenativeStringComparer(culture);

            var instances = this.instances
                                .Where(x => comparer.Equals(x.Description.FontFamily(culture), name))
                                .ToArray();

            return instances;
        }
    }
}
