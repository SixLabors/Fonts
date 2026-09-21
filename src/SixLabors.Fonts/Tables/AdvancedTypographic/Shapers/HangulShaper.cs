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
    /// <summary>
    /// The 'ljmo' (leading Jamo forms) feature tag.
    /// </summary>
    private static readonly Tag LjmoTag = Tag.Parse("ljmo");

    /// <summary>
    /// The 'vjmo' (vowel Jamo forms) feature tag.
    /// </summary>
    private static readonly Tag VjmoTag = Tag.Parse("vjmo");

    /// <summary>
    /// The 'tjmo' (trailing Jamo forms) feature tag.
    /// </summary>
    private static readonly Tag TjmoTag = Tag.Parse("tjmo");

    /// <summary>
    /// The base code point for precomposed Hangul syllables (U+AC00).
    /// </summary>
    private const int HangulBase = 0xac00;

    /// <summary>
    /// The base code point for leading consonant Jamo (U+1100).
    /// </summary>
    private const int LBase = 0x1100; // lead

    /// <summary>
    /// The base code point for vowel Jamo (U+1161).
    /// </summary>
    private const int VBase = 0x1161; // vowel

    /// <summary>
    /// The base code point for trailing consonant Jamo (U+11A7).
    /// </summary>
    private const int TBase = 0x11a7; // trail

    /// <summary>
    /// The number of leading consonant Jamo.
    /// </summary>
    private const int LCount = 19;

    /// <summary>
    /// The number of vowel Jamo.
    /// </summary>
    private const int VCount = 21;

    /// <summary>
    /// The number of trailing consonant Jamo (including no-trail).
    /// </summary>
    private const int TCount = 28;

    /// <summary>
    /// The last leading consonant Jamo code point.
    /// </summary>
    private const int LEnd = LBase + LCount - 1;

    /// <summary>
    /// The last vowel Jamo code point.
    /// </summary>
    private const int VEnd = VBase + VCount - 1;

    /// <summary>
    /// The last trailing consonant Jamo code point.
    /// </summary>
    private const int TEnd = TBase + TCount - 1;

    /// <summary>
    /// The dotted circle code point (U+25CC) used as a placeholder base.
    /// </summary>
    private const int DottedCircle = 0x25cc;

    /// <summary>
    /// Other character category.
    /// </summary>
    private const byte X = 0;

    /// <summary>
    /// Leading consonant category.
    /// </summary>
    private const byte L = 1;

    /// <summary>
    /// Medial vowel category.
    /// </summary>
    private const byte V = 2;

    /// <summary>
    /// Trailing consonant category.
    /// </summary>
    private const byte T = 3;

    /// <summary>
    /// Composed lead-vowel syllable category.
    /// </summary>
    private const byte LV = 4;

    /// <summary>
    /// Composed lead-vowel-trail syllable category.
    /// </summary>
    private const byte LVT = 5;

    /// <summary>
    /// Tone mark category.
    /// </summary>
    private const byte M = 6;

    /// <summary>
    /// No action.
    /// </summary>
    private const byte None = 0;

    /// <summary>
    /// Decompose composed syllable action.
    /// </summary>
    private const byte Decompose = 1;

    /// <summary>
    /// Compose Jamo sequence action.
    /// </summary>
    private const byte Compose = 2;

    /// <summary>
    /// Reorder tone mark action.
    /// </summary>
    private const byte ToneMark = 4;

    /// <summary>
    /// Invalid sequence (insert dotted circle) action.
    /// </summary>
    private const byte Invalid = 5;

    /// <summary>
    /// State machine table for Hangul syllable composition/decomposition.
    /// Each entry is [action, nextState]. Rows are states, columns are character categories.
    /// </summary>
    private static readonly byte[,][] StateTable =
    {
        // #             X                       L                       V                       T                       LV                           LVT                          M
        // State 0: start state
        { [None, 0], [None, 1], [None, 0], [None, 0], [Decompose, 2], [Decompose, 3], [Invalid, 0] },

        // State 1: <L>
        { [None, 0], [None, 1], [Compose, 2], [None, 0], [Decompose, 2], [Decompose, 3], [Invalid, 0] },

        // State 2: <L,V> or <LV>
        { [None, 0], [None, 1], [None, 0], [Compose, 3], [Decompose, 2], [Decompose, 3], [ToneMark, 0] },

        // State 3: <L,V,T> or <LVT>
        { [None, 0], [None, 1], [None, 0], [None, 0], [Decompose, 2], [Decompose, 3], [ToneMark, 0] },
    };

    /// <summary>
    /// The font metrics used for glyph lookups during composition/decomposition.
    /// </summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangulShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    public HangulShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics)
        : base(script, MarkZeroingMode.None, textOptions)
    {
        this.fontMetrics = fontMetrics;

        // The text is left exactly as it was written. This shaper composes and
        // decomposes syllables itself, by the rules the script's own features
        // describe, and a font of this script is not built to mix ready-made
        // syllables with the letters they are built from. Taking the text apart
        // beforehand would hand it text of both kinds at once.
        this.NormalizationMode = NormalizationMode.None;
    }

    /// <inheritdoc/>
    protected override void PlanFeatures(ShapingBuffer buffer, int index, int count)
    {
        this.AddFeature(buffer, index, count, LjmoTag, false);
        this.AddFeature(buffer, index, count, VjmoTag, false);
        this.AddFeature(buffer, index, count, TjmoTag, false);
    }

    /// <inheritdoc/>
    protected override void PlanPostprocessingFeatures(ShapingBuffer buffer, int index, int count)
    {
        base.PlanPostprocessingFeatures(buffer, index, count);

        // Certain fonts (Noto Sans CJK, Source Han Sans, etc) apply all of the
        // jamo lookups through contextual alternates, which is not desirable.
        // The feature is demoted from global to a varying feature whose mask is
        // off by default: no glyph applies it unless a later range registration
        // enables it, and feature assignment clears it on jamo even then, so the
        // jamo lookups such fonts hide behind the feature can never fire there.
        this.AddFeature(buffer, index, count, CaltTag, false);
    }

    /// <inheritdoc/>
    protected override void AssignFeatures(ShapingBuffer buffer, int index, int count)
    {
        int entryCount = buffer.Count;

        // Apply the state machine to map glyphs to features.
        if (buffer.Role == ShapingBufferRole.Substitution)
        {
            // Hangul composition and decomposition use at most three jamo. Keep one
            // fixed scratch span outside the state-machine loop for every syllable.
            Span<ushort> compositionBuffer = stackalloc ushort[3];

            // GSub
            int state = 0;
            for (int i = 0; i < count; i++)
            {
                if (i + index >= buffer.Count)
                {
                    break;
                }

                ref GlyphShapingData data = ref buffer[i + index];
                CodePoint codePoint = data.CodePoint;
                int type = GetSyllableType(codePoint);
                byte[] actionsWithState = StateTable[state, type];
                byte action = actionsWithState[0];
                state = actionsWithState[1];

                switch (action)
                {
                    case Decompose:

                        // Decompose the composed syllable if it is not supported by the font.
                        if (data.GlyphId == 0)
                        {
                            i = this.DecomposeGlyph(buffer, ref data, i, compositionBuffer);
                        }

                        break;

                    case Compose:

                        // Found a decomposed syllable. Try to compose if supported by the font.
                        i = this.ComposeGlyph(buffer, i, type, compositionBuffer);
                        break;

                    case ToneMark:

                        // Got a valid syllable, followed by a tone mark. Move the tone mark to the beginning of the syllable.
                        this.ReOrderToneMark(buffer, ref data, i);
                        break;

                    case Invalid:

                        // Tone mark has no valid syllable to attach to, so insert a dotted circle.
                        i = this.InsertDottedCircle(buffer, ref data, i, compositionBuffer);
                        break;
                }
            }
        }
        else
        {
            // GPos
            // Simply loop and enable based on type.
            // Glyph substitution has handled [de]composition.
            // The three Jamo masks are invariant for the run, so resolving them
            // before the loop avoids searching the plan for every glyph.
            uint ljmoMask = this.Features.GetMask(LjmoTag);
            uint vjmoMask = this.Features.GetMask(VjmoTag);
            uint tjmoMask = this.Features.GetMask(TjmoTag);

            for (int i = 0; i < count; i++)
            {
                if (i + index >= buffer.Count)
                {
                    break;
                }

                ref GlyphShapingData data = ref buffer[i + index];
                CodePoint codePoint = data.CodePoint;
                switch (GetSyllableType(codePoint))
                {
                    case L:
                        buffer.EnableShapingFeature(i, ljmoMask);
                        break;
                    case V:
                        buffer.EnableShapingFeature(i, vjmoMask);
                        break;
                    case T:
                        buffer.EnableShapingFeature(i, tjmoMask);
                        break;
                    case LV:
                        buffer.EnableShapingFeature(i, ljmoMask);
                        buffer.EnableShapingFeature(i, vjmoMask);
                        break;
                    case LVT:
                        buffer.EnableShapingFeature(i, ljmoMask);
                        buffer.EnableShapingFeature(i, vjmoMask);
                        buffer.EnableShapingFeature(i, tjmoMask);
                        break;
                }
            }
        }

        // Keep contextual alternates away from jamo, running after composition
        // and decomposition so the check sees the segment's final code points:
        // composed syllables keep the feature while the jamo lookups some fonts
        // hide behind it can never fire on the jamo themselves.
        count += buffer.Count - entryCount;
        int end = index + count;

        // The contextual-alternates mask is likewise invariant while this run is
        // scanned for decomposed Jamo.
        uint caltMask = this.Features.GetMask(CaltTag);
        for (int i = index; i < end && i < buffer.Count; i++)
        {
            int type = GetSyllableType(buffer[i].CodePoint);
            if (type is L or V or T)
            {
                buffer.DisableShapingFeature(i, caltMask);
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
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="data">The shaping data for the composed syllable.</param>
    /// <param name="index">The index of the glyph to decompose.</param>
    /// <param name="compositinoBuffer">A buffer for temporary glyph ID storage.</param>
    /// <returns>The updated index after decomposition.</returns>
    private int DecomposeGlyph(ShapingBuffer buffer, ref GlyphShapingData data, int index, Span<ushort> compositinoBuffer)
    {
        // Decompose the syllable into a sequence of glyphs.
        int s = data.CodePoint.Value - HangulBase;
        int t = TBase + (s % TCount);
        s = (s / TCount) | 0;
        int l = (LBase + (s / VCount)) | 0;
        int v = VBase + (s % VCount);

        FontMetrics metrics = this.fontMetrics;

        // Don't decompose if all of the components are not available
        if (!metrics.TryGetGlyphId(new CodePoint(l), out ushort ljmo) ||
            !metrics.TryGetGlyphId(new CodePoint(v), out ushort vjmo) ||
            (!metrics.TryGetGlyphId(new CodePoint(t), out ushort tjmo) && t != TBase))
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

            buffer.Replace(index, ii, KnownFeatureTags.GlyphCompositionDecomposition);
            buffer.EnableShapingFeature(index, this.Features.GetMask(LjmoTag));
            buffer.EnableShapingFeature(index + 1, this.Features.GetMask(VjmoTag));
            return index + 1;
        }

        Span<ushort> iii = compositinoBuffer[..3];
        iii[2] = tjmo;
        iii[1] = vjmo;
        iii[0] = ljmo;

        buffer.Replace(index, iii, KnownFeatureTags.GlyphCompositionDecomposition);
        buffer.EnableShapingFeature(index, this.Features.GetMask(LjmoTag));
        buffer.EnableShapingFeature(index + 1, this.Features.GetMask(VjmoTag));
        buffer.EnableShapingFeature(index + 2, this.Features.GetMask(TjmoTag));
        return index + 2;
    }

    /// <summary>
    /// Attempts to compose decomposed Jamo into a precomposed Hangul syllable.
    /// </summary>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="index">The current index in the buffer.</param>
    /// <param name="type">The syllable type of the current glyph.</param>
    /// <param name="compositionBuffer">A buffer for glyph IDs during composition.</param>
    /// <returns>The updated index after composition.</returns>
    private int ComposeGlyph(ShapingBuffer buffer, int index, int type, Span<ushort> compositionBuffer)
    {
        if (index == 0)
        {
            return index;
        }

        ref GlyphShapingData prev = ref buffer[index - 1];
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

            CodePoint l = buffer[ljmo].CodePoint;
            CodePoint v = buffer[vjmo].CodePoint;

            // Make sure L and V are combining characters
            if (IsCombiningL(l) && IsCombiningV(v))
            {
                lv = new CodePoint(HangulBase + ((((l.Value - LBase) * VCount) + (v.Value - VBase)) * TCount));
            }
        }

        CodePoint t = tjmo >= 0 ? buffer[tjmo].CodePoint : new CodePoint(TBase);
        if ((lv != default) && (t.Value == TBase || IsCombiningT(t)))
        {
            CodePoint s = new(lv.Value + (t.Value - TBase));

            // Replace with a composed glyph if supported by the font,
            // otherwise apply the proper OpenType features to each component.
            if (this.fontMetrics.TryGetGlyphId(s, out ushort id))
            {
                int del = prevType == V ? 3 : 2;
                int idx = index - del + 1;
                buffer.Replace(idx, del - 1, id, KnownFeatureTags.GlyphCompositionDecomposition);
                buffer[idx].CodePoint = s;
                return idx;
            }
        }

        // Didn't compose (either a non-combining component or unsupported by font).
        if (ljmo >= 0)
        {
            buffer.CombineInputStarts(ljmo, (tjmo >= 0 ? tjmo : vjmo) + 1);
            buffer.EnableShapingFeature(ljmo, this.Features.GetMask(LjmoTag));
        }

        if (vjmo >= 0)
        {
            buffer.EnableShapingFeature(vjmo, this.Features.GetMask(VjmoTag));
        }

        if (tjmo >= 0)
        {
            buffer.EnableShapingFeature(tjmo, this.Features.GetMask(TjmoTag));
        }

        if (prevType == LV)
        {
            // Sequence was originally <L,V>, which got combined earlier.
            // Either the T was non-combining, or the LVT glyph wasn't supported.
            // Decompose the glyph again and apply OT features.
            this.DecomposeGlyph(buffer, ref buffer[index - 1], index - 1, compositionBuffer);
            return index + 1;
        }

        return index;
    }

    /// <summary>
    /// Reorders a tone mark to the beginning of the preceding syllable.
    /// </summary>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="data">The shaping data of the tone mark glyph.</param>
    /// <param name="index">The index of the tone mark in the buffer.</param>
    private void ReOrderToneMark(ShapingBuffer buffer, ref GlyphShapingData data, int index)
    {
        if (index == 0)
        {
            return;
        }

        // Move tone mark to the beginning of the previous syllable, unless it is zero width
        // We don't have access to the glyphs metrics as an array when substituting so we have to loop.
        FontMetrics fontMetrics = this.fontMetrics;
        TextRun textRun = buffer.TextRuns[data.TextRunIndex];
        TextAttributes textAttributes = textRun.TextAttributes;
        TextDecorations textDecorations = textRun.TextDecorations;
        LayoutMode layoutMode = buffer.TextOptions.LayoutMode;
        ColorFontSupport colorFontSupport = textRun.ColorFontSupport ?? buffer.TextOptions.ColorFontSupport;
        FontPalette? fontPalette = textRun.FontPalette ?? buffer.TextOptions.FontPalette;
        if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, fontPalette, out FontGlyphMetrics? metrics)
            && metrics.AdvanceWidth == 0)
        {
            return;
        }

        ref GlyphShapingData prev = ref buffer[index - 1];
        int len = GetSyllableLength(prev.CodePoint);
        int syllableStart = index - len;

        buffer.CombineInputStarts(syllableStart, index + 1);
        buffer.MoveGlyph(index, syllableStart);
    }

    /// <summary>
    /// Inserts a dotted circle glyph as a placeholder for an invalid tone mark that has no syllable to attach to.
    /// </summary>
    /// <param name="buffer">The glyph substitution buffer.</param>
    /// <param name="data">The shaping data of the invalid tone mark glyph.</param>
    /// <param name="index">The index of the tone mark in the buffer.</param>
    /// <param name="compositionBuffer">A buffer for glyph IDs during insertion.</param>
    /// <returns>The updated index after insertion.</returns>
    private int InsertDottedCircle(ShapingBuffer buffer, ref GlyphShapingData data, int index, Span<ushort> compositionBuffer)
    {
        bool after = false;
        FontMetrics fontMetrics = this.fontMetrics;

        if (fontMetrics.TryGetGlyphId(new CodePoint(DottedCircle), out ushort id))
        {
            TextRun textRun = buffer.TextRuns[data.TextRunIndex];
            TextAttributes textAttributes = textRun.TextAttributes;
            TextDecorations textDecorations = textRun.TextDecorations;
            LayoutMode layoutMode = buffer.TextOptions.LayoutMode;
            ColorFontSupport colorFontSupport = textRun.ColorFontSupport ?? buffer.TextOptions.ColorFontSupport;
            FontPalette? fontPalette = textRun.FontPalette ?? buffer.TextOptions.FontPalette;
            if (fontMetrics.TryGetGlyphMetrics(data.CodePoint, textAttributes, textDecorations, layoutMode, colorFontSupport, fontPalette, out FontGlyphMetrics? metrics)
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

            buffer.Replace(index, glyphs, KnownFeatureTags.GlyphCompositionDecomposition);
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
