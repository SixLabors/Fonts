// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a group of type faces having a similar basic design and certain variations in styles. This class cannot be inherited.
    /// </summary>
    public sealed class FontFamily
    {
        private readonly FontCollection collection;

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
        /// Gets the available <see cref="FontStyle"/> that are currently available.
        /// </summary>
        /// <value>
        /// The available styles.
        /// </value>
        public IEnumerable<FontStyle> AvailableStyles => this.collection.AvailableStyles(this.Name);

        /// <summary>
        /// Determines whether the specified <see cref="FontStyle"/> is available.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="FontStyle"/> is available; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStyleAvailable(FontStyle style) => this.AvailableStyles.Contains(style);

        internal FontStyle DefaultStyle => this.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : this.AvailableStyles.First();

        internal IFontInstance Find(FontStyle style)
        {
            return this.collection.Find(this.Name, style);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => this.Name;
    }
}
