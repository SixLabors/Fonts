// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors.Fonts.Unicode;

namespace SixLabors.Fonts.Tables.AdvancedTypographic.Shapers;

/// <summary>
/// Thai and Lao shaper. Handles SARA AM decomposition, NIKHAHIT/NIGGAHITA reordering,
/// and PUA-based fallback mark positioning for legacy fonts.
/// Based on HarfBuzz: <see href="https://github.com/harfbuzz/harfbuzz/blob/main/src/hb-ot-shaper-thai.cc"/>
/// </summary>
#pragma warning disable SA1201 // Nested types are grouped with the static data they define.
internal class ThaiShaper : DefaultShaper
{
    // Above-base state machine.
    // Start states by consonant type: NC=0, AC=1, RC=0, DC=0, NotConsonant=3
    private static readonly int[] AboveStartState = [0, 1, 0, 0, 3];

    // State transitions [state, markType] → (action, nextState)
    //              AV              BV              T
    // T0:  {NOP,T3}        {NOP,T0}        {SD, T3}
    // T1:  {SL, T2}        {NOP,T1}        {SDL,T2}
    // T2:  {NOP,T3}        {NOP,T2}        {SL, T3}
    // T3:  {NOP,T3}        {NOP,T3}        {NOP,T3}
    private static readonly StateTransition[,] AboveStateMachine =
    {
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 0), new(PuaAction.SD, 3) },
        { new(PuaAction.SL, 2), new(PuaAction.NOP, 1), new(PuaAction.SDL, 2) },
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 2), new(PuaAction.SL, 3) },
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 3), new(PuaAction.NOP, 3) },
    };

    // Below-base state machine.
    // Start states by consonant type: NC=0, AC=0, RC=1, DC=2, NotConsonant=2
    private static readonly int[] BelowStartState = [0, 0, 1, 2, 2];

    // State transitions [state, markType] → (action, nextState)
    //              AV              BV              T
    // B0:  {NOP,B0}        {NOP,B2}        {NOP,B0}
    // B1:  {NOP,B1}        {RD, B2}        {NOP,B1}
    // B2:  {NOP,B2}        {SD, B2}        {NOP,B2}
    private static readonly StateTransition[,] BelowStateMachine =
    {
        { new(PuaAction.NOP, 0), new(PuaAction.NOP, 2), new(PuaAction.NOP, 0) },
        { new(PuaAction.NOP, 1), new(PuaAction.RD, 2), new(PuaAction.NOP, 1) },
        { new(PuaAction.NOP, 2), new(PuaAction.SD, 2), new(PuaAction.NOP, 2) },
    };

    // Shift-Down PUA mappings.
    private static readonly PuaMapping[] SdMappings =
    [
        new(0x0E48, 0xF70A, 0xF88B), // MAI EK
        new(0x0E49, 0xF70B, 0xF88E), // MAI THO
        new(0x0E4A, 0xF70C, 0xF891), // MAI TRI
        new(0x0E4B, 0xF70D, 0xF894), // MAI CHATTAWA
        new(0x0E4C, 0xF70E, 0xF897), // THANTHAKHAT
        new(0x0E38, 0xF718, 0xF89B), // SARA U
        new(0x0E39, 0xF719, 0xF89C), // SARA UU
        new(0x0E3A, 0xF71A, 0xF89D), // PHINTHU
    ];

    // Shift-Down-Left PUA mappings.
    private static readonly PuaMapping[] SdlMappings =
    [
        new(0x0E48, 0xF705, 0xF88C), // MAI EK
        new(0x0E49, 0xF706, 0xF88F), // MAI THO
        new(0x0E4A, 0xF707, 0xF892), // MAI TRI
        new(0x0E4B, 0xF708, 0xF895), // MAI CHATTAWA
        new(0x0E4C, 0xF709, 0xF898), // THANTHAKHAT
    ];

    // Shift-Left PUA mappings.
    private static readonly PuaMapping[] SlMappings =
    [
        new(0x0E48, 0xF713, 0xF88A), // MAI EK
        new(0x0E49, 0xF714, 0xF88D), // MAI THO
        new(0x0E4A, 0xF715, 0xF890), // MAI TRI
        new(0x0E4B, 0xF716, 0xF893), // MAI CHATTAWA
        new(0x0E4C, 0xF717, 0xF896), // THANTHAKHAT
        new(0x0E31, 0xF710, 0xF884), // MAI HAN-AKAT
        new(0x0E34, 0xF701, 0xF885), // SARA I
        new(0x0E35, 0xF702, 0xF886), // SARA II
        new(0x0E36, 0xF703, 0xF887), // SARA UE
        new(0x0E37, 0xF704, 0xF888), // SARA UEE
        new(0x0E47, 0xF712, 0xF889), // MAITAIKHU
        new(0x0E4D, 0xF711, 0xF899), // NIKHAHIT
    ];

    // Remove-Descender PUA mappings.
    private static readonly PuaMapping[] RdMappings =
    [
        new(0x0E0D, 0xF70F, 0xF89A), // YO YING
        new(0x0E10, 0xF700, 0xF89E), // THO THAN
    ];

    private readonly FontMetrics fontMetrics;
    private readonly bool hasGsub;

    /// <summary>
    /// Thai consonant types for the PUA shaping state machines.
    /// </summary>
    private enum ConsonantType
    {
        /// <summary>Normal consonant.</summary>
        NC,

        /// <summary>Ascending consonant (Thai: 0x0E1B, 0x0E1D, 0x0E1F).</summary>
        AC,

        /// <summary>Consonant with removable descender (Thai: 0x0E0D, 0x0E10).</summary>
        RC,

        /// <summary>Consonant with strict descender (Thai: 0x0E0E, 0x0E0F).</summary>
        DC,

        /// <summary>Not a consonant.</summary>
        NotConsonant
    }

    /// <summary>
    /// Thai mark types for the PUA shaping state machines.
    /// </summary>
    private enum MarkType
    {
        /// <summary>Above-vowel mark.</summary>
        AV,

        /// <summary>Below-vowel mark.</summary>
        BV,

        /// <summary>Tone mark.</summary>
        T,

        /// <summary>Not a mark.</summary>
        NotMark
    }

    /// <summary>
    /// Actions emitted by the PUA shaping state machines.
    /// </summary>
    private enum PuaAction
    {
        /// <summary>No operation.</summary>
        NOP,

        /// <summary>Shift combining-mark down.</summary>
        SD,

        /// <summary>Shift combining-mark left.</summary>
        SL,

        /// <summary>Shift combining-mark down-left.</summary>
        SDL,

        /// <summary>Remove descender from base consonant.</summary>
        RD
    }

    public ThaiShaper(ScriptClass script, TextOptions textOptions, FontMetrics fontMetrics, bool hasGsub)
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

        // Step 1: Always decompose SARA AM -> NIKHAHIT + SARA AA and reorder.
        // This is needed even when the font has Thai/Lao GSUB tables.
        count = PreprocessSaraAm(substitutionCollection, this.fontMetrics, index, count);

        // Step 2: PUA-based fallback mark positioning.
        // Only applied for Thai (not Lao) when the font lacks Thai GSUB features.
        if (this.ScriptClass == ScriptClass.Thai && !this.hasGsub)
        {
            DoThaiPuaShaping(substitutionCollection, this.fontMetrics, index, count);
        }
    }

    /// <summary>
    /// Decomposes SARA AM (Thai U+0E33 / Lao U+0EB3) into NIKHAHIT + SARA AA,
    /// then reorders NIKHAHIT backward over any above-base marks.
    /// <para>
    /// This is needed even when the font has Thai/Lao GSUB tables.
    /// </para>
    /// <see href="https://linux.thai.net/~thep/th-otf/shaping.html"/>
    /// </summary>
    /// <returns>The updated count after decomposition.</returns>
    private static int PreprocessSaraAm(GlyphSubstitutionCollection collection, FontMetrics fontMetrics, int index, int count)
    {
        // Characters of significance:
        //
        //           Thai    Lao
        // SARA AM:  U+0E33  U+0EB3
        // SARA AA:  U+0E32  U+0EB2
        // Nikhahit: U+0E4D  U+0ECD
        //
        // When SARA AM is found, decompose into NIKHAHIT + SARA AA,
        // then move NIKHAHIT backward past any above-base marks.
        //
        // Example: <0E14, 0E4B, 0E33> -> <0E14, 0E4D, 0E4B, 0E32>
        int end = index + count;
        for (int i = index; i < end; i++)
        {
            GlyphShapingData data = collection[i];
            int codepoint = data.CodePoint.Value;

            if (!IsSaraAm(codepoint))
            {
                continue;
            }

            int nikhahitCodepoint = NikhahitFromSaraAm(codepoint);
            int saraAACodepoint = SaraAAFromSaraAm(codepoint);

            if (!fontMetrics.TryGetGlyphId(new CodePoint(nikhahitCodepoint), out ushort nikhahitId) ||
                !fontMetrics.TryGetGlyphId(new CodePoint(saraAACodepoint), out ushort saraAAId))
            {
                continue;
            }

            // Decompose SARA AM into [NIKHAHIT, SARA AA].
            // Replace puts NIKHAHIT at index i, SARA AA at index i+1.
            collection.Replace(i, [nikhahitId, saraAAId], FeatureTags.GlyphCompositionDecomposition);
            collection[i].CodePoint = new CodePoint(nikhahitCodepoint);
            collection[i + 1].CodePoint = new CodePoint(saraAACodepoint);
            end++;

            // Move NIKHAHIT backward over any above-base marks.
            int target = i;
            while (target > index && IsAboveBaseMark(collection[target - 1].CodePoint.Value))
            {
                target--;
            }

            if (target < i)
            {
                collection.MoveGlyph(i, target);
            }

            // Skip past SARA AA.
            i++;
        }

        return end - index;
    }

    /// <summary>
    /// Applies PUA-based fallback mark positioning using state machines.
    /// Only used for Thai fonts that lack GSUB features.
    /// </summary>
    private static void DoThaiPuaShaping(GlyphSubstitutionCollection collection, FontMetrics fontMetrics, int index, int count)
    {
        int aboveState = AboveStartState[(int)ConsonantType.NotConsonant];
        int belowState = BelowStartState[(int)ConsonantType.NotConsonant];
        int baseIndex = index;

        int end = index + count;
        for (int i = index; i < end; i++)
        {
            int codepoint = collection[i].CodePoint.Value;
            MarkType mt = GetMarkType(codepoint);

            if (mt == MarkType.NotMark)
            {
                ConsonantType ct = GetConsonantType(codepoint);
                aboveState = AboveStartState[(int)ct];
                belowState = BelowStartState[(int)ct];
                baseIndex = i;
                continue;
            }

            StateTransition aboveEdge = AboveStateMachine[aboveState, (int)mt];
            StateTransition belowEdge = BelowStateMachine[belowState, (int)mt];
            aboveState = aboveEdge.NextState;
            belowState = belowEdge.NextState;

            // At least one of the above/below actions is NOP.
            PuaAction action = aboveEdge.Action != PuaAction.NOP ? aboveEdge.Action : belowEdge.Action;

            if (action == PuaAction.RD)
            {
                int baseCp = collection[baseIndex].CodePoint.Value;
                int puaCp = ThaiPuaShape(baseCp, action, fontMetrics);
                if (puaCp != baseCp && fontMetrics.TryGetGlyphId(new CodePoint(puaCp), out ushort puaId))
                {
                    collection[baseIndex].CodePoint = new CodePoint(puaCp);
                    collection[baseIndex].GlyphId = puaId;
                }
            }
            else if (action != PuaAction.NOP)
            {
                int puaCp = ThaiPuaShape(codepoint, action, fontMetrics);
                if (puaCp != codepoint && fontMetrics.TryGetGlyphId(new CodePoint(puaCp), out ushort puaId))
                {
                    collection[i].CodePoint = new CodePoint(puaCp);
                    collection[i].GlyphId = puaId;
                }
            }
        }
    }

    /// <summary>
    /// Maps a Thai codepoint to its PUA variant based on the action.
    /// Tries Windows PUA first, then Mac PUA.
    /// </summary>
    private static int ThaiPuaShape(int codepoint, PuaAction action, FontMetrics fontMetrics)
    {
        ReadOnlySpan<PuaMapping> mappings = action switch
        {
            PuaAction.SD => SdMappings,
            PuaAction.SDL => SdlMappings,
            PuaAction.SL => SlMappings,
            PuaAction.RD => RdMappings,
            _ => default
        };

        for (int i = 0; i < mappings.Length; i++)
        {
            if (mappings[i].Original == codepoint)
            {
                // Try Windows PUA first.
                if (fontMetrics.TryGetGlyphId(new CodePoint(mappings[i].WinPua), out _))
                {
                    return mappings[i].WinPua;
                }

                // Try Mac PUA.
                if (fontMetrics.TryGetGlyphId(new CodePoint(mappings[i].MacPua), out _))
                {
                    return mappings[i].MacPua;
                }

                break;
            }
        }

        return codepoint;
    }

    /// <summary>
    /// Classifies a Thai consonant by its vertical extent.
    /// Only works for Thai codepoints (U+0E01..U+0E2E).
    /// </summary>
    private static ConsonantType GetConsonantType(int codepoint)
    {
        // Ascending consonants (tall right stroke).
        if (codepoint is 0x0E1B or 0x0E1D or 0x0E1F)
        {
            return ConsonantType.AC;
        }

        // Consonants with removable descender.
        if (codepoint is 0x0E0D or 0x0E10)
        {
            return ConsonantType.RC;
        }

        // Consonants with strict descender.
        if (codepoint is 0x0E0E or 0x0E0F)
        {
            return ConsonantType.DC;
        }

        // Normal consonant range.
        if (codepoint is >= 0x0E01 and <= 0x0E2E)
        {
            return ConsonantType.NC;
        }

        return ConsonantType.NotConsonant;
    }

    /// <summary>
    /// Classifies a Thai mark by its position relative to the base consonant.
    /// Only works for Thai codepoints.
    /// </summary>
    private static MarkType GetMarkType(int codepoint)
    {
        // Above-vowel marks.
        if (codepoint is 0x0E31
            or (>= 0x0E34 and <= 0x0E37) or 0x0E47
            or (>= 0x0E4D and <= 0x0E4E))
        {
            return MarkType.AV;
        }

        // Below-vowel marks.
        if (codepoint is >= 0x0E38 and <= 0x0E3A)
        {
            return MarkType.BV;
        }

        // Tone marks.
        if (codepoint is >= 0x0E48 and <= 0x0E4C)
        {
            return MarkType.T;
        }

        return MarkType.NotMark;
    }

    /// <summary>
    /// SARA AM: Thai U+0E33, Lao U+0EB3. The Lao variant is Thai + 0x80.
    /// </summary>
    private static bool IsSaraAm(int codepoint)
        => (codepoint & ~0x0080) == 0x0E33;

    /// <summary>
    /// Derives NIKHAHIT/NIGGAHITA from SARA AM. Thai: U+0E4D, Lao: U+0ECD.
    /// </summary>
    private static int NikhahitFromSaraAm(int codepoint)
        => codepoint - 0x0E33 + 0x0E4D;

    /// <summary>
    /// Derives SARA AA from SARA AM. Thai: U+0E32, Lao: U+0EB2.
    /// </summary>
    private static int SaraAAFromSaraAm(int codepoint)
        => codepoint - 1;

    /// <summary>
    /// Returns <see langword="true"/> if the codepoint is an above-base mark that NIKHAHIT
    /// should reorder past during SARA AM decomposition.
    /// Uses the <c>(codepoint &amp; ~0x80)</c> trick to handle both Thai and Lao uniformly.
    /// </summary>
    private static bool IsAboveBaseMark(int codepoint)
    {
        int u = codepoint & ~0x0080;
        return u is (>= 0x0E34 and <= 0x0E37) or (>= 0x0E47 and <= 0x0E4E) or 0x0E31
            or 0x0E3B;
    }

    /// <summary>
    /// State + action pair for state machine transitions.
    /// </summary>
    private readonly struct StateTransition(PuaAction action, int nextState)
    {
        public PuaAction Action { get; } = action;

        public int NextState { get; } = nextState;
    }

    /// <summary>
    /// PUA mapping entry: original codepoint, Windows PUA, Mac PUA.
    /// </summary>
    private readonly struct PuaMapping(ushort original, ushort winPua, ushort macPua)
    {
        public ushort Original { get; } = original;

        public ushort WinPua { get; } = winPua;

        public ushort MacPua { get; } = macPua;
    }
}
