// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// A ref struct stack implementation that uses a pooled span to store the data.
/// </summary>
/// <typeparam name="T">The type of elements in the stack.</typeparam>
internal ref struct RefStack<T>
    where T : unmanaged
{
    private const int MaxLength = 0X7FFFFFC7;
    private Buffer<T> buffer;
    private Span<T> stack;
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefStack{T}"/> struct with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the stack. Values less than 1 default to 4.</param>
    public RefStack(int capacity)
    {
        if (capacity < 1)
        {
            capacity = 4;
        }

        this.buffer = new Buffer<T>(capacity);
        this.stack = this.buffer.GetSpan();
        this.isDisposed = false;
        this.Length = 0;
    }

    /// <summary>
    /// Gets the number of elements currently in the stack.
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Gets or sets the element at the specified index in the stack.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        readonly get
        {
            if ((uint)index >= (uint)this.Length)
            {
                ThrowForOutOfRange();
            }

            return this.stack[index];
        }

        set
        {
            if ((uint)index >= (uint)this.Length)
            {
                this.Push(value);
                return;
            }

            this.stack[index] = value;
        }
    }

    /// <summary>
    /// Adds an item to the stack.
    /// </summary>
    /// <param name="value">The item to add.</param>
    public void Push(T value)
    {
        if ((uint)this.Length < (uint)this.stack.Length)
        {
            this.stack[this.Length++] = value;
        }
        else
        {
            int capacity = this.stack.Length * 2;
            if ((uint)capacity > MaxLength)
            {
                capacity = MaxLength;
            }

            var newBuffer = new Buffer<T>(capacity);
            Span<T> newStack = newBuffer.GetSpan();

            this.stack.CopyTo(newStack);
            this.buffer.Dispose();

            this.buffer = newBuffer;
            this.stack = newStack;

            this.stack[this.Length++] = value;
        }
    }

    /// <summary>
    /// Removes the first element of the stack.
    /// </summary>
    /// <returns>The <typeparamref name="T"/> element.</returns>
    public T Shift()
    {
        int newSize = this.Length - 1;
        if (newSize < 0)
        {
            ThrowForEmptyStack();
        }

        T item = this.stack[0];
        this.stack = this.stack.Slice(1);
        this.Length = newSize;
        return item;
    }

    /// <summary>
    /// Removes the last element of the stack.
    /// </summary>
    /// <returns>The <typeparamref name="T"/> element.</returns>
    public T Pop()
    {
        int newSize = this.Length - 1;
        if (newSize < 0)
        {
            ThrowForEmptyStack();
        }

        this.Length = newSize;
        return this.stack[newSize];
    }

    /// <summary>
    /// Clears the current stack.
    /// </summary>
    public void Clear()
    {
        this.Length = 0;
        this.stack = this.buffer.GetSpan();
    }

    /// <summary>
    /// Releases the pooled buffer used by this stack.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.buffer.Dispose();
        this.isDisposed = true;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> for an out-of-range index access.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForOutOfRange()
        => throw new InvalidOperationException("Index must be greater or equal to zero or less than the stack length.");

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when attempting to pop or shift from an empty stack.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForEmptyStack() => throw new InvalidOperationException("Empty stack!");
}
