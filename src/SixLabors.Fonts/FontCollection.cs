// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private Dictionary<string, List<IFontInstance>> instances = new Dictionary<string, List<IFontInstance>>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, FontFamily> families = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase);

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
        public IEnumerable<FontFamily> Families => this.families.Values.ToImmutableArray();

#if FILESYSTEM
        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return this.Install(fs);
            }
        }
#endif

        /// <summary>
        /// Installs the specified font stream.
        /// </summary>
        /// <param name="fontStream">The font stream.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontFamily Install(Stream fontStream)
        {
            FontInstance instance = FontInstance.LoadFont(fontStream);

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
            if (this.families.ContainsKey(fontFamily))
            {
                family = this.families[fontFamily];
                return true;
            }

            family = null;
            return false;
        }

        internal IEnumerable<FontStyle> AvailibleStyles(string fontFamily)
        {
            return this.FindAll(fontFamily).Select(x => x.Description.Style).ToImmutableArray();
        }

        internal FontFamily Install(IFontInstance instance)
        {
            if (instance != null && instance.Description != null)
            {
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

            return null;
        }

        internal IFontInstance Find(string fontFamily, FontStyle style)
        {
            if (!this.instances.ContainsKey(fontFamily))
            {
                return null;
            }

            // once we have to support verient fonts then we
            List<IFontInstance> inFamily = this.instances[fontFamily];

            return inFamily.FirstOrDefault(x => x.Description.Style == style);
        }

        internal IEnumerable<IFontInstance> FindAll(string name)
        {
            if (!this.instances.ContainsKey(name))
            {
                return Enumerable.Empty<IFontInstance>();
            }

            // once we have to support verient fonts then we
            return this.instances[name];
        }
    }
}
