// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Globalization;
using SixLabors.Fonts.Unicode;
using SixLabors.Fonts.Unicode.Resources;
using UnicodeTrieGenerator.StateAutomation;
using static SixLabors.Fonts.Unicode.Resources.IndicShapingData;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers
{
    /// <summary>
    /// The IndicShaper supports Indic scripts e.g. Devanagari, Kannada, etc.
    /// </summary>
    internal sealed class IndicShaper : DefaultShaper
    {
        private static readonly StateMachine StateMachine =
            new(StateTable, AcceptingStates, Tags);

        private static readonly Tag RphfTag = Tag.Parse("rphf");
        private static readonly Tag NuktTag = Tag.Parse("nukt");
        private static readonly Tag AkhnTag = Tag.Parse("akhn");
        private static readonly Tag PrefTag = Tag.Parse("pref");

        private static readonly Tag RkrfTag = Tag.Parse("rkrf");
        private static readonly Tag AbvfTag = Tag.Parse("abvf");
        private static readonly Tag BlwfTag = Tag.Parse("blwf");
        private static readonly Tag HalfTag = Tag.Parse("half");
        private static readonly Tag PstfTag = Tag.Parse("pstf");
        private static readonly Tag VatuTag = Tag.Parse("vatu");
        private static readonly Tag CjctTag = Tag.Parse("cjct");
        private static readonly Tag CfarTag = Tag.Parse("cfar");

        private static readonly Tag InitTag = Tag.Parse("init");
        private static readonly Tag AbvsTag = Tag.Parse("abvs");
        private static readonly Tag BlwsTag = Tag.Parse("blws");
        private static readonly Tag PresTag = Tag.Parse("pres");
        private static readonly Tag PstsTag = Tag.Parse("psts");
        private static readonly Tag HalnTag = Tag.Parse("haln");
        private static readonly Tag DistTag = Tag.Parse("dist");
        private static readonly Tag AbvmTag = Tag.Parse("abvm");
        private static readonly Tag BlwmTag = Tag.Parse("blwm");

        private const int DottedCircle = 0x25cc;

        private readonly TextOptions textOptions;
        private ShapingConfiguration indicConfiguration;
        private readonly bool isOldSpec;

        public IndicShaper(ScriptClass script, Tag unicodeScriptTag, TextOptions textOptions)
            : base(script, MarkZeroingMode.None, textOptions)
        {
            this.textOptions = textOptions;

            if (IndicConfigurations.ContainsKey(script))
            {
                this.indicConfiguration = IndicConfigurations[script];
            }
            else
            {
                this.indicConfiguration = ShapingConfiguration.Default;
            }

            this.isOldSpec = this.indicConfiguration.HasOldSpec && !unicodeScriptTag.ToString().EndsWith("2");
        }

        protected override void PlanFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            this.AddFeature(collection, index, count, LoclTag, preAction: SetupSyllables);
            this.AddFeature(collection, index, count, CcmpTag);

            this.AddFeature(collection, index, count, NuktTag, preAction: this.InitialReorder);
            this.AddFeature(collection, index, count, AkhnTag);

            this.AddFeature(collection, index, count, RphfTag, false);
            this.AddFeature(collection, index, count, RkrfTag);
            this.AddFeature(collection, index, count, PrefTag, false);
            this.AddFeature(collection, index, count, BlwfTag, false);
            this.AddFeature(collection, index, count, AbvfTag, false);
            this.AddFeature(collection, index, count, HalfTag, false);
            this.AddFeature(collection, index, count, PstfTag, false);
            this.AddFeature(collection, index, count, VatuTag);
            this.AddFeature(collection, index, count, CjctTag);
            this.AddFeature(collection, index, count, CfarTag, false, postAction: this.FinalReorder);

            this.AddFeature(collection, index, count, InitTag, false);
            this.AddFeature(collection, index, count, PresTag);
            this.AddFeature(collection, index, count, AbvsTag);
            this.AddFeature(collection, index, count, BlwsTag);
            this.AddFeature(collection, index, count, PstsTag);
            this.AddFeature(collection, index, count, HalnTag);
            this.AddFeature(collection, index, count, DistTag);
            this.AddFeature(collection, index, count, AbvmTag);
            this.AddFeature(collection, index, count, BlwmTag);
        }

        protected override void AssignFeatures(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            // Decompose split matras
            Span<ushort> buffer = stackalloc ushort[16];
            int end = index + count;
            for (int i = end - 1; i >= 0; i--)
            {
                GlyphShapingData data = substitutionCollection[i];
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;

                if ((Decompositions.TryGetValue(data.CodePoint.Value, out int[]? decompositions) ||
                    UniversalShapingData.Decompositions.TryGetValue(data.CodePoint.Value, out decompositions)) &&
                    decompositions != null)
                {
                    Span<ushort> ids = buffer.Slice(0, decompositions.Length);
                    for (int j = 0; j < decompositions.Length; j++)
                    {
                        // Font should always contain the decomposed glyph.
                        fontMetrics.TryGetGlyphId(new CodePoint(decompositions[j]), out ushort id);

                        ids[j] = id;
                    }

                    substitutionCollection.Replace(i, ids);
                }
            }
        }

        private static void SetupSyllables(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            Span<int> values = count <= 64 ? stackalloc int[count] : new int[count];

            for (int i = index; i < index + count; i++)
            {
                CodePoint codePoint = substitutionCollection[i].CodePoint;
                values[i - index] = IndicShapingCategory(codePoint);
            }

            int syllable = 0;
            int last = 0;
            foreach (StateMatch match in StateMachine.Match(values))
            {
                if (match.StartIndex > last)
                {
                    ++syllable;
                    for (int i = last; i < match.StartIndex; i++)
                    {
                        GlyphShapingData data = substitutionCollection[i + index];
                        data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
                    }
                }

                ++syllable;

                // Create shaper info.
                for (int i = match.StartIndex; i <= match.EndIndex; i++)
                {
                    GlyphShapingData data = substitutionCollection[i + index];
                    CodePoint codePoint = data.CodePoint;

                    data.IndicShapingEngineInfo = new(
                        (Categories)(1 << IndicShapingCategory(codePoint)),
                        (Positions)IndicShapingPosition(codePoint),
                        match.Tags[0],
                        syllable);
                }

                last = match.EndIndex + 1;
            }

            if (last < count)
            {
                ++syllable;
                for (int i = last; i < count; i++)
                {
                    GlyphShapingData data = substitutionCollection[i + index];
                    data.IndicShapingEngineInfo = new(Categories.X, Positions.End, "non_indic_cluster", syllable);
                }
            }
        }

        private static int IndicShapingCategory(CodePoint codePoint)
            => UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) >> 8;

        private static int IndicShapingPosition(CodePoint codePoint)
            => 1 << (UnicodeData.GetIndicShapingProperties((uint)codePoint.Value) & 0xFF);

        private void InitialReorder(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            // Create a reusable temporary substitution collection and buffer to allow checking whether
            // certain combinations will be substituted.
            GlyphSubstitutionCollection tempCollection = new(this.textOptions);
            Span<GlyphShapingData> tempBuffer = new GlyphShapingData[3];

            ShapingConfiguration indicConfiguration = this.indicConfiguration;
            for (int i = 0; i < count; i++)
            {
                GlyphShapingData data = substitutionCollection[i + index];
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;

                fontMetrics.TryGetGlyphId(new(0x0020), out ushort spc);

                IndicShapingEngineInfo? info = data.IndicShapingEngineInfo;
                if (info?.Position == Positions.Base_C)
                {
                    CodePoint cp = new(indicConfiguration.Virama);
                    if (fontMetrics.TryGetGlyphId(cp, out ushort id))
                    {
                        GlyphShapingData virama = new(data, false)
                        {
                            GlyphId = id,
                            CodePoint = cp
                        };

                        tempBuffer[2] = virama;
                        tempBuffer[1] = data;
                        tempBuffer[0] = virama;

                        info.Position = this.ConsonantPosition(tempCollection, tempBuffer);
                    }
                }
            }

            int max = index + count;
            int start = index;
            int end = NextSyllable(substitutionCollection, index, max);
            while (start < max)
            {
                GlyphShapingData data = substitutionCollection[start];
                IndicShapingEngineInfo? dataInfo = data.IndicShapingEngineInfo;
                string? type = dataInfo?.SyllableType;

                if (type is "symbol_cluster" or "non_indic_cluster")
                {
                    goto Increment;
                }

                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                if (!fontMetrics.TryGetGSubTable(out GSubTable? gSubTable))
                {
                    break;
                }

                if (dataInfo != null && type == "broken_cluster" && fontMetrics.TryGetGlyphId(new(DottedCircle), out ushort id))
                {
                    // Insert after possible Repha.
                    int i = start;
                    GlyphShapingData current = substitutionCollection[i];
                    for (i = start; i < end; i++)
                    {
                        if (current.IndicShapingEngineInfo?.Category != Categories.Repha)
                        {
                            break;
                        }

                        current = substitutionCollection[i];
                    }

                    Span<ushort> glyphs = stackalloc ushort[2];
                    glyphs[0] = current.GlyphId;
                    glyphs[1] = id;

                    substitutionCollection.Replace(i, glyphs);

                    // Update shaping info for newly inserted data.
                    GlyphShapingData dotted = substitutionCollection[i + 1];
                    var dottedCategory = (Categories)(1 << IndicShapingCategory(dotted.CodePoint));
                    var dottedPosition = (Positions)IndicShapingPosition(dotted.CodePoint);
                    dotted.IndicShapingEngineInfo = new(dottedCategory, dottedPosition, dataInfo.SyllableType, dataInfo.Syllable);

                    end++;
                    max++;
                }

                // 1. Find base consonant:
                //
                // The shaping engine finds the base consonant of the syllable, using the
                // following algorithm: starting from the end of the syllable, move backwards
                // until a consonant is found that does not have a below-base or post-base
                // form (post-base forms have to follow below-base forms), or that is not a
                // pre-base reordering Ra, or arrive at the first consonant. The consonant
                // stopped at will be the base.
                int basePosition = end;
                int limit = start;
                bool hasReph = false;

                // If the syllable starts with Ra + Halant (in a script that has Reph)
                // and has more than one consonant, Ra is excluded from candidates for
                // base consonants.
                if (start + 3 <= end &&
                    indicConfiguration.RephPosition != Positions.Ra_To_Become_Reph &&
                    gSubTable.TryGetFeatureLookups(in RphfTag, this.ScriptClass, out _) &&
                    ((indicConfiguration.RephMode == RephMode.Implicit && !IsJoiner(substitutionCollection[start + 2])) ||
                     (indicConfiguration.RephMode == RephMode.Explicit && substitutionCollection[start + 2].IndicShapingEngineInfo?.Category == Categories.ZWJ)))
                {
                    // See if it matches the 'rphf' feature.
                    tempBuffer[2] = substitutionCollection[start + 2];
                    tempBuffer[1] = substitutionCollection[start + 1];
                    tempBuffer[0] = substitutionCollection[start];

                    if ((indicConfiguration.RephMode == RephMode.Explicit && this.WouldSubstitute(tempCollection, in RphfTag, tempBuffer)) ||
                        this.WouldSubstitute(tempCollection, in RphfTag, tempBuffer.Slice(0, 2)))
                    {
                        limit += 2;
                        while (limit < end && IsJoiner(substitutionCollection[limit]))
                        {
                            limit++;
                        }

                        basePosition = start;
                        hasReph = true;
                    }
                }
                else if (indicConfiguration.RephMode == RephMode.Log_Repha &&
                         substitutionCollection[start].IndicShapingEngineInfo?.Category == Categories.Repha)
                {
                    limit++;
                    while (limit < end && IsJoiner(substitutionCollection[limit]))
                    {
                        limit++;
                    }

                    basePosition = start;
                    hasReph = true;
                }

                switch (indicConfiguration.BasePosition)
                {
                    case BasePosition.Last:
                    {
                        // Starting from the end of the syllable, move backwards
                        int i = end;
                        bool seenBelow = false;

                        do
                        {
                            IndicShapingEngineInfo? prevInfo = substitutionCollection[--i].IndicShapingEngineInfo;

                            // Until a consonant is found
                            if (IsConsonant(substitutionCollection[i]))
                            {
                                // that does not have a below-base or post-base form
                                // (post-base forms have to follow below-base forms),
                                if (prevInfo?.Position != Positions.Below_C && (prevInfo?.Position != Positions.Post_C || seenBelow))
                                {
                                    basePosition = i;
                                    break;
                                }

                                // or that is not a pre-base reordering Ra,
                                //
                                // IMPLEMENTATION NOTES:
                                //
                                // Our pre-base reordering Ra's are marked POS_POST_C, so will be skipped
                                // by the logic above already.
                                //

                                // or arrive at the first consonant. The consonant stopped at will
                                // be the base.
                                if (prevInfo?.Position == Positions.Below_C)
                                {
                                    seenBelow = true;
                                }

                                basePosition = i;
                            }
                            else if (start < i && prevInfo?.Category == Categories.ZWJ &&
                                     substitutionCollection[i - 1].IndicShapingEngineInfo?.Category == Categories.H)
                            {
                                // A ZWJ after a Halant stops the base search, and requests an explicit
                                // half form.
                                // A ZWJ before a Halant, requests a subjoined form instead, and hence
                                // search continues.  This is particularly important for Bengali
                                // sequence Ra,H,Ya that should form Ya-Phalaa by subjoining Ya.
                                break;
                            }
                        }
                        while (i > limit);

                        break;
                    }

                    case BasePosition.First:
                    {
                        // The first consonant is always the base.
                        basePosition = start;

                        for (int i = basePosition + 1; i < end; i++)
                        {
                            GlyphShapingData c = substitutionCollection[i];
                            if (IsConsonant(c) && c.IndicShapingEngineInfo != null)
                            {
                                c.IndicShapingEngineInfo.Position = Positions.Below_C;
                            }
                        }

                        break;
                    }
                }

                // If the syllable starts with Ra + Halant (in a script that has Reph)
                // and has more than one consonant, Ra is excluded from candidates for
                // base consonants.
                //
                //  Only do this for unforced Reph. (ie. not for Ra,H,ZWJ)
                if (hasReph && basePosition == start && limit - basePosition <= 2)
                {
                    hasReph = false;
                }

                // 2. Decompose and reorder Matras:
                //
                // Each matra and any syllable modifier sign in the cluster are moved to the
                // appropriate position relative to the consonant(s) in the cluster. The
                // shaping engine decomposes two- or three-part matras into their constituent
                // parts before any repositioning. Matra characters are classified by which
                // consonant in a conjunct they have affinity for and are reordered to the
                // following positions:
                //
                //   o Before first half form in the syllable
                //   o After subjoined consonants
                //   o After post-form consonant
                //   o After main consonant (for above marks)
                //
                // IMPLEMENTATION NOTES:
                //
                // The normalize() routine has already decomposed matras for us, so we don't
                // need to worry about that.

                // 3.  Reorder marks to canonical order:
                //
                // Adjacent nukta and halant or nukta and vedic sign are always repositioned
                // if necessary, so that the nukta is first.
                //
                // IMPLEMENTATION NOTES:
                //
                // We don't need to do this: the normalize() routine already did this for us.

                // Reorder characters
                for (int i = start; i < basePosition; i++)
                {
                    IndicShapingEngineInfo? info = substitutionCollection[i].IndicShapingEngineInfo;
                    if (info != null)
                    {
                        info.Position = (Positions)Math.Min((int)Positions.Pre_C, (int)info.Position);
                    }
                }

                if (basePosition < end)
                {
                    IndicShapingEngineInfo? info = substitutionCollection[basePosition].IndicShapingEngineInfo;
                    if (info != null)
                    {
                        info.Position = Positions.Base_C;
                    }
                }

                // Mark final consonants.  A final consonant is one appearing after a matra,
                // like in Khmer.
                for (int i = basePosition + 1; i < end; i++)
                {
                    if (substitutionCollection[i].IndicShapingEngineInfo?.Category == Categories.M)
                    {
                        for (int j = i + 1; j < end; j++)
                        {
                            GlyphShapingData c = substitutionCollection[j];
                            if (IsConsonant(c) && c.IndicShapingEngineInfo != null)
                            {
                                c.IndicShapingEngineInfo.Position = Positions.Final_C;
                                break;
                            }
                        }

                        break;
                    }
                }

                // Handle beginning Ra
                if (hasReph)
                {
                    GlyphShapingData c = substitutionCollection[start];
                    if (c.IndicShapingEngineInfo != null)
                    {
                        c.IndicShapingEngineInfo.Position = Positions.Ra_To_Become_Reph;
                    }
                }

                // For old-style Indic script tags, move the first post-base Halant after
                // last consonant.
                //
                // Reports suggest that in some scripts Uniscribe does this only if there
                // is *not* a Halant after last consonant already (eg. Kannada), while it
                // does it unconditionally in other scripts (eg. Malayalam).  We don't
                // currently know about other scripts, so we single out Malayalam for now.
                //
                // Kannada test case:
                // U+0C9A,U+0CCD,U+0C9A,U+0CCD
                // With some versions of Lohit Kannada.
                // https://bugs.freedesktop.org/show_bug.cgi?id=59118
                //
                // Malayalam test case:
                // U+0D38,U+0D4D,U+0D31,U+0D4D,U+0D31,U+0D4D
                // With lohit-ttf-20121122/Lohit-Malayalam.ttf
                if (this.isOldSpec)
                {
                    bool disallowDoubleHalants = this.ScriptClass != ScriptClass.Malayalam;
                    for (int i = basePosition + 1; i < end; i++)
                    {
                        if (substitutionCollection[i].IndicShapingEngineInfo?.Category == Categories.H)
                        {
                            int j;
                            for (j = end - 1; j > i; j--)
                            {
                                GlyphShapingData c = substitutionCollection[j];
                                if (IsConsonant(c) || (disallowDoubleHalants && c.IndicShapingEngineInfo?.Category == Categories.H))
                                {
                                    break;
                                }
                            }

                            if (j > i && substitutionCollection[j].IndicShapingEngineInfo?.Category != Categories.H)
                            {
                                // Move Halant to after last consonant.
                                substitutionCollection.MoveGlyph(i, j);
                            }

                            break;
                        }
                    }
                }

                // Attach misc marks to previous char to move with them.
                Positions lastPosition = Positions.Start;
                for (int i = start; i < end; i++)
                {
                    IndicShapingEngineInfo? info = substitutionCollection[i].IndicShapingEngineInfo;
                    if (info != null)
                    {
                        if ((info.Category & (JoinerFlags | Categories.N | Categories.RS | Categories.CM | (HalantOrCoengFlags & info.Category))) != 0)
                        {
                            info.Position = lastPosition;
                            if (info.Category == Categories.H && info.Position == Positions.Pre_M)
                            {
                                // Uniscribe doesn't move the Halant with Left Matra.
                                // TEST: U+092B,U+093F,U+094DE
                                // We follow.  This is important for the Sinhala
                                // U+0DDA split matra since it decomposes to U+0DD9,U+0DCA
                                // where U+0DD9 is a left matra and U+0DCA is the virama.
                                // We don't want to move the virama with the left matra.
                                // TEST: U+0D9A,U+0DDA
                                for (int j = i; j > start; j--)
                                {
                                    Positions? pos = substitutionCollection[j - 1].IndicShapingEngineInfo?.Position;
                                    if (pos is not null and not Positions.Pre_M)
                                    {
                                        info.Position = pos.Value;
                                        break;
                                    }
                                }
                            }
                        }
                        else if (info.Position != Positions.SMVD)
                        {
                            lastPosition = info.Position;
                        }
                    }
                }

                // For post-base consonants let them own anything before them
                // since the last consonant or matra.
                int last = basePosition;
                for (int i = basePosition + 1; i < end; i++)
                {
                    GlyphShapingData current = substitutionCollection[i];
                    IndicShapingEngineInfo? info = current.IndicShapingEngineInfo;
                    if (info != null)
                    {
                        if (IsConsonant(current))
                        {
                            for (int j = last + 1; j < i; j++)
                            {
                                IndicShapingEngineInfo? jInfo = substitutionCollection[j].IndicShapingEngineInfo;
                                if (jInfo?.Position < Positions.SMVD)
                                {
                                    jInfo.Position = info.Position;
                                }
                            }

                            last = i;
                        }
                        else if (info.Category == Categories.M)
                        {
                            last = i;
                        }
                    }
                }

                substitutionCollection.Sort(start, end, (a, b) =>
                {
                    int pa = a.IndicShapingEngineInfo?.Position != null ? (int)a.IndicShapingEngineInfo.Position : 0;
                    int pb = b.IndicShapingEngineInfo?.Position != null ? (int)b.IndicShapingEngineInfo.Position : 0;
                    return pa - pb;
                });

                // Find base again
                for (int i = start; i < end; i++)
                {
                    if (substitutionCollection[i].IndicShapingEngineInfo?.Position == Positions.Base_C)
                    {
                        basePosition = i;
                        break;
                    }
                }

                // Setup features now.

                // Reph.
                for (int i = start; i < end; i++)
                {
                    IndicShapingEngineInfo? info = substitutionCollection[i].IndicShapingEngineInfo;
                    if (info?.Position != Positions.Ra_To_Become_Reph)
                    {
                        break;
                    }

                    substitutionCollection.EnableShapingFeature(i, RphfTag);
                }

                // Pre-base
                bool blwf = !this.isOldSpec && indicConfiguration.BlwfMode == BlwfMode.Pre_And_Post;
                for (int i = start; i < basePosition; i++)
                {
                    substitutionCollection.EnableShapingFeature(i, HalfTag);
                    if (blwf)
                    {
                        substitutionCollection.EnableShapingFeature(i, BlwfTag);
                    }
                }

                // Post-base
                for (int i = basePosition + 1; i < end; i++)
                {
                    substitutionCollection.EnableShapingFeature(i, AbvfTag);
                    substitutionCollection.EnableShapingFeature(i, PstfTag);
                    substitutionCollection.EnableShapingFeature(i, BlwfTag);
                }

                if (this.isOldSpec && this.ScriptClass == ScriptClass.Devanagari)
                {
                    // Old-spec eye-lash Ra needs special handling.
                    // From the spec:
                    //
                    // "The feature 'below-base form' is applied to consonants
                    // having below-base forms and following the base consonant.
                    // The exception is vattu, which may appear below half forms
                    // as well as below the base glyph. The feature 'below-base
                    // form' will be applied to all such occurrences of Ra as well."
                    //
                    // Test case: U+0924,U+094D,U+0930,U+094d,U+0915
                    // with Sanskrit 2003 font.
                    //
                    // However, note that Ra,Halant,ZWJ is the correct way to
                    // request eyelash form of Ra, so we wouldn't inhibit it
                    // in that sequence.
                    //
                    // Test case: U+0924,U+094D,U+0930,U+094d,U+200D,U+0915
                    for (int i = start; i + 1 < basePosition; i++)
                    {
                        if (substitutionCollection[i].IndicShapingEngineInfo?.Category == Categories.Ra &&
                            substitutionCollection[i + 1].IndicShapingEngineInfo?.Category == Categories.H &&
                            (i + 1 == basePosition || substitutionCollection[i + 2].IndicShapingEngineInfo?.Category == Categories.ZWJ))
                        {
                            substitutionCollection.EnableShapingFeature(i, BlwfTag);
                            substitutionCollection.EnableShapingFeature(i + 1, BlwfTag);
                        }
                    }
                }

                int prefLen = 2;
                if (basePosition + prefLen < end &&
                    gSubTable.TryGetFeatureLookups(in PrefTag, this.ScriptClass, out _))
                {
                    // Find a Halant,Ra sequence and mark it for pre-base reordering processing.
                    for (int i = basePosition + 1; i + prefLen - 1 < end; i++)
                    {
                        tempBuffer[1] = substitutionCollection[i + 1];
                        tempBuffer[0] = substitutionCollection[i];
                        if (this.WouldSubstitute(tempCollection, in PrefTag, tempBuffer.Slice(0, 2)))
                        {
                            for (int j = 0; j < prefLen; j++)
                            {
                                substitutionCollection.EnableShapingFeature(i++, PrefTag);
                            }

                            // Mark the subsequent stuff with 'cfar'.  Used in Khmer.
                            // Read the feature spec.
                            // This allows distinguishing the following cases with MS Khmer fonts:
                            // U+1784,U+17D2,U+179A,U+17D2,U+1782
                            // U+1784,U+17D2,U+1782,U+17D2,U+179A
                            if (gSubTable.TryGetFeatureLookups(in CfarTag, this.ScriptClass, out _))
                            {
                                while (i < end)
                                {
                                    substitutionCollection.EnableShapingFeature(i, CfarTag);
                                    i++;
                                }
                            }

                            break;
                        }
                    }
                }

                // Apply ZWJ/ZWNJ effects
                for (int i = start + 1; i < end; i++)
                {
                    GlyphShapingData current = substitutionCollection[i];
                    if (IsJoiner(current))
                    {
                        bool nonJoiner = current.IndicShapingEngineInfo?.Category == Categories.ZWJ;
                        int j = i;

                        do
                        {
                            j--;

                            // ZWJ/ZWNJ should disable CJCT.  They do that by simply
                            // being there, since we don't skip them for the CJCT
                            // feature (ie. F_MANUAL_ZWJ)

                            // A ZWNJ disables HALF.
                            if (nonJoiner)
                            {
                                substitutionCollection.DisableShapingFeature(j, HalfTag);
                            }
                        }
                        while (j > start && !IsConsonant(substitutionCollection[j]));
                    }
                }

                Increment:
                start = end;
                end = NextSyllable(substitutionCollection, start, max);
            }
        }

        private Positions ConsonantPosition(GlyphSubstitutionCollection collection, ReadOnlySpan<GlyphShapingData> data)
        {
            if (this.WouldSubstitute(collection, in BlwfTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in BlwfTag, data.Slice(1, 2)))
            {
                return Positions.Below_C;
            }

            if (this.WouldSubstitute(collection, in PstfTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in PstfTag, data.Slice(1, 2)))
            {
                return Positions.Post_C;
            }

            if (this.WouldSubstitute(collection, in PrefTag, data.Slice(0, 2)) ||
                this.WouldSubstitute(collection, in PrefTag, data.Slice(1, 2)))
            {
                return Positions.Post_C;
            }

            return Positions.Base_C;
        }

        private bool WouldSubstitute(GlyphSubstitutionCollection collection, in Tag featureTag, ReadOnlySpan<GlyphShapingData> buffer)
        {
            collection.Clear();
            for (int i = 0; i < buffer.Length; i++)
            {
                collection.AddGlyph(buffer[i], i);
                collection.EnableShapingFeature(i, featureTag);
            }

            GlyphShapingData data = buffer[0];
            FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;

            if (fontMetrics.TryGetGSubTable(out GSubTable? gSubTable))
            {
                const int index = 0;
                SkippingGlyphIterator iterator = new(fontMetrics, collection, index, default);
                int initialCount = collection.Count;
                int collectionCount = initialCount;
                int count = initialCount - index;
                int i = index;

                // Set max constraints to prevent OutOfMemoryException or infinite loops from attacks.
                int maxCount = AdvancedTypographicUtils.GetMaxAllowableShapingCollectionCount(collection.Count);
                int maxOperationsCount = AdvancedTypographicUtils.GetMaxAllowableShapingOperationsCount(collection.Count);
                int currentOperations = 0;

                gSubTable.ApplyFeature(
                    fontMetrics,
                    collection,
                    ref iterator,
                    in featureTag,
                    this.ScriptClass,
                    index,
                    ref count,
                    ref i,
                    ref collectionCount,
                    maxCount,
                    maxOperationsCount,
                    ref currentOperations);

                return collection.Count != initialCount;
            }

            return false;
        }

        private static bool IsConsonant(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & ConsonantFlags) != 0;

        private static bool IsJoiner(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & JoinerFlags) != 0;

        private static bool IsHalantOrCoeng(GlyphShapingData data)
            => data.IndicShapingEngineInfo != null && (data.IndicShapingEngineInfo.Category & HalantOrCoengFlags) != 0;

        private static int NextSyllable(IGlyphShapingCollection collection, int index, int count)
        {
            if (index >= count)
            {
                return index;
            }

            int? syllable = collection[index].IndicShapingEngineInfo?.Syllable;
            while (++index < count)
            {
                if (collection[index].IndicShapingEngineInfo?.Syllable != syllable)
                {
                    break;
                }
            }

            return index;
        }

        private void FinalReorder(IGlyphShapingCollection collection, int index, int count)
        {
            if (collection is not GlyphSubstitutionCollection substitutionCollection)
            {
                return;
            }

            // Create a reusable temporary substitution collection and buffer to allow checking whether
            // certain combinations will be substituted.
            GlyphSubstitutionCollection tempCollection = new(this.textOptions);
            Span<GlyphShapingData> tempBuffer = new GlyphShapingData[3];

            int max = index + count;
            int start = index;
            int end = NextSyllable(substitutionCollection, index, max);
            while (start < max)
            {
                // 4. Final reordering:
                //
                // After the localized forms and basic shaping forms GSUB features have been
                // applied (see below), the shaping engine performs some final glyph
                // reordering before applying all the remaining font features to the entire
                // cluster.
                GlyphShapingData data = substitutionCollection[start];
                FontMetrics fontMetrics = data.TextRun.Font!.FontMetrics;
                if (!fontMetrics.TryGetGSubTable(out GSubTable? gSubTable))
                {
                    break;
                }

                bool tryPref = gSubTable.TryGetFeatureLookups(in PrefTag, this.ScriptClass, out _);

                // Find base consonant again.
                int basePosition = start;
                for (; basePosition < end; basePosition++)
                {
                    if (substitutionCollection[basePosition].IndicShapingEngineInfo?.Position >= Positions.Base_C)
                    {
                        if (tryPref && basePosition + 1 < end)
                        {
                            for (int i = basePosition + 1; i < end; i++)
                            {
                                GlyphShapingData current = substitutionCollection[i];
                                if (current.Features.FindIndex(x => x.Tag == PrefTag && x.Enabled) >= 0)
                                {
                                    if (!current.IsSubstituted && current.LigatureId != 0 && !current.IsDecomposed)
                                    {
                                        // Ok, this was a 'pref' candidate but didn't form any.
                                        // Base is around here...
                                        basePosition = i;
                                        while (basePosition < end && IsHalantOrCoeng(substitutionCollection[basePosition]))
                                        {
                                            basePosition++;
                                        }

                                        IndicShapingEngineInfo? info = substitutionCollection[basePosition].IndicShapingEngineInfo;
                                        if (info != null)
                                        {
                                            info.Position = Positions.Base_C;
                                            tryPref = false;
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        // For Malayalam, skip over unformed below- (but NOT post-) forms.
                        if (this.ScriptClass == ScriptClass.Malayalam)
                        {
                            for (int i = basePosition + 1; i < end; i++)
                            {
                                while (i < end && IsJoiner(substitutionCollection[i]))
                                {
                                    i++;
                                }

                                if (i == end || !IsHalantOrCoeng(substitutionCollection[i]))
                                {
                                    break;
                                }

                                i++; // Skip halant.
                                while (i < end && IsJoiner(substitutionCollection[i]))
                                {
                                    i++;
                                }

                                GlyphShapingData current = substitutionCollection[i];
                                if (i < end && IsConsonant(current) && current.IndicShapingEngineInfo?.Position == Positions.Below_C)
                                {
                                    basePosition = i;
                                    IndicShapingEngineInfo? info = substitutionCollection[basePosition].IndicShapingEngineInfo;
                                    if (info != null)
                                    {
                                        info.Position = Positions.Base_C;
                                    }
                                }
                            }
                        }

                        if (start < basePosition && substitutionCollection[basePosition].IndicShapingEngineInfo?.Position > Positions.Base_C)
                        {
                            basePosition--;
                        }

                        break;
                    }
                }

                if (basePosition == end && start < basePosition && substitutionCollection[basePosition - 1].IndicShapingEngineInfo?.Category == Categories.ZWJ)
                {
                    basePosition--;
                }

                if (basePosition < end)
                {
                    while (start < basePosition && (substitutionCollection[basePosition].IndicShapingEngineInfo?.Category & (Categories.N | HalantOrCoengFlags)) != 0)
                    {
                        basePosition--;
                    }
                }

                // o Reorder matras:
                //
                // If a pre-base matra character had been reordered before applying basic
                // features, the glyph can be moved closer to the main consonant based on
                // whether half-forms had been formed. Actual position for the matra is
                // defined as “after last standalone halant glyph, after initial matra
                // position and before the main consonant”. If ZWJ or ZWNJ follow this
                // halant, position is moved after it.
                //
                // Otherwise there can't be any pre-base matra characters.
                if (start + 1 < end && start < basePosition)
                {
                    // If we lost track of base, alas, position before last thingy.
                    int newPos = basePosition == end ? basePosition - 2 : basePosition - 1;

                    // Malayalam / Tamil do not have "half" forms or explicit virama forms.
                    // The glyphs formed by 'half' are Chillus or ligated explicit viramas.
                    // We want to position matra after them.
                    if (this.ScriptClass is not ScriptClass.Malayalam and not ScriptClass.Tamil)
                    {
                        while (newPos > start && (substitutionCollection[newPos].IndicShapingEngineInfo?.Category & (Categories.M | HalantOrCoengFlags)) == 0)
                        {
                            newPos--;
                        }

                        // If we found no Halant we are done.
                        // Otherwise only proceed if the Halant does
                        // not belong to the Matra itself!
                        GlyphShapingData current = substitutionCollection[newPos];
                        if (IsHalantOrCoeng(current) && current.IndicShapingEngineInfo?.Position != Positions.Pre_M)
                        {
                            // If ZWJ or ZWNJ follow this halant, position is moved after it.
                            if (newPos + 1 < end && IsJoiner(substitutionCollection[newPos + 1]))
                            {
                                newPos++;
                            }
                        }
                        else
                        {
                            newPos = start; // No move.
                        }
                    }

                    if (start < newPos && substitutionCollection[newPos].IndicShapingEngineInfo?.Position != Positions.Pre_M)
                    {
                        // Now go see if there's actually any matras...
                        for (int i = newPos; i > start; i--)
                        {
                            if (substitutionCollection[i - 1].IndicShapingEngineInfo?.Position == Positions.Pre_M)
                            {
                                int oldPos = i - 1;
                                if (oldPos < basePosition && basePosition <= newPos)
                                {
                                    // Shouldn't actually happen.
                                    basePosition--;
                                }

                                substitutionCollection.MoveGlyph(oldPos, newPos);
                                newPos--;
                            }
                        }
                    }
                }

                // o Reorder reph:
                //
                // Reph’s original position is always at the beginning of the syllable,
                // (i.e. it is not reordered at the character reordering stage). However,
                // it will be reordered according to the basic-forms shaping results.
                // Possible positions for reph, depending on the script, are; after main,
                // before post-base consonant forms, and after post-base consonant forms.

                // Two cases:
                //
                // - If repha is encoded as a sequence of characters (Ra,H or Ra,H,ZWJ), then
                //   we should only move it if the sequence ligated to the repha form.
                //
                // - If repha is encoded separately and in the logical position, we should only
                //   move it if it did NOT ligate.  If it ligated, it's probably the font trying
                //   to make it work without the reordering.
                GlyphShapingData original = substitutionCollection[start];
                if (start + 1 < end &&
                    original.IndicShapingEngineInfo?.Position == Positions.Ra_To_Become_Reph &&
                    (original.IndicShapingEngineInfo?.Category == Categories.Repha != (original.LigatureId != 0 && !original.IsDecomposed)))
                {
                    int newRephPos = start;
                    Positions rephPos = this.indicConfiguration.RephPosition;
                    bool found = false;

                    // 1. If reph should be positioned after post-base consonant forms,
                    //    proceed to step 5.
                    if (rephPos != Positions.After_Post)
                    {
                        // 2. If the reph repositioning class is not after post-base: target
                        //    position is after the first explicit halant glyph between the
                        //    first post-reph consonant and last main consonant. If ZWJ or ZWNJ
                        //    are following this halant, position is moved after it. If such
                        //    position is found, this is the target position. Otherwise,
                        //    proceed to the next step.
                        //
                        //    Note: in old-implementation fonts, where classifications were
                        //    fixed in shaping engine, there was no case where reph position
                        //    will be found on this step.
                        newRephPos = start + 1;
                        while (newRephPos < basePosition && !IsHalantOrCoeng(substitutionCollection[newRephPos]))
                        {
                            newRephPos++;
                        }

                        if (newRephPos < basePosition && IsHalantOrCoeng(substitutionCollection[newRephPos]))
                        {
                            // ->If ZWJ or ZWNJ are following this halant, position is moved after it.
                            if (newRephPos + 1 < basePosition && IsJoiner(substitutionCollection[newRephPos + 1]))
                            {
                                newRephPos++;
                            }

                            found = true;
                        }

                        // 3. If reph should be repositioned after the main consonant: find the
                        //    first consonant not ligated with main, or find the first
                        //    consonant that is not a potential pre-base reordering Ra.
                        if (!found && rephPos == Positions.After_Main)
                        {
                            newRephPos = basePosition;
                            while (newRephPos + 1 < end && substitutionCollection[newRephPos + 1].IndicShapingEngineInfo?.Position <= Positions.After_Main)
                            {
                                newRephPos++;
                            }

                            found = newRephPos < end;
                        }

                        // 4. If reph should be positioned before post-base consonant, find
                        //    first post-base classified consonant not ligated with main. If no
                        //    consonant is found, the target position should be before the
                        //    first matra, syllable modifier sign or vedic sign.
                        //
                        // This is our take on what step 4 is trying to say (and failing, BADLY).
                        if (!found && rephPos == Positions.After_Sub)
                        {
                            newRephPos = basePosition;
                            while (newRephPos + 1 < end && (substitutionCollection[newRephPos + 1].IndicShapingEngineInfo?.Position & (Positions.Post_C | Positions.After_Post | Positions.SMVD)) == 0)
                            {
                                newRephPos++;
                            }

                            found = newRephPos < end;
                        }
                    }

                    // 5. If no consonant is found in steps 3 or 4, move reph to a position
                    //    immediately before the first post-base matra, syllable modifier
                    //    sign or vedic sign that has a reordering class after the intended
                    //    reph position. For example, if the reordering position for reph
                    //    is post-main, it will skip above-base matras that also have a
                    //    post-main position.
                    if (!found)
                    {
                        // Copied from step 2.
                        newRephPos = start + 1;
                        while (newRephPos < basePosition && !IsHalantOrCoeng(substitutionCollection[newRephPos]))
                        {
                            newRephPos++;
                        }

                        if (newRephPos < basePosition && IsHalantOrCoeng(substitutionCollection[newRephPos]))
                        {
                            // ->If ZWJ or ZWNJ are following this halant, position is moved after it.
                            if (newRephPos + 1 < basePosition && IsJoiner(substitutionCollection[newRephPos + 1]))
                            {
                                newRephPos++;
                            }

                            found = true;
                        }
                    }

                    // 6. Otherwise, reorder reph to the end of the syllable.
                    if (!found)
                    {
                        newRephPos = end - 1;
                        while (newRephPos > start && substitutionCollection[newRephPos].IndicShapingEngineInfo?.Position == Positions.SMVD)
                        {
                            newRephPos--;
                        }

                        // If the Reph is to be ending up after a Matra,Halant sequence,
                        // position it before that Halant so it can interact with the Matra.
                        // However, if it's a plain Consonant,Halant we shouldn't do that.
                        // Uniscribe doesn't do this.
                        // TEST: U+0930,U+094D,U+0915,U+094B,U+094D
                        if (IsHalantOrCoeng(substitutionCollection[newRephPos]))
                        {
                            for (int i = basePosition + 1; i < newRephPos; i++)
                            {
                                if (substitutionCollection[i].IndicShapingEngineInfo?.Category == Categories.H)
                                {
                                    newRephPos--;
                                }
                            }
                        }
                    }

                    if (newRephPos != start)
                    {
                        substitutionCollection.MoveGlyph(start, newRephPos);
                    }

                    if (start < basePosition && basePosition <= newRephPos)
                    {
                        basePosition--;
                    }
                }

                // o Reorder pre-base reordering consonants:
                //
                // If a pre-base reordering consonant is found, reorder it according to
                // the following rules:
                if (tryPref && basePosition + 1 < end)
                {
                    for (int i = basePosition + 1; i < end; i++)
                    {
                        GlyphShapingData current = substitutionCollection[i];
                        if (current.Features.FindIndex(x => x.Tag == PrefTag && x.Enabled) >= 0)
                        {
                            // 1. Only reorder a glyph produced by substitution during application
                            //    of the <pref> feature. (Note that a font may shape a Ra consonant with
                            //    the feature generally but block it in certain contexts.)

                            // Note: We just check that something got substituted.  We don't check that
                            // the <pref> feature actually did it...
                            //
                            // Reorder pref only if it ligated.
                            if (current.LigatureId != 0 && !current.IsDecomposed)
                            {
                                // 2. Try to find a target position the same way as for pre-base matra.
                                //    If it is found, reorder pre-base consonant glyph.
                                //
                                // 3. If position is not found, reorder immediately before main
                                //    consonant.
                                int newPos = basePosition;

                                // Malayalam / Tamil do not have "half" forms or explicit virama forms.
                                // The glyphs formed by 'half' are Chillus or ligated explicit viramas.
                                // We want to position matra after them.
                                if (this.ScriptClass is not ScriptClass.Malayalam and not ScriptClass.Tamil)
                                {
                                    while (newPos > start && (substitutionCollection[newPos + 1].IndicShapingEngineInfo?.Category & (Categories.M | HalantOrCoengFlags)) == 0)
                                    {
                                        newPos--;
                                    }

                                    // In Khmer coeng model, a H,Ra can go *after* matras.  If it goes after a
                                    // split matra, it should be reordered to *before* the left part of such matra.
                                    if (newPos > start && substitutionCollection[newPos + 1].IndicShapingEngineInfo?.Category == Categories.M)
                                    {
                                        int oldPos = i;
                                        for (int j = basePosition + 1; j < oldPos; j++)
                                        {
                                            if (substitutionCollection[j].IndicShapingEngineInfo?.Category == Categories.M)
                                            {
                                                newPos--;
                                                break;
                                            }
                                        }
                                    }

                                    if (newPos > start && IsHalantOrCoeng(substitutionCollection[newPos - 1]))
                                    {
                                        // -> If ZWJ or ZWNJ follow this halant, position is moved after it.
                                        if (newPos < end && IsJoiner(substitutionCollection[newPos]))
                                        {
                                            newPos++;
                                        }
                                    }

                                    substitutionCollection.MoveGlyph(i, newPos);

                                    if (newPos <= basePosition && basePosition < i)
                                    {
                                        basePosition++;
                                    }
                                }
                            }

                            break;
                        }

                        // Apply 'init' to the Left Matra if it's a word start.
                        if (substitutionCollection[start].IndicShapingEngineInfo?.Position == Positions.Pre_M &&
                            (start == 0 || CodePoint.GetGeneralCategory(substitutionCollection[start - 1].CodePoint) is not UnicodeCategory.NonSpacingMark and not UnicodeCategory.Format))
                        {
                            substitutionCollection.EnableShapingFeature(start, InitTag);
                        }
                    }
                }

                start = end;
                end = NextSyllable(substitutionCollection, start, max);
            }
        }
    }
}
