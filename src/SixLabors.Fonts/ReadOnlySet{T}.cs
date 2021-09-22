// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections;
using System.Collections.Generic;

namespace SixLabors.Fonts
{
    /// <inheritdoc/>
    internal class ReadOnlySet<T> : IReadOnlySet<T>
    {
        private readonly ISet<T> set;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlySet{T}"/> class.
        /// </summary>
        /// <param name="set">The set to wrap.</param>
        public ReadOnlySet(ISet<T> set) => this.set = set;

        /// <inheritdoc/>
        public int Count => this.set.Count;

        /// <inheritdoc/>
        public bool Contains(T item) => this.set.Contains(item);

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => this.set.GetEnumerator();

        /// <inheritdoc/>
        public bool IsProperSubsetOf(IEnumerable<T> other) => this.set.IsProperSubsetOf(other);

        /// <inheritdoc/>
        public bool IsProperSupersetOf(IEnumerable<T> other) => this.set.IsProperSupersetOf(other);

        /// <inheritdoc/>
        public bool IsSubsetOf(IEnumerable<T> other) => this.set.IsSubsetOf(other);

        /// <inheritdoc/>
        public bool IsSupersetOf(IEnumerable<T> other) => this.set.IsSupersetOf(other);

        /// <inheritdoc/>
        public bool Overlaps(IEnumerable<T> other) => this.set.Overlaps(other);

        /// <inheritdoc/>
        public bool SetEquals(IEnumerable<T> other) => this.set.SetEquals(other);

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.set.GetEnumerator();
    }
}
