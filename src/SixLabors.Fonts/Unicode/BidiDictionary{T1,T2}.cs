// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// A simple bi-directional dictionary.
/// </summary>
/// <typeparam name="T1">Key type</typeparam>
/// <typeparam name="T2">Value type</typeparam>
internal struct BidiDictionary<T1, T2> : IDisposable
{
    private ArrayBuilder<KeyValuePair<T1, T2>> items = default;
    private ArrayBuilder<Entry<T1>> keys = default;
    private ArrayBuilder<Entry<T2>> values = default;

    public BidiDictionary()
    {
    }

    public void Clear()
    {
        this.items.Dispose();
        this.keys.Dispose();
        this.values.Dispose();
        this = default;
    }

    private void Resize()
    {
        int length = this.items.Count;
        this.keys.Count = length;
        this.values.Count = length;
        for (int i = 0; i < length; i++)
        {
            this.keys[i] = new();
            this.values[i] = new();
        }

        for (int i = 0; i < length; i++)
        {
            Entry<T1> keyEntry = new()
            {
                Index = i,
                Next = -1,
                Key = this.items[i].Key
            };
            int index = Math.Abs(this.items[i].Key!.GetHashCode()) % length;
            ref Entry<T1> currentKeyEntry = ref this.keys[index];
            while (currentKeyEntry.Next >= 0)
            {
                index = currentKeyEntry.Next;
                currentKeyEntry = ref this.keys[index];
            }

            if (currentKeyEntry.Index < 0)
            {
                currentKeyEntry = keyEntry;
            }
            else
            {
                int target = index;
                for (int j = 0; j < length; j++)
                {
                    int k = (target + j) % length;
                    if (this.keys[k].Index < 0)
                    {
                        target = k;
                        break;
                    }
                    else
                    {
                        index = k;
                        currentKeyEntry = ref this.keys[index];
                    }
                }

                this.keys[target] = keyEntry;
                currentKeyEntry.Next = target;
            }

            Entry<T2> valueEntry = new()
            {
                Index = i,
                Next = -1,
                Key = this.items[i].Value
            };

            index = Math.Abs(this.items[i].Value!.GetHashCode()) % length;
            ref Entry<T2> currentValueEntry = ref this.values[index];
            while (currentValueEntry.Next >= 0)
            {
                index = currentValueEntry.Next;
                currentValueEntry = ref this.values[index];
            }

            if (currentValueEntry.Index < 0)
            {
                currentValueEntry = valueEntry;
            }
            else
            {
                int target = index;
                for (int j = 0; j < length; j++)
                {
                    int k = (target + j) % length;
                    if (this.values[k].Index < 0)
                    {
                        target = k;
                        break;
                    }
                    else
                    {
                        index = k;
                        currentValueEntry = ref this.values[index];
                    }
                }

                this.values[target] = valueEntry;
                currentValueEntry.Next = target;
            }
        }
    }

    public void Add(T1 key, T2 value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        bool found = false;
        for (int i = 0; i < this.items.Count; i++)
        {
            if (this.items[i].Key!.Equals(key) || this.items[i].Value!.Equals(value))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            this.items.Add(new(key, value));
            this.Resize();
        }
    }

    public readonly bool TryGetValue(T1 key, out T2 value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        int length = this.items.Count;
        int index = Math.Abs(key.GetHashCode()) % length;
        Entry<T1> currentKeyEntry;
        do
        {
            currentKeyEntry = this.keys[index];
            index = currentKeyEntry.Next;
        }
        while (index >= 0 && !currentKeyEntry.Key!.Equals(key));

        if (currentKeyEntry.Index < 0 || !currentKeyEntry.Key!.Equals(key))
        {
            value = default!;
            return false;
        }
        else
        {
            value = this.items[currentKeyEntry.Index].Value;
            return true;
        }
    }

    public readonly bool TryGetKey(T2 value, out T1 key)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        int length = this.items.Count;
        int index = Math.Abs(value.GetHashCode()) % length;
        Entry<T2> currentKeyEntry;
        do
        {
            currentKeyEntry = this.values[index];
            index = currentKeyEntry.Next;
        }
        while (index >= 0 && !currentKeyEntry.Key!.Equals(value));

        if (currentKeyEntry.Index < 0 || !currentKeyEntry.Key!.Equals(value))
        {
            key = default!;
            return false;
        }
        else
        {
            key = this.items[currentKeyEntry.Index].Key;
            return true;
        }
    }

    public readonly bool ContainsKey(T1 key)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        int length = this.items.Count;
        int index = key.GetHashCode() % length;
        Entry<T1> currentKeyEntry;
        do
        {
            currentKeyEntry = this.keys[index];
            index = currentKeyEntry.Next;
        }
        while (index >= 0 && !currentKeyEntry.Key!.Equals(key));

        return currentKeyEntry.Index >= 0 && currentKeyEntry.Key!.Equals(key);
    }

    public readonly bool ContainsValue(T2 value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        int length = this.items.Count;
        int index = value.GetHashCode() % length;
        Entry<T2> currentKeyEntry;
        do
        {
            currentKeyEntry = this.values[index];
            index = currentKeyEntry.Next;
        }
        while (index >= 0 && !currentKeyEntry.Key!.Equals(value));

        return currentKeyEntry.Index >= 0 && currentKeyEntry.Key!.Equals(value);
    }

    public void Dispose()
    {
        this.items.Dispose();
        this.keys.Dispose();
        this.values.Dispose();
        this = default;
    }

    private struct Entry<T>
    {
        public int Index = -1;
        public int Next = -1;
        public T? Key = default;

        public Entry()
        {
        }
    }
}
