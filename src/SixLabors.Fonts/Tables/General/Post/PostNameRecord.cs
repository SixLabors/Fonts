// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.General.Post;

/// <summary>
/// Represents a single glyph name record in the OpenType 'post' table (format 2.0).
/// <see href="https://learn.microsoft.com/en-us/typography/opentype/spec/post"/>
/// </summary>
internal class PostNameRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostNameRecord"/> class.
    /// </summary>
    /// <param name="nameIndex">The index into the glyph name data.</param>
    /// <param name="name">The resolved glyph name string.</param>
    internal PostNameRecord(ushort nameIndex, string name)
    {
        this.Name = name;
        this.NameIndex = nameIndex;
    }

    /// <summary>
    /// Gets the index into the string data or the standard Apple glyph name map.
    /// </summary>
    public ushort NameIndex { get; }

    /// <summary>
    /// Gets the resolved PostScript glyph name.
    /// </summary>
    public string Name { get; }
}
