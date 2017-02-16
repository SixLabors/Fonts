﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Provides a collection of fonts.
    /// </summary>
    public sealed class FontCollection
    {
#if FILESYSTEM
        private static Lazy<FontCollection> lazySystemFonts = new Lazy<FontCollection>(CreateSystemFontsCollection);
        public static FontCollection SystemFonts => lazySystemFonts.Value;

        private static FontCollection CreateSystemFontsCollection()
        {
            var collection = new FontCollection();

            string[] paths = new[] {
                // windows directories
                "%SYSTEMROOT%\\Fonts",
            };

            var expanded = paths.Select(x => Environment.ExpandEnvironmentVariables(x)).ToArray();
            var found = expanded.Where(x => Directory.Exists(x)).ToArray();

            var fonts = found.SelectMany(x => Directory.EnumerateFiles(x, "*.ttf")).Select(x => new FileFontInstance(x)).ToArray();
            foreach (var f in fonts)
            {
                collection.Install(f);
            }

            return collection;
        }
#endif

        Dictionary<string, List<IFontInstance>> instances = new Dictionary<string, List<IFontInstance>>(StringComparer.OrdinalIgnoreCase);
        private List<FontFamily> families = new List<FontFamily>();

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
        public IEnumerable<FontFamily> Families => families.ToImmutableArray();

#if FILESYSTEM
        /// <summary>
        /// Installs a font from the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>the description of the font just loaded.</returns>
        public FontDescription Install(string path)
        {
            using (var fs = File.OpenRead(path))
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
        public FontDescription Install(Stream fontStream)
        {
            var instance = FontInstance.LoadFont(fontStream);

            return Install(instance);
        }

        /// <summary>
        /// Finds the specified font family.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <returns></returns>
        public FontFamily Find(string fontFamily)
        {
            return this.families.FirstOrDefault(x => x.Name.Equals(fontFamily, StringComparison.OrdinalIgnoreCase));
        }

        internal IEnumerable<FontStyle> AvailibleStyles(string fontFamily)
        {
            return FindAll(fontFamily).Select(X => X.Description.Style).ToImmutableArray();
        }

        internal FontDescription Install(IFontInstance instance)
        {
            if (instance != null)
            {
                lock (instances)
                {
                    if (!instances.ContainsKey(instance.Description.FontFamily))
                    {
                        instances.Add(instance.Description.FontFamily, new List<IFontInstance>(4));
                        families.Add(new FontFamily(instance.Description, this));
                    }
                    instances[instance.Description.FontFamily].Add(instance);
                }

                return instance.Description;
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
            var inFamily = this.instances[fontFamily];

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