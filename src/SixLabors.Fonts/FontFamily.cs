// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a group of type faces having a similar basic design and certain variations in styles. This class cannot be inherited.
    /// </summary>
    public sealed class FontFamily : IEquatable<FontFamily?>
    {
        private readonly FontCollection collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamily"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="culture">The culture the family was extracted against</param>
        internal FontFamily(string name, FontCollection collection, CultureInfo culture)
        {
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.Name = name;
            this.Culture = culture;
        }

        /// <summary>
        /// Gets the name of the <see cref="FontFamily"/>.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the culture this <see cref="FontFamily"/> was created against.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Gets the available <see cref="FontStyle"/> that are currently available.
        /// </summary>
        /// <value>
        /// The available styles.
        /// </value>
        public IEnumerable<FontStyle> AvailableStyles
            => this.collection.AvailableStyles(this.Name, this.Culture);

        internal FontStyle DefaultStyle
            => this.IsStyleAvailable(FontStyle.Regular)
            ? FontStyle.Regular
            : this.AvailableStyles.First();

        /// <summary>
        /// Compares two <see cref="FontFamily"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is equal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(FontFamily? left, FontFamily? right)
            => EqualityComparer<FontFamily?>.Default.Equals(left, right);

        /// <summary>
        /// Compares two <see cref="FontFamily"/> objects for inequality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is unequal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(FontFamily? left, FontFamily? right)
            => !(left == right);

        internal IFontInstance? Find(FontStyle style)
            => this.collection.Find(this.Name, this.Culture, style);

        /// <summary>
        /// Determines whether the specified <see cref="FontStyle"/> is available.
        /// </summary>
        /// <param name="style">The style.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="FontStyle"/> is available; otherwise, <c>false</c>.
        /// </returns>
        public bool IsStyleAvailable(FontStyle style) => this.AvailableStyles.Contains(style);

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => this.Name;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            StringComparer? comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(this.Culture);
            return HashCode.Combine(this.collection, this.Culture, this.DefaultStyle, this.AvailableStyles) ^ comparer.GetHashCode(this.Name);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => this.Equals(obj as FontFamily);

        /// <inheritdoc/>
        public bool Equals(FontFamily? other)
        {
            if (other is null)
            {
                return false;
            }

            StringComparer? comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(this.Culture);
            return this.collection == other.collection
                && this.Culture == other.Culture
                && this.DefaultStyle == other.DefaultStyle
                && this.AvailableStyles.SequenceEqual(other.AvailableStyles)
                && comparer.Equals(this.Name, other.Name);
        }
    }
}
