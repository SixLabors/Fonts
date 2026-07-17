// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Native;

/// <summary>
/// Represents one platform family face resolved to a local font file.
/// </summary>
internal readonly struct NativeSystemFontFace
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NativeSystemFontFace"/> struct.
    /// </summary>
    /// <param name="familyName">The platform family name.</param>
    /// <param name="faceName">The platform face name.</param>
    /// <param name="path">The local font file path.</param>
    /// <param name="style">The platform font style.</param>
    /// <param name="styleScore">The platform-specific preference score for the font style.</param>
    /// <param name="faceIndex">The zero-based face index within the font file.</param>
    public NativeSystemFontFace(string familyName, string faceName, string path, FontStyle style, int styleScore, int faceIndex)
    {
        this.FamilyName = familyName;
        this.FaceName = faceName;
        this.Path = path;
        this.Style = style;
        this.StyleScore = styleScore;
        this.FaceIndex = faceIndex;
    }

    /// <summary>
    /// Gets the platform family name.
    /// </summary>
    public string FamilyName { get; }

    /// <summary>
    /// Gets the platform face name used to identify a specific font inside its family.
    /// </summary>
    public string FaceName { get; }

    /// <summary>
    /// Gets the local font file path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the platform font style.
    /// </summary>
    public FontStyle Style { get; }

    /// <summary>
    /// Gets the platform-specific preference score for the font style.
    /// </summary>
    public int StyleScore { get; }

    /// <summary>
    /// Gets the zero-based face index within the font file.
    /// </summary>
    public int FaceIndex { get; }
}
