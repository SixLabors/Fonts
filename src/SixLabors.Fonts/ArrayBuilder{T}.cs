// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts;

/// <summary>
/// A helper type for avoiding allocations while building arrays.
/// </summary>
/// <typeparam name="T">The type of item contained in the array.</typeparam>
internal struct ArrayBuilder<T> : IDisposable, IList<T>, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;
    private const int MaxCoreClrArrayLength = 0x7FeFFFFF;

    // Starts out null, initialized on first Add.
    private T[]? data;
    private int size;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayBuilder{T}"/> struct.
    /// </summary>
    /// <param name="capacity">The initial capacity of the array.</param>
    public ArrayBuilder(int capacity)
        : this()
    {
        Guard.MustBeGreaterThanOrEqualTo(capacity, 0, nameof(capacity));

        this.data = ArrayPool<T>.Shared.Rent(capacity);
    }

    /// <summary>
    /// Gets or sets the number of items in the array.
    /// </summary>
    public int Count
    {
        readonly get => this.size;

        set
        {
            if (value != this.size)
            {
                if (value > 0)
                {
                    this.EnsureCapacity(value);
                    this.size = value;
                }
                else
                {
                    this.size = 0;
                }
            }
        }
    }

    public readonly bool IsReadOnly => false;

    T IList<T>.this[int index] { readonly get => this[index]; set => this[index] = value; }

    readonly T IReadOnlyList<T>.this[int index] => this[index];

    /// <summary>
    /// Returns a reference to specified element of the array.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The <typeparamref name="T"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to <see cref="Count"/>.
    /// </exception>
    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            DebugGuard.MustBeBetweenOrEqualTo(index, 0, this.size, nameof(index));
            return ref this.data![index];
        }
    }

    /// <summary>
    /// Adds the given item to the array.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        int position = this.size;

        // Expand the array.
        this.Count++;
        this.data![position] = item;
    }

    /// <summary>
    /// Appends a given number of empty items to the array returning
    /// the items as a slice.
    /// </summary>
    /// <param name="length">The number of items in the slice.</param>
    /// <param name="clear">Whether to clear the new slice, Defaults to <see langword="true"/>.</param>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    public ArraySlice<T> Add(int length, bool clear = true)
    {
        int position = this.size;

        // Expand the array.
        this.Count += length;

        ArraySlice<T> slice = this.AsSlice(position, this.Count - position);
        if (clear)
        {
            slice.Span.Clear();
        }

        return slice;
    }

    /// <summary>
    /// Appends the slice to the array copying the data across.
    /// </summary>
    /// <param name="value">The array slice.</param>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    public ArraySlice<T> Add(in ReadOnlyArraySlice<T> value)
    {
        int position = this.size;

        // Expand the array.
        this.Count += value.Length;

        ArraySlice<T> slice = this.AsSlice(position, this.Count - position);
        value.CopyTo(slice);

        return slice;
    }

    /// <summary>
    /// Clears the array.
    /// Allocated memory is left intact for future usage.
    /// </summary>
    public void Clear() =>

        // No need to actually clear since we're not allowing reference types.
        this.size = 0;

    private void EnsureCapacity(int min)
    {
        int length = this.data?.Length ?? 0;
        if (length < min)
        {
            // Same expansion algorithm as List<T>.
            uint newCapacity = length == 0 ? DefaultCapacity : (uint)length * 2u;
            if (newCapacity > MaxCoreClrArrayLength)
            {
                newCapacity = MaxCoreClrArrayLength;
            }

            if (newCapacity < min)
            {
                newCapacity = (uint)min;
            }

            T[] array = ArrayPool<T>.Shared.Rent((int)newCapacity);
            if (this.size > 0)
            {
                Array.Copy(this.data!, array, this.size);
                ArrayPool<T>.Shared.Return(this.data!);
            }

            this.data = array;
        }
    }

    public readonly bool Contains(T item)
    {
        if (this.size != 0)
        {
            return this.IndexOf(item) != -1;
        }

        return false;
    }

    public readonly int IndexOf(T item) => Array.IndexOf(this.data!, item, 0, this.size);

    public void Insert(int index, T item)
    {
        if (index < 0 || index > this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index != this.Count++)
        {
            Array.Copy(this.data!, index, this.data!, index + 1, this.size - index - 1);
        }

        this[index] = item;
    }

    public bool Remove(T item)
    {
        int index = this.IndexOf(item);
        if (index >= 0)
        {
            this.RemoveAt(index);
            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (index != this.Count - 1)
        {
            Array.Copy(this.data!, index + 1, this.data!, index, this.size - index - 1);
        }

        this.Count--;
    }

    public void RemoveRange(int start, int count)
    {
        if (start < 0 || start >= this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        if (start + count > this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        int end = start + count;
        if (end != this.Count - 1)
        {
            Array.Copy(this.data!, end, this.data!, start, this.size - end);
        }

        this.Count -= count;
    }

    public readonly void Sort() => this.data!.AsSpan(0, this.size).Sort();

    public void Dispose()
    {
        if (this.data != null)
        {
            ArrayPool<T>.Shared.Return(this.data!);
        }

        this = default;
    }

    public readonly IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < this.size; i++)
        {
            yield return this.data![i];
        }
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (array.Length - arrayIndex < this.size)
        {
            throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
        }

        Array.Copy(this.data!, 0, array, arrayIndex, this.size);
    }

    /// <summary>
    /// Returns the current state of the array as a slice.
    /// </summary>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ArraySlice<T> AsSlice() => this.AsSlice(this.Count);

    /// <summary>
    /// Returns the current state of the array as a slice.
    /// </summary>
    /// <param name="length">The number of items in the slice.</param>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ArraySlice<T> AsSlice(int length)
        => new(this.data!, 0, length);

    /// <summary>
    /// Returns the current state of the array as a slice.
    /// </summary>
    /// <param name="start">The index at which to begin the slice.</param>
    /// <param name="length">The number of items in the slice.</param>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ArraySlice<T> AsSlice(int start, int length)
        => new(this.data!, start, length);
}
