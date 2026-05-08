// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// This is a shaper for the Hangul script, used by the Korean language.
/// The shaping state machine was ported from fontkit.
/// <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/HangulShaper.js"/>
/// </summary>
internal sealed class HangulShaper : DefaultShaper
{
    /// <summary>The 'ljmo' (leading Jamo forms) feature tag.</summary>
    private static readonly Tag LjmoTag = Tag.Parse("ljmo");

    /// <summary>The 'vjmo' (vowel Jamo forms) feature tag.</summary>
    private static readonly Tag VjmoTag = Tag.Parse("vjmo");

    /// <summary>The 'tjmo' (trailing Jamo forms) feature tag.</summary>
    private static readonly Tag TjmoTag = Tag.Parse("tjmo");

    /// <summary>The base code point for precomposed Hangul syllables (U+AC00).</summary>
    private const int HangulBase = 0xac00;

    /// <summary>The base code point for leading consonant Jamo (U+1100).</summary>
    private const int LBase = 0x1100; // lead

    /// <summary>The base code point for vowel Jamo (U+1161).</summary>
    private const int VBase = 0x1161; // vowel

    /// <summary>The base code point for trailing consonant Jamo (U+11A7).</summary>
    private const int TBase = 0x11a7; // trail

    /// <summary>The number of leading consonant Jamo.</summary>
    private const int LCount = 19;

    /// <summary>The number of vowel Jamo.</summary>
    private const int VCount = 21;

    /// <summary>The number of trailing consonant Jamo (including no-trail).</summary>
    private const int TCount = 28;

    /// <summary>The last leading consonant Jamo code point.</summary>
    private const int LEnd = LBase + LCount - 1;

    /// <summary>The last vowel Jamo code point.</summary>
    private const int VEnd = VBase + VCount - 1;

    /// <summary>The last trailing consonant Jamo code point.</summary>
    private const int TEnd = TBase + TCount - 1;

    /// <summary>The dotted circle code point (U+25CC) used as a placeholder base.</summary>
    private const int DottedCircle = 0x25cc;

    /// <summary>Other character category.</summary>
    private const byte X = 0;

    /// <summary>Leading consonant category.</summary>
    private const byte L = 1;

    /// <summary>Medial vowel category.</summary>
    private const byte V = 2;

    /// <summary>Trailing consonant category.</summary>
    private const byte T = 3;

    /// <summary>Composed lead-vowel syllable category.</summary>
    private const byte LV = 4;

    /// <summary>Composed lead-vowel-trail syllable category.</summary>
    private const byte LVT = 5;

    /// <summary>Tone mark category.</summary>
    private const byte M = 6;

    /// <summary>No action.</summary>
    private const byte None = 0;

    /// <summary>Decompose composed syllable action.</summary>
    private const byte Decompose = 1;

    /// <summary>Compose Jamo sequence action.</summary>
    private const byte Compose = 2;

    /// <summary>Reorder tone mark action.</summary>
    private const byte ToneMark = 4;

    /// <summary>Invalid sequence (insert dotted circle) action.</summary>
    private const byte Invalid = 5;

    /// <summary>
    /// State machine table for Hangul syllable composition/decomposition.
    /// Each entry is [action, nextState]. Rows are states, columns are character categories.
    /// </summary>
    private static readonly byte[,][] StateTable =
    {
        // #             X                       L                       V                       T                       LV                           LVT                          M
        // State 0: start state
        { new byte[] { None, 0 }, new byte[] { None, 1 }, new byte[] { None, 0 }, new byte[] { None, 0 }, new byte[] { Decompose, 2 }, new byte[] { Decompose, 3 }, new byte[] { Invalid, 0 } },

        // State 1: <L>
        { new byte[] { None, 0 }, new byte[] { None, 1 }, new byte[] { Compose, 2 }, new byte[] { None, 0 }, new byte[] { Decompose, 2 }, new byte[] { Decompose, 3 }, new byte[] { Invalid, 0 } },

        // State 2: <L,V> or <LV>
        { new byte[] { None, 0 }, new byte[] { None, 1 }, new byte[] { None, 0 }, new byte[] { Compose, 3 }, new byte[] { Decompose, 2 }, new byte[] { Decompose, 3 }, new byte[] { ToneMark, 0 } },

        // State 3: <L,V,T> or <LVT>
        { new byte[] { None, 0 }, new byte[] { None, 1 }, new byte[] { None, 0 }, new byte[] { None, 0 }, new byte[] { Decompose, 2 }, new byte[] { Decompose, 3 }, new byte[] { ToneMark, 0 } },
    };

