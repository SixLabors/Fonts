// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General.Name;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public sealed class FontCollection : IFontCollection
    {
        private readonly Dictionary<string, List<IFontInstance>> instances = new Dictionary<string, List<IFontInstance>>(StringComparer.CurrentCultureIgnoreCase);
        private readonly Dictionary<string, FontFamily> families = new Dictionary<string, FontFamily>(StringComparer.CurrentCultureIgnoreCase);
#if SUPPORTS_CULTUREINFO_LCID
        // If LCID is supported, we will also be able to provide font families in all languages
        // And we can create StringComparer for any valid LCID.
        private readonly Dictionary<int, Dictionary<string, FontFamily>> familiesByLcid = new Dictionary<int, Dictionary<string, FontFamily>>();
#endif
        private readonly CultureInfo culture;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontCollection"/> class.
        /// </summary>
        /// <param name="culture">The culture to use for font metadata.</param>
        public FontCollection(CultureInfo culture)
        {
            this.culture = culture;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontCollection"/> class.
        /// </summary>
        public FontCollection()
            : this(CultureInfo.InvariantCulture)
        {
        }

        /// <summary>
        /// Gets the collection of <see cref="FontFamily"/> objects associated with this <see cref="FontCollection"/>.
        /// </summary>
        /// <value>
        /// The families.
        /// </value>
        public IEnumerable<FontFamily> Families => this.families.Values;

        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path)
        {
            return this.Install(path, out _);
        }

        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path, out FontDescription fontDescription)
        {
            var instance = new FileFontInstance(path, this.culture);
            fontDescription = instance.Description;
            return this.Install(instance);
        }

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream)
        {
            return this.Install(fontStream, out _);
        }

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <param name="fontDescription">The font description of the installed font.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream, out FontDescription fontDescription)
        {
            var instance = FontInstance.LoadFont(fontStream, this.culture);
            fontDescription = instance.Description;

            return this.Install(instance);
        }

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath)
        {
            return this.InstallCollection(fontCollectionPath, out _);
        }

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionPath">The font collection path (should be typically a .ttc file like simsun.ttc).</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(string fontCollectionPath, out IEnumerable<FontDescription> fontDescriptions)
        {
            FileFontInstance[] fonts = FileFontInstance.LoadFontCollection(fontCollectionPath, this.culture);

            var description = new FontDescription[fonts.Length];
            var families = new HashSet<FontFamily>();
            for (int i = 0; i < fonts.Length; i++)
            {
                description[i] = fonts[i].Description;
                FontFamily family = this.Install(fonts[i]);
                families.Add(family);
            }

            fontDescriptions = description;
            return families;
        }

        /// <summary>
        /// Installs a true type font collection (.ttc) from the specified font collection stream.
        /// </summary>
        /// <param name="fontCollectionStream">The font stream.</param>
        /// <param name="fontDescriptions">The descriptions of fonts installed from the collection.</param>
        /// <returns>The font descriptions of the installed fonts.</returns>
        public IEnumerable<FontFamily> InstallCollection(Stream fontCollectionStream, out IEnumerable<FontDescription> fontDescriptions)
        {
            long startPos = fontCollectionStream.Position;
            var reader = new BinaryReader(fontCollectionStream, true);
            var ttcHeader = TtcHeader.Read(reader);
            var result = new List<FontDescription>((int)ttcHeader.NumFonts);
            var installedFamilies = new HashSet<FontFamily>();
            for (int i = 0; i < ttcHeader.NumFonts; ++i)
            {
                fontCollectionStream.Position = startPos + ttcHeader.OffsetTable[i];
                var instance = FontInstance.LoadFont(fontCollectionStream, this.culture);
                installedFamilies.Add(this.Install(instance));
                FontDescription fontDescription = instance.Description;
                result.Add(fontDescription);
            }

            fontDescriptions = result;
            return installedFamilies;
        }

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        public FontFamily Find(string fontFamily)
        {
            if (this.TryFind(fontFamily, out FontFamily result))
            {
                return result;
            }

            throw new FontFamilyNotFoundException(fontFamily);
        }

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        public bool TryFind(string fontFamily, out FontFamily family)
        {
            return this.families.TryGetValue(fontFamily, out family);
        }

        /// <summary>
        /// Finds the specified font family, also by looking into locale specific names.<br/>
        /// <b>Note</b>: On targets where <see cref="CultureInfo"/>.LCID is not supported,
        /// such as .NET Standard 1.0,  it's same as <see cref="Find(string)"/>
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="preferredCulture">Preferred culture, can be null.</param>
        /// <returns>The family if installed otherwise throws <see cref="FontFamilyNotFoundException"/></returns>
        public FontFamily Find(string fontFamily, CultureInfo? preferredCulture)
        {
            if (this.TryFind(fontFamily, preferredCulture, out FontFamily result))
            {
                return result;
            }

            throw new FontFamilyNotFoundException(fontFamily);
        }

        /// <summary>
        /// Finds the specified font family, also by looking into locale specific names.
        /// <b>Note</b>: On targets where <see cref="CultureInfo"/>.LCID is not supported,
        /// such as .NET Standard 1.0,  it's same as <see cref="TryFind(string, out FontFamily)"/>
        /// </summary>
        /// <param name="fontFamily">The font family to find.</param>
        /// <param name="preferredCulture">Preferred culture, can be null.</param>
        /// <param name="family">The found family.</param>
        /// <returns>true if a font of that family has been installed into the font collection.</returns>
        public bool TryFind(string fontFamily, CultureInfo? preferredCulture, out FontFamily family)
        {
#if SUPPORTS_CULTUREINFO_LCID
            IEnumerable<Dictionary<string, FontFamily>> collections = this.familiesByLcid.Values;
            if (preferredCulture != null)
            {
                if (this.familiesByLcid.TryGetValue(preferredCulture.LCID, out Dictionary<string, FontFamily> families))
                {
                    collections = Enumerable.Repeat(families, 1).Concat(collections);
                }
            }

            foreach (Dictionary<string, FontFamily> families in collections)
            {
                if (families.TryGetValue(fontFamily, out family))
                {
                    return true;
                }
            }
#endif
            return this.TryFind(fontFamily, out family);
        }

        internal IEnumerable<FontStyle> AvailableStyles(string fontFamily)
        {
            return this.FindAll(fontFamily).Select(x => x.Description.Style).ToArray();
        }

        internal FontFamily Install(IFontInstance instance)
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
                if (!this.instances.ContainsKey(instance.Description.FontFamily))
                {
                    this.instances.Add(instance.Description.FontFamily, new List<IFontInstance>(4));
                }

                if (!this.families.TryGetValue(instance.Description.FontFamily, out FontFamily fontFamily))
                {
                    fontFamily = new FontFamily(instance.Description.FontFamily, this);
                    this.families.Add(fontFamily.Name, fontFamily);
                }

