// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Factory for creating the appropriate script shaper based on a given script classification.
/// </summary>
internal static class ShaperFactory
{
    /// <summary>The 'mym2' (Myanmar v2) script tag used to distinguish Myanmar shaper versions.</summary>
    private static readonly Tag Mym2Tag = Tag.Parse("mym2");

    /// <summary>The 'latn' (Latin) script tag, which a font can carry in place of the one asked for.</summary>
    private static readonly Tag LatnTag = Tag.Parse("latn");

    /// <summary>
    /// The last character of a script tag naming the third revision of its
    /// script, the revision the universal engine describes.
    /// </summary>
    private const uint ThirdRevisionTagSuffix = (byte)'3';

    /// <summary>
    /// Determines whether the font offers nothing designed for the script, so
    /// that the tag settled on is the default one or a Latin one.
    /// </summary>
    /// <param name="unicodeScriptTag">The script tag found in the font.</param>
    /// <returns><see langword="true"/> when the font was not designed for the script.</returns>
    private static bool IsDefaultDesign(Tag unicodeScriptTag)
        => unicodeScriptTag == default || unicodeScriptTag == LatnTag;

    /// <summary>
    /// Determines whether the tag names the third revision of its script.
    /// </summary>
    /// <param name="unicodeScriptTag">The script tag found in the font.</param>
    /// <returns><see langword="true"/> when the tag names the third revision.</returns>
    private static bool IsThirdRevision(Tag unicodeScriptTag)
        => (unicodeScriptTag.Value & 0xFF) == ThirdRevisionTagSuffix;

    /// <summary>
    /// Creates the shaper for a run. AAT supplies script substitutions, so
    /// specialized scripts use default preparation without mark zeroing.
    /// </summary>
    /// <param name="script">The Unicode script of the run.</param>
    /// <param name="unicodeScriptTag">The OpenType script tag selected for the run.</param>
    /// <param name="fontMetrics">The font metrics for the run.</param>
    /// <param name="textOptions">The text options for the run.</param>
    /// <param name="languageTags">The candidate OpenType language system tags.</param>
    /// <param name="useMetamorphosis">Whether the run uses AAT substitution.</param>
    /// <returns>The shaper for the run.</returns>
    public static BaseShaper Create(
        ScriptClass script,
        Tag unicodeScriptTag,
        FontMetrics fontMetrics,
        TextOptions textOptions,
        Tag[] languageTags,
        bool useMetamorphosis)
        => script switch
        {
            // Arabic and Syriac
            ScriptClass.Arabic
            or ScriptClass.Syriac

            // Arabic keeps its script shaper when the font has no matching script
            // system because Arabic alone has presentation-form fallback shaping.
            // Syriac requires a non-default script system selected from the font.
            //
            // The script shaper assigns joining forms along the horizontal inline
            // axis. Forced vertical layout keeps glyphs upright and stacks them, so
            // generic shaping applies common and vertical features without contextual
            // joining. Mixed vertical layout rotates Arabic and Syriac glyphs, keeping
            // their horizontal RTL inline direction and script-specific joining.
            => (unicodeScriptTag != default || script == ScriptClass.Arabic)
            && !textOptions.LayoutMode.IsVertical()
            ? useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, true)
                : new ArabicShaper(script, textOptions, fontMetrics, languageTags)
            : new DefaultShaper(script, textOptions),

            // Hebrew
            ScriptClass.Hebrew => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, true)
                : new HebrewShaper(script, textOptions, fontMetrics, unicodeScriptTag != default),

            // Thai / Lao
            ScriptClass.Thai
            or ScriptClass.Lao => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new ThaiShaper(script, textOptions, fontMetrics, unicodeScriptTag != default),

            // Hangul
            ScriptClass.Hangul => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, true)
                : new HangulShaper(script, textOptions, fontMetrics),

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

            // A font whose only script is the default one, or the Latin one we
            // would otherwise pick arbitrarily, was not designed for the script,
            // so it is shaped plainly. A font designed to the third revision of
            // the script is shaped by the universal engine, which is what that
            // revision describes.
            => IsDefaultDesign(unicodeScriptTag)
            ? new DefaultShaper(script, textOptions)
            : IsThirdRevision(unicodeScriptTag)
            ? useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new UniversalShaper(script, textOptions, fontMetrics)
            : (useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new IndicShaper(script, unicodeScriptTag, textOptions, fontMetrics)),

            ScriptClass.Khmer => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new KhmerShaper(script, textOptions, fontMetrics),

            // Myanmar
            ScriptClass.Myanmar

            // If the designer designed the font for the 'DFLT' script,
            // (or we ended up arbitrarily pick 'latn'), use the default shaper.
            // Otherwise, use the specific shaper.
            //
            // If designer designed for 'mymr' tag, also send to default
            // shaper.  That's tag used from before Myanmar shaping spec
            // was developed.  The shaping spec uses 'mym2' tag.
            => unicodeScriptTag == Mym2Tag
            ? useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new MyanmarShaper(script, textOptions, fontMetrics)
            : new DefaultShaper(script, textOptions),

            ScriptClass.MyanmarZawgyi => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new MyanmarZawgyiShaper(script, textOptions),

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
            or ScriptClass.Kawi
            or ScriptClass.NagMundari
            or ScriptClass.Garay
            or ScriptClass.GurungKhema
            or ScriptClass.KiratRai
            or ScriptClass.OlOnal
            or ScriptClass.Sunuwar
            or ScriptClass.Todhri
            or ScriptClass.TuluTigalari
            or ScriptClass.BeriaErfe
            or ScriptClass.Sidetic
            or ScriptClass.TaiYo
            or ScriptClass.TolongSiki
            or ScriptClass.Ahom
            or ScriptClass.Multani
            or ScriptClass.Miao
            or ScriptClass.Adlam
            or ScriptClass.Bhaiksuki
            or ScriptClass.Marchen
            or ScriptClass.Newa
            or ScriptClass.MasaramGondi
            or ScriptClass.Soyombo
            or ScriptClass.ZanabazarSquare
            or ScriptClass.Dogra
            or ScriptClass.GunjalaGondi
            or ScriptClass.HanifiRohingya
            or ScriptClass.Makasar
            or ScriptClass.Medefaidrin
            or ScriptClass.OldSogdian
            or ScriptClass.Sogdian
            or ScriptClass.Elymaic
            or ScriptClass.Nandinagari
            or ScriptClass.NyiakengPuachueHmong
            or ScriptClass.Wancho
            or ScriptClass.Chorasmian
            or ScriptClass.DivesAkuru
            or ScriptClass.KhitanSmallScript
            or ScriptClass.Yezidi
            or ScriptClass.CyproMinoan
            or ScriptClass.OldUyghur
            or ScriptClass.Tangsa
            or ScriptClass.Toto
            or ScriptClass.Vithkuqi
            or ScriptClass.Mongolian
            or ScriptClass.Nko
            or ScriptClass.PhagsPa
            or ScriptClass.Mandaic
            or ScriptClass.Manichaean
            or ScriptClass.PsalterPahlavi

            // Universal syllable analysis is selected from the Unicode script,
            // independently of which layout tables the font provides. A font with
            // no script system still needs broken-syllable repair and reordering.
            => useMetamorphosis
                ? DefaultShaper.CreateForMetamorphosis(script, textOptions, false)
                : new UniversalShaper(script, textOptions, fontMetrics),
            _ => new DefaultShaper(script, textOptions),
        };
}