    /// <summary>The font metrics used for glyph lookups during composition/decomposition.</summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangulShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public HangulShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
        : base(script, MarkZeroingMode.None, textOptions)
        => this.fontMetrics = fontMetrics;

    /// <inheritdoc/>
    protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        this.AddFeature(collection, index, count, LjmoTag, false);
        this.AddFeature(collection, index, count, VjmoTag, false);
        this.AddFeature(collection, index, count, TjmoTag, false);
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
    {
        for (int i = index; i < count; i++)
        {
            // Uniscribe does not apply 'calt' for Hangul, and certain fonts
            // (Noto Sans CJK, Source Sans Han, etc) apply all of jamo lookups
            // in calt, which is not desirable.
            collection.DisableShapingFeature(i, CaltTag);
        }

        // Apply the state machine to map glyphs to features.
        if (collection is GlyphSubstitutionCollection substitutionCollection)
        {
            // Allocate a small buffer for composition operations.
            Span<ushort> compositionBuffer = stackalloc ushort[3];

            // GSub
            int state = 0;
            for (int i = 0; i < count; i++)
            {
                if (i + index >= substitutionCollection.Count)
                {
                    break;
                }

                GlyphShapingData data = substitutionCollection[i + index];
                CodePoint codePoint = data.CodePoint;
                int type = GetSyllableType(codePoint);
                byte[] actionsWithState = StateTable[state, type];
                byte action = actionsWithState[0];
                state = actionsWithState[1];

                // TODO: Do not stackalloc in the loop.
                switch (action)
                {
                    case Decompose:

                        // Decompose the composed syllable if it is not supported by the font.
                        if (data.GlyphId == 0)
                        {
                            i = this.DecomposeGlyph(substitutionCollection, data, i, compositionBuffer);
                        }

                        break;

                    case Compose:

                        // Found a decomposed syllable. Try to compose if supported by the font.
                        i = this.ComposeGlyph(substitutionCollection, i, type, compositionBuffer);
                        break;

                    case ToneMark:

                        // Got a valid syllable, followed by a tone mark. Move the tone mark to the beginning of the syllable.
                        this.ReOrderToneMark(substitutionCollection, data, i);
                        break;

                    case Invalid:

                        // Tone mark has no valid syllable to attach to, so insert a dotted circle.
                        i = this.InsertDottedCircle(substitutionCollection, data, i, compositionBuffer);
                        break;
                }
            }
        }
        else
        {
            // GPos
            // Simply loop and enable based on type.
            // Glyph substitution has handled [de]composition.
            for (int i = 0; i < count; i++)
            {
                if (i + index >= collection.Count)
                {
                    break;
                }

                GlyphShapingData data = collection[i + index];
                CodePoint codePoint = data.CodePoint;
                switch (GetSyllableType(codePoint))
                {
                    case L:
                        collection.EnableShapingFeature(i, LjmoTag);
                        break;
                    case V:
                        collection.EnableShapingFeature(i, VjmoTag);
                        break;
                    case T:
                        collection.EnableShapingFeature(i, TjmoTag);
                        break;
                    case LV:
                        collection.EnableShapingFeature(i, LjmoTag);
                        collection.EnableShapingFeature(i, VjmoTag);
                        break;
                    case LVT:
                        collection.EnableShapingFeature(i, LjmoTag);
                        collection.EnableShapingFeature(i, VjmoTag);
                        collection.EnableShapingFeature(i, TjmoTag);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the Hangul syllable type category for a code point.
    /// </summary>
    /// <param name="codePoint">The code point to classify.</param>
    /// <returns>The syllable type constant (L, V, T, LV, LVT, M, or X).</returns>
    private static int GetSyllableType(CodePoint codePoint)
    {
        GraphemeClusterClass type = CodePoint.GetGraphemeClusterClass(codePoint);
        int value = codePoint.Value;

        return type switch
        {
            GraphemeClusterClass.HangulLead => L,
            GraphemeClusterClass.HangulVowel => V,
            GraphemeClusterClass.HangulTail => T,
            GraphemeClusterClass.HangulLeadVowel => LV,
            GraphemeClusterClass.HangulLeadVowelTail => LVT,

            // HANGUL SINGLE DOT TONE MARK
            // HANGUL DOUBLE DOT TONE MARK
            _ => value is >= 0x302E and <= 0x302F ? M : X,
        };
    }

    /// <summary>
    /// Gets the number of Jamo components in a syllable for tone mark reordering.
    /// </summary>
    /// <param name="codePoint">The code point to measure.</param>
    /// <returns>The syllable length in Jamo components.</returns>
    private static int GetSyllableLength(CodePoint codePoint)
        => GetSyllableType(codePoint) switch
        {
            LV or LVT => 1,
            V => 2,
            T => 3,
            _ => 0,
        };

    /// <summary>
    /// Decomposes a precomposed Hangul syllable into its constituent Jamo glyphs.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="data">The shaping data for the composed syllable.</param>
    /// <param name="index">The index of the glyph to decompose.</param>
    /// <param name="compositinoBuffer">A buffer for temporary glyph ID storage.</param>
    /// <returns>The updated index after decomposition.</returns>
    private int DecomposeGlyph(GlyphSubstitutionCollection collection, GlyphShapingData data, int index, Span<ushort> compositinoBuffer)
    {
        // Decompose the syllable into a sequence of glyphs.
        int s = data.CodePoint.Value - HangulBase;
        int t = TBase + (s % TCount);
        s = (s / TCount) | 0;
        int l = (LBase + (s / VCount)) | 0;
        int v = VBase + (s % VCount);

        FontMetrics metrics = this.fontMetrics;

        // Don't decompose if all of the components are not available
        if (!metrics.TryGetGlyphId(new(l), out ushort ljmo) ||
            !metrics.TryGetGlyphId(new(v), out ushort vjmo) ||
            (!metrics.TryGetGlyphId(new(t), out ushort tjmo) && t != TBase))
        {
            return index;
        }

        // Replace the current glyph with decomposed L, V, and T glyphs,
        // and apply the proper OpenType features to each component.
        if (t <= TBase)
        {
            Span<ushort> ii = compositinoBuffer[..2];
            ii[1] = vjmo;
            ii[0] = ljmo;

            collection.Replace(index, ii, KnownFeatureTags.GlyphCompositionDecomposition);
            collection.EnableShapingFeature(index, LjmoTag);
            collection.EnableShapingFeature(index + 1, VjmoTag);
            return index + 1;
        }

        Span<ushort> iii = compositinoBuffer[..3];
        iii[2] = tjmo;
        iii[1] = vjmo;
        iii[0] = ljmo;

        collection.Replace(index, iii, KnownFeatureTags.GlyphCompositionDecomposition);
        collection.EnableShapingFeature(index, LjmoTag);
        collection.EnableShapingFeature(index + 1, VjmoTag);
        collection.EnableShapingFeature(index + 2, TjmoTag);
        return index + 2;
    }

    /// <summary>
    /// Attempts to compose decomposed Jamo into a precomposed Hangul syllable.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="index">The current index in the collection.</param>
    /// <param name="type">The syllable type of the current glyph.</param>
    /// <param name="compositionBuffer">A buffer for glyph IDs during composition.</param>
    /// <returns>The updated index after composition.</returns>
    private int ComposeGlyph(GlyphSubstitutionCollection collection, int index, int type, Span<ushort> compositionBuffer)
    {
        if (index == 0)
        {
            return index;
        }

        GlyphShapingData prev = collection[index - 1];
        CodePoint prevCodePoint = prev.CodePoint;
        int prevType = GetSyllableType(prevCodePoint);

        // Figure out what type of syllable we're dealing with
        CodePoint lv = default;
        int ljmo = -1, vjmo = -1, tjmo = -1;

        if (prevType == LV && type == T)
        {
            // <LV,T>
            lv = prevCodePoint;
            tjmo = index;
        }
        else
        {
            if (type == V)
            {
                // <L,V>
                ljmo = index - 1;
                vjmo = index;
            }
            else
            {
                // <L,V,T>
                ljmo = index - 2;
                vjmo = index - 1;
                tjmo = index;
            }

            CodePoint l = collection[ljmo].CodePoint;
            CodePoint v = collection[vjmo].CodePoint;

            // Make sure L and V are combining characters
            if (IsCombiningL(l) && IsCombiningV(v))
            {
                lv = new CodePoint(HangulBase + ((((l.Value - LBase) * VCount) + (v.Value - VBase)) * TCount));
            }
        }

        CodePoint t = tjmo >= 0 ? collection[tjmo].CodePoint : new CodePoint(TBase);
        if ((lv != default) && (t.Value == TBase || IsCombiningT(t)))
        {
            CodePoint s = new(lv.Value + (t.Value - TBase));

            // Replace with a composed glyph if supported by the font,
            // otherwise apply the proper OpenType features to each component.
            if (this.fontMetrics.TryGetGlyphId(s, out ushort id))
            {
                int del = prevType == V ? 3 : 2;
                int idx = index - del + 1;
                collection.Replace(idx, del - 1, id, KnownFeatureTags.GlyphCompositionDecomposition);
                collection[idx].CodePoint = s;
                return idx;
            }
        }

        // Didn't compose (either a non-combining component or unsupported by font).
        if (ljmo >= 0)
        {
            collection.EnableShapingFeature(ljmo, LjmoTag);
        }

        if (vjmo >= 0)
        {
            collection.EnableShapingFeature(vjmo, VjmoTag);
        }

        if (tjmo >= 0)
        {
            collection.EnableShapingFeature(tjmo, TjmoTag);
        }

        if (prevType == LV)
        {
            // Sequence was originally <L,V>, which got combined earlier.
            // Either the T was non-combining, or the LVT glyph wasn't supported.
            // Decompose the glyph again and apply OT features.
            this.DecomposeGlyph(collection, collection[index - 1], index - 1, compositionBuffer);
            return index + 1;
        }

        return index;
    }

    /// <summary>
    /// Reorders a tone mark to the beginning of the preceding syllable.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="data">The shaping data of the tone mark glyph.</param>
    /// <param name="index">The index of the tone mark in the collection.</param>
    private void ReOrderToneMark(GlyphSubstitutionCollection collection, GlyphShapingData data, int index)
    {
        if (index == 0)
        {
            return;
        }

        // Move tone mark to the beginning of the previous syllable, unless it is zero width
        // We don't have access to the glyphs metrics as an array when substituting so we have to loop.
        FontMetrics fontMetrics = this.fontMetrics;
        TextAttributes textAttributes = data.TextRun.TextAttributes;
        TextDecorations textDecorations = data.TextRun.TextDecorations;
        LayoutMode layoutMode = collection.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = collection.TextOptions.ColorFontSupport;
        if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, out FontGlyphMetrics? metrics)
            && metrics.AdvanceWidth == 0)
        {
            return;
        }

        GlyphShapingData prev = collection[index - 1];
        int len = GetSyllableLength(prev.CodePoint);
        collection.MoveGlyph(index, index - len);
    }

    /// <summary>
    /// Inserts a dotted circle glyph as a placeholder for an invalid tone mark that has no syllable to attach to.
    /// </summary>
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="data">The shaping data of the invalid tone mark glyph.</param>
    /// <param name="index">The index of the tone mark in the collection.</param>
    /// <param name="compositionBuffer">A buffer for glyph IDs during insertion.</param>
    /// <returns>The updated index after insertion.</returns>
    private int InsertDottedCircle(GlyphSubstitutionCollection collection, GlyphShapingData data, int index, Span<ushort> compositionBuffer)
    {
        bool after = false;
        FontMetrics fontMetrics = this.fontMetrics;

        if (fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort id))
        {
            TextAttributes textAttributes = data.TextRun.TextAttributes;
            TextDecorations textDecorations = data.TextRun.TextDecorations;
            LayoutMode layoutMode = collection.TextOptions.LayoutMode;
            ColorFontSupport colorFontSupport = collection.TextOptions.ColorFontSupport;
            if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, out FontGlyphMetrics? metrics)
                && metrics.AdvanceWidth != 0)
            {
                after = true;
            }

            // If the tone mark is zero width, insert the dotted circle before, otherwise after
            Span<ushort> glyphs = compositionBuffer[..2];
            if (after)
            {
                glyphs[1] = id;
                glyphs[0] = data.GlyphId;
            }
            else
            {
                glyphs[1] = data.GlyphId;
                glyphs[0] = id;
            }

            collection.Replace(index, glyphs, KnownFeatureTags.GlyphCompositionDecomposition);
            return index + 1;
        }

        return index;
    }

    /// <summary>
    /// Determines whether the code point is a combining leading consonant Jamo.
    /// </summary>
    /// <param name="code">The code point to test.</param>
    /// <returns><see langword="true"/> if the code point is in the leading Jamo range.</returns>
    private static bool IsCombiningL(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, LBase, LEnd);

    /// <summary>
    /// Determines whether the code point is a combining vowel Jamo.
    /// </summary>
    /// <param name="code">The code point to test.</param>
    /// <returns><see langword="true"/> if the code point is in the vowel Jamo range.</returns>
    private static bool IsCombiningV(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, VBase, VEnd);

    /// <summary>
    /// Determines whether the code point is a combining trailing consonant Jamo.
    /// </summary>
    /// <param name="code">The code point to test.</param>
    /// <returns><see langword="true"/> if the code point is in the trailing Jamo range.</returns>
    private static bool IsCombiningT(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, TBase + 1, TEnd);
}
