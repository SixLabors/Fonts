// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public sealed class FontCollection : IFontCollection
    {
        private readonly Dictionary<string, List<IFontInstance>> instances = new Dictionary<string, List<IFontInstance>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FontFamily> families = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="FontCollection"/> class.
        /// </summary>
        public FontCollection()
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
            var instance = new FileFontInstance(path);
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
            FontInstance instance = FontInstance.LoadFont(fontStream);
            fontDescription = instance.Description;

            return this.Install(instance);
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

                if (!this.families.ContainsKey(instance.Description.FontFamily))
                {
                    this.families.Add(instance.Description.FontFamily, new FontFamily(instance.Description.FontFamily, this));
                }

                this.instances[instance.Description.FontFamily].Add(instance);
            }

            return this.families[instance.Description.FontFamily];
        }

        internal IFontInstance Find(string fontFamily, FontStyle style)
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
