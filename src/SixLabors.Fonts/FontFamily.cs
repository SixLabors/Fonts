// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SixLabors.Fonts.Exceptions;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Defines a group of type faces having a similar basic design and certain
    /// variations in styles.
    /// </summary>
    public struct FontFamily : IEquatable<FontFamily>
    {
        private readonly IReadOnlyFontMetricsCollection collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamily"/> struct.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="culture">The culture the family was extracted against</param>
        internal FontFamily(string name, IReadOnlyFontMetricsCollection collection, CultureInfo culture)
        {
            Guard.NotNull(collection, nameof(collection));

            this.collection = collection;
            this.Name = name;
            this.Culture = culture;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the culture this instance was extracted against.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Compares two <see cref="FontFamily"/> objects for equality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is equal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator ==(FontFamily left, FontFamily right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="FontFamily"/> objects for inequality.
        /// </summary>
        /// <param name="left">The <see cref="FontFamily"/> on the left side of the operand.</param>
        /// <param name="right">The <see cref="FontFamily"/> on the right side of the operand.</param>
        /// <returns>
        /// <see langword="true"/> if the current left is unequal to the <paramref name="right"/>
        /// parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool operator !=(FontFamily left, FontFamily right)
            => !(left == right);

        /// <summary>
        /// Create a new instance of the <see cref="Font" /> for the named font family with regular styling.
        /// </summary>
        /// <param name="size">The size of the font in PT units.</param>
        /// <returns>The new <see cref="Font" />.</returns>
        public Font CreateFont(float size)
        {
            if (this == default)
            {
                FontsThrowHelper.ThrowDefaultInstance();
            }

            return new Font(this, size);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Font" /> for the named font family.
        /// </summary>
        /// <param name="size">The size of the font in PT units.</param>
        /// <param name="style">The font style.</param>
        /// <returns>The new <see cref="Font" />.</returns>
        public Font CreateFont(float size, FontStyle style)
        {
            if (this == default)
            {
                FontsThrowHelper.ThrowDefaultInstance();
            }

            return new Font(this, size, style);
        }

        /// <summary>
        /// Gets the collection of <see cref="FontStyle" /> that are currently available.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{T}" />.</returns>
        public IEnumerable<FontStyle> AvailableStyles()
        {
            if (this == default)
            {
                FontsThrowHelper.ThrowDefaultInstance();
            }

            return this.collection.GetAllStyles(this.Name, this.Culture);
        }

        /// <summary>
        /// Gets the filesystem path to the font family source.
        /// </summary>
        /// <param name="path">
        /// When this method returns, contains the filesystem path to the font family source,
        /// if the path exists; otherwise, the default value for the type of the path parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if the <see cref="FontFamily" /> was created via a filesystem path; otherwise, <see langword="false" />.
        /// </returns>
        public bool TryGetPath([NotNullWhen(true)] out string? path)
        {
            if (this == default)
            {
                FontsThrowHelper.ThrowDefaultInstance();
            }

            if (this.collection.TryGetMetrics(this.Name, this.Culture, out IFontMetrics? metrics)
                && metrics is FileFontMetrics fileMetrics)
            {
                path = fileMetrics.Path;
                return true;
            }

            path = null;
            return false;
        }

        /// <summary>
        /// Gets the specified font metrics matching the given font style.
        /// </summary>
        /// <param name="style">The font style to use when searching for a match.</param>
        /// <param name="metrics">
        /// When this method returns, contains the metrics associated with the specified name,
        /// if the name is found; otherwise, the default value for the type of the metrics parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="FontFamily"/> contains font metrics
        /// with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        internal bool TryGetMetrics(FontStyle style, [NotNullWhen(true)] out IFontMetrics? metrics)
        {
            if (this == default)
            {
                FontsThrowHelper.ThrowDefaultInstance();
            }

            return this.collection.TryGetMetrics(this.Name, this.Culture, style, out metrics);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is FontFamily family && this.Equals(family);

        /// <inheritdoc/>
        public bool Equals(FontFamily other)
        {
            StringComparer comparer = StringComparerHelpers.GetCaseInsensitiveStringComparer(this.Culture);
            return comparer.Equals(this.Name, other.Name)
                && EqualityComparer<CultureInfo>.Default.Equals(this.Culture, other.Culture)
                && EqualityComparer<IReadOnlyFontMetricsCollection>.Default.Equals(this.collection, other.collection);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(this.collection, this.Name, this.Culture);

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }
}
