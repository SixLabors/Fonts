// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Unicode;

/// <summary>
/// A simple bi-directional dictionary.
/// </summary>
/// <typeparam name="T1">Key type</typeparam>
/// <typeparam name="T2">Value type</typeparam>
internal sealed class BidiDictionary<T1, T2>
    where T1 : struct
    where T2 : struct
{
    public Dictionary<T1, T2> Forward { get; } = new Dictionary<T1, T2>();

    public Dictionary<T2, T1> Reverse { get; } = new Dictionary<T2, T1>();

    public void Clear()
    {
        this.Forward.Clear();
        this.Reverse.Clear();
    }

    public void Add(T1 key, T2 value)
    {
        this.Forward.Add(key, value);
        this.Reverse.Add(value, key);
    }

    public bool TryGetValue(T1 key, out T2 value) => this.Forward.TryGetValue(key, out value);

    public bool TryGetKey(T2 value, out T1 key) => this.Reverse.TryGetValue(value, out key);

    public bool ContainsKey(T1 key) => this.Forward.ContainsKey(key);

    public bool ContainsValue(T2 value) => this.Reverse.ContainsKey(value);
}
