// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SixLabors.Fonts
{
    /// <inheritdoc/>
    public struct FontFamily2 : IFontFamily, IEquatable<FontFamily2>
    {
        private readonly FontCollection2 collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamily2"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="culture">The culture the family was extracted against</param>
        internal FontFamily2(string name, FontCollection2 collection, CultureInfo culture)
        {
            Guard.NotNull(collection, nameof(collection));

            this.collection = collection;
            this.Name = name;
            this.Culture = culture;
        }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Compares two <see cref="FontFamily2"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily2"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily2"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is equal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(FontFamily2 left, FontFamily2 right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="FontFamily2"/> objects for inequality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily2"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily2"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is unequal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(FontFamily2 left, FontFamily2 right)
            => !(left == right);

        /// <inheritdoc/>
        public IEnumerable<FontStyle> AvailableStyles()
            => this.collection.AvailableStyles(this.Name, this.Culture);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is FontFamily2 family && this.Equals(family);

        /// <inheritdoc/>
        public bool Equals(FontFamily2 other)
        {
            StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(this.Culture);
            return comparer.Equals(this.Name, other.Name)
                && EqualityComparer<CultureInfo>.Default.Equals(this.Culture, other.Culture)
                && EqualityComparer<FontCollection2>.Default.Equals(this.collection, other.collection);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(this.collection, this.Name, this.Culture);

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }
}
