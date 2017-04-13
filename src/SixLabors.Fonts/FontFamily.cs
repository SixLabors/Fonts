using System;
using System.IO;
using System.Numerics;

using SixLabors.Fonts.Tables;
using SixLabors.Fonts.Tables.General;
using System.Linq;
using System.Collections.Generic;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a group of type faces having a similar basic design and certain variations in styles. This class cannot be inherited.
    /// </summary>
    public class FontFamily
    {
        private FontCollection collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamily"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collection">The collection.</param>
        internal FontFamily(string name, FontCollection collection)
        {
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the <see cref="FontFamily"/>.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the availible <see cref="FontStyle"/> that are currently availible.
        /// </summary>
        /// <value>
        /// The availible styles.
        /// </value>
        public IEnumerable<FontStyle> AvailibleStyles => collection.AvailibleStyles(this.Name);

        /// <summary>
        /// Determines whether the specified <see cref="FontStyle"/> is availible.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="FontStyle"/> is availible; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStyleAvailible(FontStyle style) => this.AvailibleStyles.Contains(style);

        internal FontStyle DefaultStyle => IsStyleAvailible(FontStyle.Regular) ? FontStyle.Regular : AvailibleStyles.First();

        internal IFontInstance Find(FontStyle style)
        {
            return collection.Find(this.Name, style);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