#if SUPPORTS_CULTUREINFO_LCID
                // If LCID is supported, we will also be able to provide font families in all languages
                foreach (NameRecord record in instance.Description.FontFamilyNames)
                {
                    // Create the families' Dictionary of current language if it does not exist
                    if (!this.familiesByLcid.TryGetValue(record.LanguageID, out Dictionary<string, FontFamily> familiesOfCurrentLanguage))
                    {
                        var currentCulture = CultureInfo.GetCultureInfo(record.LanguageID);
                        familiesOfCurrentLanguage = new Dictionary<string, FontFamily>(StringComparer.Create(currentCulture, true));
                        this.familiesByLcid.Add(record.LanguageID, familiesOfCurrentLanguage);
                    }

                    familiesOfCurrentLanguage.Add(record.Value, fontFamily);
                }
#endif

                this.instances[instance.Description.FontFamily].Add(instance);
            }

            return this.families[instance.Description.FontFamily];
        }

        internal IFontInstance? Find(string fontFamily, FontStyle style)
        {
            return this.instances.TryGetValue(fontFamily, out List<IFontInstance> inFamily)
                ? inFamily.FirstOrDefault(x => x.Description.Style == style)
                : null;
        }

        internal IEnumerable<IFontInstance> FindAll(string name)
        {
            // once we have to support verient fonts then we
            return this.instances.TryGetValue(name, out List<IFontInstance> value)
                ? value
                : Enumerable.Empty<IFontInstance>();
        }
    }
}
