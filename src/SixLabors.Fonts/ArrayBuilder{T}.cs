// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts;

/// <summary>
/// A helper type for avoiding allocations while building arrays.
/// </summary>
/// <typeparam name="T">The type of item contained in the array.</typeparam>
internal struct ArrayBuilder<T>
    where T : struct
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
    public int Length
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

    /// <summary>
    /// Returns a reference to specified element of the array.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The <typeparamref name="T"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown when index less than 0 or index greater than or equal to <see cref="Length"/>.
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
        this.Length++;
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
        this.Length += length;

        ArraySlice<T> slice = this.AsSlice(position, this.Length - position);
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
        this.Length += value.Length;

        ArraySlice<T> slice = this.AsSlice(position, this.Length - position);
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
        for (int i = 0; i < this.size; i++)
        {
            if (this[i].Equals(item))
            {
                return true;
            }
        }

        return false;
    }

    public readonly int IndexOf(T item)
    {
        for (int i = 0; i < this.size; i++)
        {
            if (this[i].Equals(item))
            {
                return i;
            }
        }

        return -1;
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index > this.size)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        this.Length++;
        Array.Copy(this.data!, index, this.data!, index + 1, this.size - index - 1);
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

        Array.Copy(this.data!, index + 1, this.data!, index, this.size - index - 1);
        this.Length--;
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

        this.Length -= count;
    }

    public readonly void Sort() => this.data!.AsSpan(0, this.size).Sort();

    public unsafe void Free()
    {
        if (this.data != null)
        {
            ArrayPool<T>.Shared.Return(this.data!);
        }

        this = default;
    }

    /// <summary>
    /// Returns the current state of the array as a slice.
    /// </summary>
    /// <returns>The <see cref="ArraySlice{T}"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ArraySlice<T> AsSlice() => this.AsSlice(this.Length);

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
