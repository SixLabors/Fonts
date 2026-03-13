// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Diagnostics;

namespace SixLabors.Fonts.Tables.AdvancedTypographic;

/// <summary>
/// Represents an OpenType feature tag with an enabled/disabled state, used during shaping
/// to track which features are active for a given glyph.
/// </summary>
[DebuggerDisplay("Tag: {Tag}, Enabled: {Enabled}")]
internal struct TagEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagEntry"/> struct.
    /// </summary>
    /// <param name="tag">The feature tag.</param>
    /// <param name="enabled">Whether the feature is enabled.</param>
    public TagEntry(Tag tag, bool enabled)
    {
        this.Tag = tag;
        this.Enabled = enabled;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets the feature tag.
    /// </summary>
    public Tag Tag { get; }
}
