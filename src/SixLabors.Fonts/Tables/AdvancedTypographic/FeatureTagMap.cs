// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.AdvancedTypographic
{
    /// <summary>
    /// Provides a map from feature tag <see cref="FeatureTag"/> to <see cref="Tag"/>.
    /// <see href="https://docs.microsoft.com/en-us/typography/opentype/otspec183/features_ae"/>
    /// </summary>
    internal sealed class FeatureTagMap : Dictionary<FeatureTag, Tag>
    {
        private static readonly Lazy<FeatureTagMap> Lazy = new(() => CreateMap());

        /// <summary>
        /// Prevents a default instance of the <see cref="FeatureTagMap"/> class from being created.
        /// </summary>
        private FeatureTagMap()
        {
        }

        public static FeatureTagMap Instance => Lazy.Value;

        private static FeatureTagMap CreateMap()
            => new()
            {
                { FeatureTag.AccessAllAlternates, Tag.Parse("aalt") },
                { FeatureTag.AboveBaseForms, Tag.Parse("abvf") },
                { FeatureTag.AboveBaseMarkPositioning, Tag.Parse("abvm") },
                { FeatureTag.AboveBaseSubstitutions, Tag.Parse("abvs") },
                { FeatureTag.AlternativeFractions, Tag.Parse("afrc") },
                { FeatureTag.Akhand, Tag.Parse("akhn") },
                { FeatureTag.BelowBaseForms, Tag.Parse("blwf") },
                { FeatureTag.BelowBaseMarkPositioning, Tag.Parse("blwm") },
                { FeatureTag.BelowBaseSubstitutions, Tag.Parse("blws") },
                { FeatureTag.ContextualAlternates, Tag.Parse("calt") },
                { FeatureTag.CaseSensitiveForms, Tag.Parse("case") },
                { FeatureTag.GlyphCompositionDecomposition, Tag.Parse("ccmp") },
                { FeatureTag.ConjunctFormAfterRo, Tag.Parse("cfar") },
                { FeatureTag.ConjunctForms, Tag.Parse("cjct") },
                { FeatureTag.ContextualLigatures, Tag.Parse("clig") },
                { FeatureTag.CenteredCjkPunctuation, Tag.Parse("cpct") },
                { FeatureTag.CapitalSpacing, Tag.Parse("cpsp") },
                { FeatureTag.ContextualSwash, Tag.Parse("cswh") },
                { FeatureTag.CursivePositioning, Tag.Parse("curs") },
                { FeatureTag.PetiteCapitalsFromCapitals, Tag.Parse("c2pc") },
                { FeatureTag.SmallCapitalsFromCapitals, Tag.Parse("c2sc") },
                { FeatureTag.Distances, Tag.Parse("dist") },
                { FeatureTag.DiscretionaryLigatures, Tag.Parse("dlig") },
                { FeatureTag.Denominators, Tag.Parse("dnom") },
                { FeatureTag.DotlessForms, Tag.Parse("dtls") },
                { FeatureTag.ExpertForms, Tag.Parse("expt") },
                { FeatureTag.FinalGlyphOnLineAlternates, Tag.Parse("falt") },
                { FeatureTag.TerminalForm2, Tag.Parse("fin2") },
                { FeatureTag.TerminalForm3, Tag.Parse("fin3") },
                { FeatureTag.TerminalForms, Tag.Parse("fina") },
                { FeatureTag.FlattenedAscentForms, Tag.Parse("flac") },
                { FeatureTag.Fractions, Tag.Parse("frac") },
                { FeatureTag.FullWidths, Tag.Parse("fwid") },
                { FeatureTag.HalfForms, Tag.Parse("half") },
                { FeatureTag.HalantForms, Tag.Parse("haln") },
                { FeatureTag.AlternateHalfWidths, Tag.Parse("halt") },
                { FeatureTag.HistoricalForms, Tag.Parse("hist") },
                { FeatureTag.HorizontalKanaAlternates, Tag.Parse("hkna") },
                { FeatureTag.HistoricalLigatures, Tag.Parse("hlig") },
                { FeatureTag.Hangul, Tag.Parse("hngl") },
                { FeatureTag.HojoKanjiForms, Tag.Parse("hojo") },
                { FeatureTag.HalfWidths, Tag.Parse("hwid") },
                { FeatureTag.InitialForms, Tag.Parse("init") },
                { FeatureTag.IsolatedForms, Tag.Parse("isol") },
                { FeatureTag.Italics, Tag.Parse("ital") },
                { FeatureTag.JustificationAlternates, Tag.Parse("jalt") },
                { FeatureTag.Jis78Forms, Tag.Parse("jp78") },
                { FeatureTag.Jis83Forms, Tag.Parse("jp83") },
                { FeatureTag.Jis90Forms, Tag.Parse("jp90") },
                { FeatureTag.Jis2004, Tag.Parse("jp04") },
                { FeatureTag.Kerning, Tag.Parse("kern") },
                { FeatureTag.LeftBounds, Tag.Parse("lfbd") },
                { FeatureTag.Ligatures, Tag.Parse("liga") },
                { FeatureTag.LeadingJamoForms, Tag.Parse("ljmo") },
                { FeatureTag.LiningFigures, Tag.Parse("lnum") },
                { FeatureTag.LocalizedForms, Tag.Parse("locl") },
                { FeatureTag.LeftToRightGlyphAlternates, Tag.Parse("ltra") },
                { FeatureTag.LeftToRightMirroredForms, Tag.Parse("ltrm") },
                { FeatureTag.MarkPositioning, Tag.Parse("mark") },
                { FeatureTag.MedialForms2, Tag.Parse("med2") },
                { FeatureTag.MedialForms, Tag.Parse("medi") },
                { FeatureTag.MathematicalGreek, Tag.Parse("mgrk") },
                { FeatureTag.MarkToMarkPositioning, Tag.Parse("mkmk") },
                { FeatureTag.Mset, Tag.Parse("mset") },
                { FeatureTag.AlternateAnnotationForms, Tag.Parse("nalt") },
                { FeatureTag.NlcKanjiForms, Tag.Parse("nlck") },
                { FeatureTag.NuktaForms, Tag.Parse("nukt") },
                { FeatureTag.Numerators, Tag.Parse("numr") },
                { FeatureTag.OldstyleFigures, Tag.Parse("onum") },
                { FeatureTag.OpticalBounds, Tag.Parse("opbd") },
                { FeatureTag.Ordinals, Tag.Parse("ordn") },
                { FeatureTag.Ornaments, Tag.Parse("ornm") },
                { FeatureTag.ProportionalAlternateWidths, Tag.Parse("palt") },
                { FeatureTag.PetiteCapitals, Tag.Parse("pcap") },
                { FeatureTag.ProportionalKana, Tag.Parse("pkna") },
                { FeatureTag.ProportionalFigures, Tag.Parse("pnum") },
                { FeatureTag.PreBaseForms, Tag.Parse("pref") },
                { FeatureTag.PreBaseSubstitutions, Tag.Parse("pres") },
                { FeatureTag.PostBaseForms, Tag.Parse("pstf") },
                { FeatureTag.PostBaseSubstitutions, Tag.Parse("psts") },
                { FeatureTag.ProportionalWidths, Tag.Parse("pwid") },
                { FeatureTag.QuarterWidths, Tag.Parse("qwid") },
                { FeatureTag.Randomize, Tag.Parse("rand") },
                { FeatureTag.RequiredContextualAlternates, Tag.Parse("rclt") },
                { FeatureTag.RequiredLigatures, Tag.Parse("rlig") },
                { FeatureTag.RakarForms, Tag.Parse("rkrf") },
                { FeatureTag.RephForm, Tag.Parse("rphf") },
                { FeatureTag.RightBounds, Tag.Parse("rtbd") },
                { FeatureTag.RightToLeftAlternates, Tag.Parse("rtla") },
                { FeatureTag.RightToLeftMirroredForms, Tag.Parse("rtlm") },
                { FeatureTag.RubyNotationForms, Tag.Parse("ruby") },
                { FeatureTag.RequiredVariationAlternates, Tag.Parse("rvrn") },
                { FeatureTag.StylisticAlternates, Tag.Parse("salt") },
                { FeatureTag.ScientificInferiors, Tag.Parse("sinf") },
                { FeatureTag.OpticalSize, Tag.Parse("size") },
                { FeatureTag.SmallCapitals, Tag.Parse("smcp") },
                { FeatureTag.SimplifiedForms, Tag.Parse("smpl") },
                { FeatureTag.MathScriptStyleAlternates, Tag.Parse("ssty") },
                { FeatureTag.StretchingGlyphDecomposition, Tag.Parse("stch") },
                { FeatureTag.Subscript, Tag.Parse("subs") },
                { FeatureTag.Superscript, Tag.Parse("sups") },
                { FeatureTag.Swash, Tag.Parse("swsh") },
                { FeatureTag.Titling, Tag.Parse("titl") },
                { FeatureTag.TrailingJamoForms, Tag.Parse("tjmo") },
                { FeatureTag.TraditionalNameForms, Tag.Parse("tnam") },
                { FeatureTag.TabularFigures, Tag.Parse("tnum") },
                { FeatureTag.TraditionalForms, Tag.Parse("trad") },
                { FeatureTag.ThirdWidths, Tag.Parse("twid") },
                { FeatureTag.Unicase, Tag.Parse("unic") },
                { FeatureTag.AlternateVerticalMetrics, Tag.Parse("valt") },
                { FeatureTag.VattuVariants, Tag.Parse("vatu") },
                { FeatureTag.VerticalAlternates, Tag.Parse("vert") },
                { FeatureTag.AlternateVerticalHalfMetrics, Tag.Parse("vhal") },
                { FeatureTag.VowelJamoForms, Tag.Parse("vjmo") },
                { FeatureTag.VerticalKanaAlternates, Tag.Parse("vkna") },
                { FeatureTag.VerticalKerning, Tag.Parse("vkrn") },
                { FeatureTag.ProportionalAlternateVerticalMetrics, Tag.Parse("vpal") },
                { FeatureTag.VerticalAlternatesAndRotation, Tag.Parse("vrt2") },
                { FeatureTag.VerticalAlternatesForRotation, Tag.Parse("vrtr") },
                { FeatureTag.SlashedZero, Tag.Parse("zero") }
            };
    }
}
