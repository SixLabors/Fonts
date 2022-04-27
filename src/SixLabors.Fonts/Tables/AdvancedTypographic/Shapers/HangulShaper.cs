// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// This is a shaper for the Hangul script, used by the Korean language.
    /// The shaping state machine was ported from fontkit.
    /// <see href="https://github.com/foliojs/fontkit/blob/master/src/opentype/shapers/HangulShaper.js"/>
    /// </summary>
    internal sealed class HangulShaper : DefaultShaper
    {
        private static readonly Tag LjmoTag = Tag.Parse("ljmo");

        private static readonly Tag VjmoTag = Tag.Parse("vjmo");

        private static readonly Tag TjmoTag = Tag.Parse("tjmo");

        private const int HANGUL_BASE = 0xac00;
        private const int HANGUL_END = 0xd7a4;
        private const int HANGUL_COUNT = HANGUL_END - HANGUL_BASE + 1;
        private const int L_BASE = 0x1100; // lead
        private const int V_BASE = 0x1161; // vowel
        private const int T_BASE = 0x11a7; // trail
        private const int L_COUNT = 19;
        private const int V_COUNT = 21;
        private const int T_COUNT = 28;
        private const int L_END = L_BASE + L_COUNT - 1;
        private const int V_END = V_BASE + V_COUNT - 1;
        private const int T_END = T_BASE + T_COUNT - 1;
        private const int DOTTED_CIRCLE = 0x25cc;

        // Character categories
        private const byte X = 0; // Other character
        private const byte L = 1; // Leading consonant
        private const byte V = 2; // Medial vowel
        private const byte T = 3; // Trailing consonant
        private const byte LV = 4; // Composed <LV> syllable
        private const byte LVT = 5; // Composed <LVT> syllable
        private const byte M = 6; // Tone mark

        // State machine actions
        private const byte None = 0;
        private const byte Decompose = 1;
        private const byte Compose = 2;
        private const byte ToneMark = 4;
        private const byte Invalid = 5;

        // Build a state machine that accepts valid syllables, and applies actions along the way.
        // The logic this is implementing is documented at the top of the file.
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

        public HangulShaper(TextOptions textOptions)
            : base(MarkZeroingMode.None, textOptions)
        {
        }

        public override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            this.AddFeature(collection, index, count, LjmoTag, false);
            this.AddFeature(collection, index, count, VjmoTag, false);
            this.AddFeature(collection, index, count, TjmoTag, false);

            base.AssignFeatures(collection, index, count);

            // Apply the state machine to map glyphs to features.
            if (collection is GlyphSubstitutionCollection substitutionCollection)
            {
                // GSub
                int state = 0;
                for (int i = 0; i < count; i++)
                {
                    GlyphShapingData data = substitutionCollection.GetGlyphShapingData(i + index);
                    CodePoint codePoint = data.CodePoint;
                    int type = GetSyllableType(codePoint);
                    byte[] actionsWithState = StateTable[state, type];
                    byte action = actionsWithState[0];
                    state = actionsWithState[1];
                    switch (action)
                    {
                        case Decompose:

                            // Decompose the composed syllable if it is not supported by the font.
                            if (!data.TextRun.Font!.FontMetrics.TryGetGlyphId(codePoint, out ushort _))
                            {
                                i = this.DecomposeGlyph(substitutionCollection, data, i);
                            }

                            break;

                        case Compose:

                            // Found a decomposed syllable. Try to compose if supported by the font.
                            i = this.ComposeGlyph(substitutionCollection, data, i, type);
                            break;

                        case ToneMark:

                            // Got a valid syllable, followed by a tone mark. Move the tone mark to the beginning of the syllable.
                            this.ReOrderToneMark(substitutionCollection, data, i);
                            break;

                        case Invalid:
                            // Tone mark has no valid syllable to attach to, so insert a dotted circle
                            i = this.InsertDottedCircle(data, i);
                            break;
                    }
                }
            }
            else
            {
                // GPos
                // Simply loop and enable based on type.
                // Glyph substitution has handled [de]composition.
            }
        }

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

        private static int GetSyllableLength(CodePoint codePoint)
            => GetSyllableType(codePoint) switch
            {
                LV or LVT => 1,
                V => 2,
                T => 3,
                _ => 0,
            };

        private int DecomposeGlyph(GlyphSubstitutionCollection collection, GlyphShapingData data, int index)
        {
            // Decompose the syllable into a sequence of glyphs.
            int s = data.CodePoint.Value - HANGUL_BASE;
            int t = T_BASE + (s % T_COUNT);
            s = (s / T_COUNT) | 0;
            int l = (L_BASE + (s / V_COUNT)) | 0;
            int v = V_BASE + (s % V_COUNT);

            FontMetrics metrics = data.TextRun.Font!.FontMetrics;

            // Don't decompose if all of the components are not available
            if (!metrics.TryGetGlyphId(new(l), out ushort ljmo) ||
                !metrics.TryGetGlyphId(new(v), out ushort vjmo) ||
                (!metrics.TryGetGlyphId(new(t), out ushort tjmo) && t != T_BASE))
            {
                return index;
            }

            // TODO: Check the insertion here.
            // We likely need to add the features separately to each of the newly
            // embedded glyph ids.
            //
            // Replace the current glyph with decomposed L, V, and T glyphs,
            // and apply the proper OpenType features to each component.
            collection.EnableShapingFeature(index, LjmoTag);
            collection.EnableShapingFeature(index, VjmoTag);

            if (t <= T_BASE)
            {
                Span<ushort> ii = stackalloc ushort[2];
                ii[0] = ljmo;
                ii[1] = vjmo;

                collection.Replace(index, ii);
                return index;
            }

            collection.EnableShapingFeature(index, TjmoTag);
            Span<ushort> iii = stackalloc ushort[3];
            iii[0] = ljmo;
            iii[1] = vjmo;
            iii[2] = tjmo;

            collection.Replace(index, iii);
            return index;
        }

        private int ComposeGlyph(GlyphSubstitutionCollection collection, GlyphShapingData data, int index, int type)
        {
            if (index == 0)
            {
                return index;
            }

            GlyphShapingData prev = collection.GetGlyphShapingData(index - 1);
            CodePoint prevCodePoint = prev.CodePoint;
            int prevType = GetSyllableType(prevCodePoint);

            // Figure out what type of syllable we're dealing with
            CodePoint lv = default;
            GlyphShapingData? ljmo = null, vjmo = null, tjmo = null;

            if (prevType == LV && type == T)
            {
                // <LV,T>
                lv = prevCodePoint;
                tjmo = data;
            }
            else
            {
                if (type == V)
                {
                    // <L,V>
                    ljmo = prev;
                    vjmo = data;
                }
                else
                {
                    // <L,V,T>
                    ljmo = collection.GetGlyphShapingData(index - 2);
                    vjmo = prev;
                    tjmo = data;
                }

                CodePoint l = ljmo.CodePoint;
                CodePoint v = vjmo.CodePoint;

                // Make sure L and V are combining characters
                if (isCombiningL(l) && isCombiningV(v))
                {
                    lv = new CodePoint(HANGUL_BASE + ((((l.Value - L_BASE) * V_COUNT) + (v.Value - V_BASE)) * T_COUNT));
                }
            }

            CodePoint t = tjmo?.CodePoint ?? new CodePoint(T_BASE);
            if ((lv != default) && (t.Value == T_BASE || isCombiningT(t)))
            {
                CodePoint s = new(lv.Value + (t.Value - T_BASE));

                // Replace with a composed glyph if supported by the font,
                // otherwise apply the proper OpenType features to each component.
                FontMetrics metrics = data.TextRun.Font!.FontMetrics;
                if (metrics.TryGetGlyphId(s, out ushort id))
                {
                    int del = prevType == V ? 3 : 2;
                    int idx = index - del + 1;
                    collection.Replace(idx, del, id);
                    return idx;
                }
            }

            // Didn't compose (either a non-combining component or unsupported by font).
            if (ljmo != null)
            {
                collection.EnableShapingFeature(ljmo.GlyphIds[0], LjmoTag);
            }

            if (vjmo != null)
            {
                collection.EnableShapingFeature(vjmo.GlyphIds[0], VjmoTag);
            }

            if (tjmo != null)
            {
                collection.EnableShapingFeature(tjmo.GlyphIds[0], TjmoTag);
            }

            if (prevType == LV)
            {
                // Sequence was originally <L,V>, which got combined earlier.
                // Either the T was non-combining, or the LVT glyph wasn't supported.
                // Decompose the glyph again and apply OT features.
                data = collection.GetGlyphShapingData(index - 1);
                this.DecomposeGlyph(collection, data, index - 1);
            }

            return index;
        }

        private int ReOrderToneMark(GlyphSubstitutionCollection collection, GlyphShapingData data, int index)
        {
            if (index == 0)
            {
                return index;
            }

            // Move tone mark to the beginning of the previous syllable, unless it is zero width
            // We don't have access to the glyphs metrics as an array when substituting so we have to loop.
            FontMetrics metrics = data.TextRun.Font!.FontMetrics;
            foreach (GlyphMetrics gm in metrics.GetGlyphMetrics(data.CodePoint, collection.TextOptions.ColorFontSupport))
            {
                if (gm.Width == 0)
                {
                    return index;
                }
            }

            GlyphShapingData prev = collection.GetGlyphShapingData(index - 1);
            int len = GetSyllableLength(prev.CodePoint);
            collection.MoveGlyph(index, index - len);
            throw new NotImplementedException();
        }

        private int InsertDottedCircle(GlyphShapingData data, int i)
        {
            // TODO: Implement.
            // We need to find a way to do this that doesn't require increasing the length of the collection.
            return i;
        }

        private static bool isCombiningL(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, L_BASE, L_END);

        private static bool isCombiningV(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, V_BASE, V_END);

        private static bool isCombiningT(CodePoint code) => UnicodeUtility.IsInRangeInclusive((uint)code.Value, T_BASE + 1, T_END);
    }
}
