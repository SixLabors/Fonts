// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Hebrew shaper. Handles mark reordering (PATAH/QAMATS before SHEVA/HIRIQ before METEG)
/// and presentation form composition for legacy fonts without GPOS mark positioning.
/// Based on HarfBuzz: <see href="https://github.com/harfbuzz/harfbuzz/blob/main/src/hb-ot-shaper-hebrew.cc"/>
/// </summary>
internal class HebrewShaper : DefaultShaper
{
    /// <summary>
    /// Hebrew presentation forms with dagesh for U+05D0..U+05EA.
    /// A value of 0x0000 means no dagesh form exists for that letter.
    /// </summary>
    private static readonly ushort[] DageshForms =
    [
        0xFB30, // ALEF
        0xFB31, // BET
        0xFB32, // GIMEL
        0xFB33, // DALET
        0xFB34, // HE
        0xFB35, // VAV
        0xFB36, // ZAYIN
        0x0000, // HET
        0xFB38, // TET
        0xFB39, // YOD
        0xFB3A, // FINAL KAF
        0xFB3B, // KAF
        0xFB3C, // LAMED
        0x0000, // FINAL MEM
        0xFB3E, // MEM
        0x0000, // FINAL NUN
        0xFB40, // NUN
        0xFB41, // SAMEKH
        0x0000, // AYIN
        0xFB43, // FINAL PE
        0xFB44, // PE
        0x0000, // FINAL TSADI
        0xFB46, // TSADI
        0xFB47, // QOF
        0xFB48, // RESH
        0xFB49, // SHIN
        0xFB4A, // TAV
    ];

    /// <summary>The font metrics used for glyph lookups during composition.</summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>Whether the font has GSUB features for Hebrew.</summary>
    private readonly bool hasGsub;

