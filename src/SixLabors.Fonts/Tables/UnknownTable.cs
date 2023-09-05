// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

internal sealed class UnknownTable : Table
{
    internal UnknownTable(string name)
        => this.Name = name;

    public string Name { get; }
}
