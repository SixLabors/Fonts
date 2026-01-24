// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.TrueType;

/// <content>
/// Contains compatibility lists for tricky fonts.
/// </content>
public partial class TrueTypeGlyphMetrics
{
    /// <summary>
    /// Represents a set of font family names that require font hinting to render correctly.
    /// Base on the Freetype list in ttobjs.c
    /// </summary>
    private static readonly HashSet<string> MustHintFonts =
        new(StringComparer.Ordinal)
        {
            "cpop",
            "DFGirl-W6-WIN-BF",
            "DFGothic-EB",
            "DFGyoSho-Lt",
            "DFHei",
            "DFHSGothic-W5",
            "DFHSMincho-W3",
            "DFHSMincho-W7",
            "DFKaiSho-SB",
            "DFKaiShu",
            "DFKai-SB",
            "DFMing",
            "DLC",
            "HuaTianKaiTi?",
            "HuaTianSongTi?",
            "Ming(for ISO10646)",
            "MingLiU",
            "MingMedium",
            "PMingLiU",
            "MingLi43",
        };

    /// <summary>
    /// Contains the set of font family names that should never be suggested as font hints.
    /// Based on community reports of rendering issues.
    /// </summary>
    private static readonly HashSet<string> NeverHint =
        new(StringComparer.Ordinal)
        {
            "MgOpen Canonica",
            "MgOpenCanonica",
        };

    /// <summary>
    /// Determines the effective hinting mode for the current font based on its name and the specified mode.
    /// </summary>
    /// <remarks>
    /// If the font name matches an entry in the internal 'NeverHint' list, hinting is disabled
    /// regardless of the requested mode. If the font name matches an entry in the 'MustHintFonts' list, standard
    /// hinting is enforced. Otherwise, the provided mode is used. This method ensures consistent rendering for certain
    /// fonts that require special handling.
    /// </remarks>
    /// <param name="mode">The requested hinting mode to use if no font-specific override applies.</param>
    /// <returns>
    /// A value indicating the hinting mode to apply. Returns a font-specific override if the font name matches a
    /// configured pattern; otherwise, returns the specified mode.
    /// </returns>
    private HintingMode GetHintingMode(HintingMode mode)
    {
        ReadOnlySpan<char> faceName = SkipPdfFontRandomTag(this.FontMetrics.Description.FontNameInvariantCulture);

        // We use partial matching here since some platforms/face collections may include additional style or
        // foundry-specific information in the face name.
        foreach (string needle in NeverHint)
        {
            if (faceName.Contains(needle, StringComparison.Ordinal))
            {
                return HintingMode.None;
            }
        }

        foreach (string needle in MustHintFonts)
        {
            if (faceName.Contains(needle, StringComparison.Ordinal))
            {
                return HintingMode.Standard;
            }
        }

        return mode;
    }

    private static ReadOnlySpan<char> SkipPdfFontRandomTag(string name)
    {
        // Fonts embedded in PDFs are sometimes made unique by prepending a randomization prefix to their names.
        // As defined in the PDF Reference ("Font Subsets"), it consists of 6 uppercase letters followed by '+'.
        // For safety, we only skip prefixes that conform to this rule.
        if (name.Length > 7
            && IsAsciiUpper(name[0])
            && IsAsciiUpper(name[1])
            && IsAsciiUpper(name[2])
            && IsAsciiUpper(name[3])
            && IsAsciiUpper(name[4])
            && IsAsciiUpper(name[5])
            && name[6] == '+')
        {
            return name.AsSpan(7);
        }

        return name;

        static bool IsAsciiUpper(char c) => (uint)(c - 'A') <= ('Z' - 'A');
    }
}