    /// <summary>
    /// Initializes a new instance of the <see cref="HebrewShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <param name="hasGsub">Whether the font has GSUB features for Hebrew.</param>
    public HebrewShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics, bool hasGsub)
        : base(script, MarkZeroingMode.PostGpos, textOptions)
    {
        this.fontMetrics = fontMetrics;
        this.hasGsub = hasGsub;
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        base.AssignFeatures(collection, index, count);

        if (collection is not GlyphSubstitutionCollection substitutionCollection)
        {
            return;
        }

        // Step 1: Reorder Hebrew marks.
        // Swap SHEVA/HIRIQ with following METEG when preceded by PATAH/QAMATS.
        // https://bugzilla.mozilla.org/show_bug.cgi?id=728866
        ReorderMarks(substitutionCollection, index, count);

        // Step 2: Compose Hebrew presentation forms for legacy fonts.
        // Only applied when the font lacks GSUB features (proxy for lacking GPOS mark).
        if (!this.hasGsub)
        {
            ComposeHebrewForms(substitutionCollection, this.fontMetrics, index, count);
        }
    }

    /// <summary>
    /// Reorders Hebrew combining marks to ensure correct rendering.
    /// <para>
    /// Looks for the pattern [PATAH/QAMATS, SHEVA/HIRIQ, METEG/BELOW] and swaps
    /// the last two marks. This ensures correct visual stacking of vowel points
    /// and the meteg stress mark.
    /// </para>
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void ReorderMarks(GlyphSubstitutionCollection collection, int index, int count)
    {
        int end = index + count;
        for (int i = index + 2; i < end; i++)
        {
            int c0 = collection[i - 2].CodePoint.Value;
            int c1 = collection[i - 1].CodePoint.Value;
            int c2 = collection[i].CodePoint.Value;

            // c0: PATAH (U+05B7) or QAMATS (U+05B8)
            // c1: SHEVA (U+05B0) or HIRIQ (U+05B4)
            // c2: METEG (U+05BD) or a below-class mark
            if (IsPatahOrQamats(c0) && IsShevaOrHiriq(c1) && IsMetegOrBelow(c2))
            {
                // Swap positions i-1 and i.
                GlyphShapingData data1 = collection[i - 1];
                GlyphShapingData data2 = collection[i];

                // Swap codepoints and glyph IDs.
                (collection[i - 1].CodePoint, collection[i].CodePoint) = (data2.CodePoint, data1.CodePoint);
                (collection[i - 1].GlyphId, collection[i].GlyphId) = (data2.GlyphId, data1.GlyphId);
                break;
            }
        }
    }

    /// <summary>
    /// Composes Hebrew base + mark sequences into precomposed presentation forms.
    /// This is a fallback for legacy fonts that lack GPOS mark-to-base positioning.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
    private static void ComposeHebrewForms(GlyphSubstitutionCollection collection, FontMetrics fontMetrics, int index, int count)
    {
        int end = index + count;
        for (int i = index + 1; i < end; i++)
        {
            int a = collection[i - 1].CodePoint.Value;
            int b = collection[i].CodePoint.Value;

            int composed = TryCompose(a, b);
            if (composed != 0 && fontMetrics.TryGetGlyphId(new CodePoint(composed), out ushort composedGlyphId))
            {
                // Replace the two glyphs with the composed form.
                collection.Replace(i - 1, 2, composedGlyphId, KnownFeatureTags.GlyphCompositionDecomposition);
                end--;
                i--;
            }
        }
    }

    /// <summary>
    /// Attempts to compose two Hebrew codepoints into a precomposed presentation form.
    /// Returns the composed codepoint, or 0 if no composition exists.
    /// </summary>
    /// <param name="a">The first (base) codepoint value.</param>
    /// <param name="b">The second (combining mark) codepoint value.</param>
    /// <returns>The composed codepoint, or 0 if no composition exists.</returns>
    private static int TryCompose(int a, int b)
    {
        switch (b)
        {
            case 0x05B4: // HIRIQ

                if (a == 0x05D9)
                {
                    // YOD
                    return 0xFB1D;
                }

                break;

            case 0x05B7: // PATAH
                if (a == 0x05F2)
                {
                    // YIDDISH YOD YOD + PATAH
                    return 0xFB1F;
                }

                if (a == 0x05D0)
                {
                    // ALEF + PATAH
                    return 0xFB2E;
                }

                break;

            case 0x05B8: // QAMATS
                if (a == 0x05D0)
                {
                    // ALEF + QAMATS
                    return 0xFB2F;
                }

                break;

            case 0x05B9: // HOLAM
                if (a == 0x05D5)
                {
                    // VAV + HOLAM
                    return 0xFB4B;
                }

                break;

            case 0x05BC: // DAGESH
                if (a is >= 0x05D0 and <= 0x05EA)
                {
                    int form = DageshForms[a - 0x05D0];
                    return form != 0 ? form : 0;
                }

                if (a == 0xFB2A)
                {
                    // SHIN WITH SHIN DOT + DAGESH
                    return 0xFB2C;
                }

                if (a == 0xFB2B)
                {
                    // SHIN WITH SIN DOT + DAGESH
                    return 0xFB2D;
                }

                break;

            case 0x05BF: // RAFE
                if (a == 0x05D1)
                {
                    // BET + RAFE
                    return 0xFB4C;
                }

                if (a == 0x05DB)
                {
                    // KAF + RAFE
                    return 0xFB4D;
                }

                if (a == 0x05E4)
                {
                    // PE + RAFE
                    return 0xFB4E;
                }

                break;

            case 0x05C1: // SHIN DOT
                if (a == 0x05E9)
                {
                    // SHIN + SHIN DOT
                    return 0xFB2A;
                }

                if (a == 0xFB49)
                {
                    // SHIN WITH DAGESH + SHIN DOT
                    return 0xFB2C;
                }

                break;

            case 0x05C2: // SIN DOT
                if (a == 0x05E9)
                {
                    // SHIN + SIN DOT
                    return 0xFB2B;
                }

                if (a == 0xFB49)
                {
                    // SHIN WITH DAGESH + SIN DOT
                    return 0xFB2D;
                }

                break;
        }

        return 0;
    }

    /// <summary>Returns <see langword="true"/> if the codepoint is PATAH (U+05B7) or QAMATS (U+05B8).</summary>
    /// <param name="codepoint">The codepoint value to test.</param>
    /// <returns><see langword="true"/> if the codepoint is PATAH or QAMATS.</returns>
    private static bool IsPatahOrQamats(int codepoint)
        => codepoint is 0x05B7 or 0x05B8;

    /// <summary>Returns <see langword="true"/> if the codepoint is SHEVA (U+05B0) or HIRIQ (U+05B4).</summary>
    /// <param name="codepoint">The codepoint value to test.</param>
    /// <returns><see langword="true"/> if the codepoint is SHEVA or HIRIQ.</returns>
    private static bool IsShevaOrHiriq(int codepoint)
        => codepoint is 0x05B0 or 0x05B4;

    /// <summary>
    /// Returns <see langword="true"/> if the codepoint is METEG (U+05BD) or a combining mark with CCC = Below (220).
    /// Currently checks METEG only; generic CCC=220 detection would require combining class data.
    /// </summary>
    /// <param name="codepoint">The codepoint value to test.</param>
    /// <returns><see langword="true"/> if the codepoint is METEG or a below-class mark.</returns>
    private static bool IsMetegOrBelow(int codepoint)
        => codepoint == 0x05BD;
}
