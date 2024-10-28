// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;

namespace SixLabors.Fonts.Tests;

internal class ApproximateFloatComparer :
    IEqualityComparer<float>,
    IEqualityComparer<Vector2>,
    IEqualityComparer<IEnumerable<Vector2>>,
    IEqualityComparer<FontRectangle>
{
    private readonly float epsilon;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApproximateFloatComparer"/> class.
    /// </summary>
    /// <param name="epsilon">The comparison error difference epsilon to use.</param>
    public ApproximateFloatComparer(float epsilon = 1F) => this.epsilon = epsilon;

    public bool Equals(float x, float y)
    {
        float d = x - y;
        return d >= -this.epsilon && d <= this.epsilon;
    }

    public bool Equals(Vector2 x, Vector2 y)
        => this.Equals(x.X, y.X) && this.Equals(x.Y, y.Y);

    public bool Equals(IEnumerable<Vector2> x, IEnumerable<Vector2> y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        if (x.Count() != y.Count())
        {
            return false;
        }

        using IEnumerator<Vector2> e1 = x.GetEnumerator();
        using IEnumerator<Vector2> e2 = x.GetEnumerator();
        while (e1.MoveNext())
        {
            if (!(e2.MoveNext() && this.Equals(e1.Current, e2.Current)))
            {
                return false;
            }
        }

        return !e2.MoveNext();
    }

    public bool Equals(FontRectangle x, FontRectangle y)
         => this.Equals(x.X, y.X) &&
            this.Equals(x.Y, y.Y) &&
            this.Equals(x.Width, y.Width)
         && this.Equals(x.Height, y.Height);

    public int GetHashCode(float obj) => obj.GetHashCode();

    public int GetHashCode(Vector2 obj) => obj.GetHashCode();

    public int GetHashCode(IEnumerable<Vector2> obj)
    {
        int hash = 17;
        foreach (Vector2 point in obj)
        {
            hash = (hash * 31) + point.GetHashCode();
        }

        return hash;
    }

    public int GetHashCode(FontRectangle obj)
        => obj.GetHashCode();
}
