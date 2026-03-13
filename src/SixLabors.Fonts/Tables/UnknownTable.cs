// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables;

/// <summary>
/// Represents an unrecognized or unsupported font table.
/// Used as a placeholder when the table loader encounters a tag it has no registered parser for.
/// </summary>
internal sealed class UnknownTable : Table
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownTable"/> class.
    /// </summary>
    /// <param name="name">The four-byte table tag.</param>
    internal UnknownTable(string name)
        => this.Name = name;

    /// <summary>
    /// Gets the four-byte table tag that was not recognized.
    /// </summary>
    public string Name { get; }
}
