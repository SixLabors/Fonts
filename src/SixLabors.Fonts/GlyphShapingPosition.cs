// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts;

/// <summary>
/// Stores the bounds, attachment links, and kerning state for one glyph.
/// The buffer sets the initial bounds with the glyph metrics after substitution.
/// </summary>
internal struct GlyphShapingPosition
{
    /// <summary>
    /// The relative cursive attachment value that identifies an unattached glyph. A nonzero value is the signed distance to the parent glyph.
    /// </summary>
    public const int NoCursiveAttachment = 0;

#pragma warning disable SA1401 // Fields exposed so positioning mutates embedded values in place.
    /// <summary>
    /// The shaping bounds. A field rather than a property so positioning lookups
    /// mutate the embedded value in place and re-seeding is plain value assignment.
    /// </summary>
    public GlyphShapingBounds Bounds;

    /// <summary>
    /// The index of any mark attachment, or <c>-1</c> when unattached.
    /// </summary>
    public int MarkAttachment;

    /// <summary>
    /// The signed offset of any cursive attachment, or <see cref="NoCursiveAttachment"/> when unattached.
    /// </summary>
    public int CursiveAttachment;
#pragma warning restore SA1401

    /// <summary>
    /// Initializes a new instance of the <see cref="GlyphShapingPosition"/> struct
    /// with the given bounds and no attachments.
    /// </summary>
    /// <param name="bounds">The seeded shaping bounds.</param>
    public GlyphShapingPosition(GlyphShapingBounds bounds)
    {
        this.Bounds = bounds;
        this.MarkAttachment = -1;
        this.CursiveAttachment = NoCursiveAttachment;
        this.IsKerned = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether this glyph has been kerned.
    /// </summary>
    public bool IsKerned { get; set; }
}
