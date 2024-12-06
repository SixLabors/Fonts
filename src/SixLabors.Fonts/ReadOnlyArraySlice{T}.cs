// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SixLabors.Fonts;

/// <summary>
/// ReadOnlyArraySlice represents a contiguous region of arbitrary memory similar
/// to <see cref="ReadOnlyMemory{T}"/> and <see cref="ReadOnlySpan{T}"/> though constrained
/// to arrays.
/// Unlike <see cref="ReadOnlySpan{T}"/>, it is not a byref-like type.
/// </summary>
/// <typeparam name="T">The type of item contained in the slice.</typeparam>
internal readonly struct ReadOnlyArraySlice<T> : IEnumerable<T>
{
    private readonly T[] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyArraySlice{T}"/> struct.
    /// </summary>
    /// <param name="data">The underlying data buffer.</param>
    public ReadOnlyArraySlice(T[] data)
        : this(data, 0, data.Length)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyArraySlice{T}"/> struct.
    /// </summary>
    /// <param name="data">The underlying data buffer.</param>
    /// <param name="start">The offset position in the underlying buffer this slice was created from.</param>
    /// <param name="length">The number of items in the slice.</param>
    public ReadOnlyArraySlice(T[] data, int start, int length)
    {
        DebugGuard.MustBeGreaterThanOrEqualTo(start, 0, nameof(start));
        DebugGuard.MustBeLessThanOrEqualTo(length, data.Length, nameof(length));
        DebugGuard.MustBeLessThanOrEqualTo(start + length, data.Length, nameof(this.data));

        this.data = data;
        this.Start = start;
        this.Length = length;
    }

    /// <summary>
    /// Gets an empty <see cref="ReadOnlyArraySlice{T}"/>
    /// </summary>
    public static ReadOnlyArraySlice<T> Empty => new(Array.Empty<T>());

    /// <summary>
    /// Gets the offset position in the underlying buffer this slice was created from.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the number of items in the slice.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets a <see cref="Span{T}"/> representing this slice.
    /// </summary>
    public ReadOnlySpan<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this.data, this.Start, this.Length);
    }

    /// <summary>
    /// Returns a reference to specified element of the slice.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The <typeparamref name="T"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
    /// </exception>
    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            DebugGuard.MustBeBetweenOrEqualTo(index, 0, this.Length, nameof(index));
            ref T b = ref MemoryMarshal.GetReference(this.Span);
            return Unsafe.Add(ref b, index);
        }
    }

    /// <summary>
    /// Defines an implicit conversion of an array to a <see cref="ReadOnlyArraySlice{T}"/>
    /// </summary>
    /// <param name="array">The input array.</param>
    public static implicit operator ReadOnlyArraySlice<T>(T[] array)
        => new(array, 0, array.Length);

    /// <summary>
    /// Copies the contents of this slice into destination span. If the source
    /// and destinations overlap, this method behaves as if the original values in
    /// a temporary location before the destination is overwritten.
    /// </summary>
    /// <param name="destination">The slice to copy items into.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination slice is shorter than the source Span.
    /// </exception>
    public void CopyTo(ArraySlice<T> destination)
        => this.Span.CopyTo(destination.Span);

    /// <summary>
    /// Forms a slice out of the given slice, beginning at 'start', of given length
    /// </summary>
    /// <param name="start">The index at which to begin this slice.</param>
    /// <param name="length">The desired length for the slice (exclusive).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the specified <paramref name="start"/> or end index is not in range (&lt;0 or &gt;Length).
    /// </exception>
    public ReadOnlyArraySlice<T> Slice(int start, int length)
        => new(this.data, start, length);

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => new Enumerator(this);

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[]? array;
        private readonly int start;
        private readonly int end; // cache Start + Length, since it's a little slow
        private int current;

        internal Enumerator(ReadOnlyArraySlice<T> slice)
        {
            DebugGuard.NotNull(slice.data, nameof(slice.data));
            DebugGuard.MustBeGreaterThanOrEqualTo(slice.Start, 0, nameof(slice.Start));
            DebugGuard.MustBeGreaterThanOrEqualTo(slice.Length, 0, nameof(slice.Length));

            DebugGuard.MustBeLessThanOrEqualTo(
                slice.Start + slice.Length,
                slice.data.Length,
                nameof(slice.data.Length));

            this.array = slice.data;
            this.start = slice.Start;
            this.end = slice.Start + slice.Length;
            this.current = slice.Start - 1;
        }

        /// <inheritdoc/>
        public readonly T Current
        {
            get
            {
                if (this.current < this.start)
                {
                    ThrowEnumNotStarted();
                }

                if (this.current >= this.end)
                {
                    ThrowEnumEnded();
                }

                return this.array![this.current];
            }
        }

        readonly object? IEnumerator.Current => this.Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (this.current < this.end)
            {
                this.current++;
                return this.current < this.end;
            }

            return false;
        }

        /// <inheritdoc/>
        void IEnumerator.Reset() => this.current = this.start - 1;

        public readonly void Dispose()
        {
        }

        private static void ThrowEnumNotStarted()
            => throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");

        private static void ThrowEnumEnded()
            => throw new InvalidOperationException("Enumeration already finished.");
    }
}
