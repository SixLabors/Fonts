// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Collections.Concurrent;

namespace SixLabors.Fonts;

/// <summary>
/// A pool for reusing objects of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>
/// This implementation keeps a cache of retained objects.
/// This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be Garbage Collected.
/// </remarks>
internal sealed class ObjectPool<T>
    where T : class
{
    private readonly Func<T> createFunc;
    private readonly Func<T, bool> returnFunc;
    private readonly int maxCapacity;
    private int numItems;

    private readonly ConcurrentQueue<T> items = new();
    private T? fastItem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    public ObjectPool(IPooledObjectPolicy<T> policy)
        : this(policy, Environment.ProcessorCount * 2)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public ObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        // cache the target interface methods, to avoid interface lookup overhead
        this.createFunc = policy.Create;
        this.returnFunc = policy.Return;
        this.maxCapacity = maximumRetained - 1;  // -1 to account for _fastItem
    }

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T"/>.</returns>
    public T Get()
    {
        T? item = this.fastItem;
        if (item == null || Interlocked.CompareExchange(ref this.fastItem, null, item) != item)
        {
            if (this.items.TryDequeue(out item))
            {
                _ = Interlocked.Decrement(ref this.numItems);
                return item;
            }

            // no object available, so go get a brand new one
            return this.createFunc();
        }

        return item;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public void Return(T obj) => this.ReturnCore(obj);

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>true if the object was returned to the pool</returns>
    private bool ReturnCore(T obj)
    {
        if (!this.returnFunc(obj))
        {
            // policy says to drop this object
            return false;
        }

        if (this.fastItem != null || Interlocked.CompareExchange(ref this.fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref this.numItems) <= this.maxCapacity)
            {
                this.items.Enqueue(obj);
                return true;
            }

            // no room, clean up the count and drop the object on the floor
            _ = Interlocked.Decrement(ref this.numItems);
            return false;
        }

        return true;
    }
}

/// <summary>
/// Represents a policy for managing pooled objects.
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
#pragma warning disable SA1201 // Elements should appear in the correct order
internal interface IPooledObjectPolicy<T>
#pragma warning restore SA1201 // Elements should appear in the correct order
    where T : notnull
{
    /// <summary>
    /// Create a <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The <typeparamref name="T"/> which was created.</returns>
    public T Create();

    /// <summary>
    /// Runs some processing when an object was returned to the pool. Can be used to reset the state of an object and indicate if the object should be returned to the pool.
    /// </summary>
    /// <param name="obj">The object to return to the pool.</param>
    /// <returns><see langword="true" /> if the object should be returned to the pool. <see langword="false" /> if it's not possible/desirable for the pool to keep the object.</returns>
    public bool Return(T obj);
}
