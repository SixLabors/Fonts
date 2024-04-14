// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

internal static class ShaperFactory
{
    /// <summary>
    /// Creates a Shaper based on the given script language.
    /// </summary>
    /// <param name="script">The script language.</param>
    /// <param name="unicodeScriptTag">The unicode script tag found in the font matching the script.</param>
    /// <param name="textOptions">The text options.</param>
    /// <returns>A shaper for the given script.</returns>
    public static BaseShaper Create(ScriptClass script, Tag unicodeScriptTag, TextOptions textOptions)
        => script switch
        {
            // Arabic
            ScriptClass.Arabic
            or ScriptClass.Mongolian
            or ScriptClass.Syriac
            or ScriptClass.Nko
            or ScriptClass.PhagsPa
            or ScriptClass.Mandaic
            or ScriptClass.Manichaean
            or ScriptClass.PsalterPahlavi => new ArabicShaper(script, textOptions),

            // Hangul
            ScriptClass.Hangul => new HangulShaper(script, textOptions),

            // Indic
            ScriptClass.Bengali
            or ScriptClass.Devanagari
            or ScriptClass.Gujarati
            or ScriptClass.Gurmukhi
            or ScriptClass.Kannada
            or ScriptClass.Malayalam
            or ScriptClass.Oriya
            or ScriptClass.Tamil
            or ScriptClass.Telugu
            or ScriptClass.Khmer => new IndicShaper(script, unicodeScriptTag, textOptions),

            // Universal
            ScriptClass.Balinese
            or ScriptClass.Batak
            or ScriptClass.Brahmi
            or ScriptClass.Buginese
            or ScriptClass.Buhid
            or ScriptClass.Chakma
            or ScriptClass.Cham
            or ScriptClass.Duployan
            or ScriptClass.EgyptianHieroglyphs
            or ScriptClass.Grantha
            or ScriptClass.Hanunoo
            or ScriptClass.Javanese
            or ScriptClass.Kaithi
            or ScriptClass.KayahLi
            or ScriptClass.Kharoshthi
            or ScriptClass.Khojki
            or ScriptClass.Khudawadi
            or ScriptClass.Lepcha
            or ScriptClass.Limbu
            or ScriptClass.Mahajani
            or ScriptClass.MeeteiMayek
            or ScriptClass.Modi
            or ScriptClass.PahawhHmong
            or ScriptClass.Rejang
            or ScriptClass.Saurashtra
            or ScriptClass.Sharada
            or ScriptClass.Siddham
            or ScriptClass.Sinhala
            or ScriptClass.Sundanese
            or ScriptClass.SylotiNagri
            or ScriptClass.Tagalog
            or ScriptClass.Tagbanwa
            or ScriptClass.TaiLe
            or ScriptClass.TaiTham
            or ScriptClass.TaiViet
            or ScriptClass.Takri
            or ScriptClass.Tibetan
            or ScriptClass.Tifinagh
            or ScriptClass.Tirhuta
            => new UniversalShaper(script, textOptions),
            _ => textOptions.DoShaping ? new DefaultShaper(script, textOptions) : new NoShaper(),
        };
}
