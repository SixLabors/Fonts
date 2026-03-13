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
    /// <summary>
    /// Above-base state machine start states indexed by <see cref="ConsonantType"/>.
    /// </summary>
    private static readonly int[] AboveStartState = [0, 1, 0, 0, 3];

    /// <summary>
    /// Above-base state machine transitions. Rows are states (T0-T3), columns are <see cref="MarkType"/> (AV, BV, T).
    /// </summary>
    private static readonly StateTransition[,] AboveStateMachine =
    {
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 0), new(PuaAction.SD, 3) },
        { new(PuaAction.SL, 2), new(PuaAction.NOP, 1), new(PuaAction.SDL, 2) },
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 2), new(PuaAction.SL, 3) },
        { new(PuaAction.NOP, 3), new(PuaAction.NOP, 3), new(PuaAction.NOP, 3) },
    };

    /// <summary>
    /// Below-base state machine start states indexed by <see cref="ConsonantType"/>.
    /// </summary>
    private static readonly int[] BelowStartState = [0, 0, 1, 2, 2];

    /// <summary>
    /// Below-base state machine transitions. Rows are states (B0-B2), columns are <see cref="MarkType"/> (AV, BV, T).
    /// </summary>
    private static readonly StateTransition[,] BelowStateMachine =
    {
        { new(PuaAction.NOP, 0), new(PuaAction.NOP, 2), new(PuaAction.NOP, 0) },
        { new(PuaAction.NOP, 1), new(PuaAction.RD, 2), new(PuaAction.NOP, 1) },
        { new(PuaAction.NOP, 2), new(PuaAction.SD, 2), new(PuaAction.NOP, 2) },
    };

    /// <summary>Shift-Down PUA mappings for tone marks and below-vowel marks.</summary>
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

    /// <summary>Shift-Down-Left PUA mappings for tone marks.</summary>
    private static readonly PuaMapping[] SdlMappings =
    [
        new(0x0E48, 0xF705, 0xF88C), // MAI EK
        new(0x0E49, 0xF706, 0xF88F), // MAI THO
        new(0x0E4A, 0xF707, 0xF892), // MAI TRI
        new(0x0E4B, 0xF708, 0xF895), // MAI CHATTAWA
        new(0x0E4C, 0xF709, 0xF898), // THANTHAKHAT
    ];

    /// <summary>Shift-Left PUA mappings for tone marks and above-vowel marks.</summary>
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

    /// <summary>Remove-Descender PUA mappings for consonants with removable descenders.</summary>
    private static readonly PuaMapping[] RdMappings =
    [
        new(0x0E0D, 0xF70F, 0xF89A), // YO YING
        new(0x0E10, 0xF700, 0xF89E), // THO THAN
    ];

    /// <summary>The font metrics used for glyph lookups and PUA shaping.</summary>
    private readonly FontMetrics fontMetrics;

    /// <summary>Whether the font has GSUB features for Thai/Lao.</summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ThaiShaper"/> class.
    /// </summary>
    /// <param name="script">The script classification.</param>
    /// <param name="textOptions">The text options.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <param name="hasGsub">Whether the font has GSUB features for Thai/Lao.</param>
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
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
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
    /// <param name="collection">The glyph substitution collection.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <param name="index">The zero-based start index.</param>
    /// <param name="count">The number of elements to process.</param>
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
    /// <param name="codepoint">The original Thai codepoint value.</param>
    /// <param name="action">The PUA action to apply.</param>
    /// <param name="fontMetrics">The font metrics for glyph lookups.</param>
    /// <returns>The PUA codepoint if found in the font; otherwise, the original codepoint.</returns>
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
    /// <param name="codepoint">The codepoint value to classify.</param>
    /// <returns>The consonant type classification.</returns>
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
    /// <param name="codepoint">The codepoint value to classify.</param>
    /// <returns>The mark type classification.</returns>
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
    /// Returns <see langword="true"/> if the codepoint is SARA AM (Thai U+0E33 or Lao U+0EB3).
    /// </summary>
    /// <param name="codepoint">The codepoint value to test.</param>
    /// <returns><see langword="true"/> if the codepoint is SARA AM.</returns>
    private static bool IsSaraAm(int codepoint)
        => (codepoint & ~0x0080) == 0x0E33;

    /// <summary>
    /// Derives NIKHAHIT/NIGGAHITA from SARA AM. Thai: U+0E4D, Lao: U+0ECD.
    /// </summary>
    /// <param name="codepoint">The SARA AM codepoint value.</param>
    /// <returns>The corresponding NIKHAHIT codepoint value.</returns>
    private static int NikhahitFromSaraAm(int codepoint)
        => codepoint - 0x0E33 + 0x0E4D;

    /// <summary>
    /// Derives SARA AA from SARA AM. Thai: U+0E32, Lao: U+0EB2.
    /// </summary>
    /// <param name="codepoint">The SARA AM codepoint value.</param>
    /// <returns>The corresponding SARA AA codepoint value.</returns>
    private static int SaraAAFromSaraAm(int codepoint)
        => codepoint - 1;

    /// <summary>
    /// Returns <see langword="true"/> if the codepoint is an above-base mark that NIKHAHIT
    /// should reorder past during SARA AM decomposition.
    /// Uses the <c>(codepoint &amp; ~0x80)</c> trick to handle both Thai and Lao uniformly.
    /// </summary>
    /// <param name="codepoint">The codepoint value to test.</param>
    /// <returns><see langword="true"/> if the codepoint is an above-base mark.</returns>
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
        /// <summary>Gets the PUA action to apply.</summary>
        public PuaAction Action { get; } = action;

        /// <summary>Gets the next state for the state machine.</summary>
        public int NextState { get; } = nextState;
    }

    /// <summary>
    /// PUA mapping entry: original codepoint, Windows PUA, Mac PUA.
    /// </summary>
    private readonly struct PuaMapping(ushort original, ushort winPua, ushort macPua)
    {
        /// <summary>Gets the original Thai codepoint.</summary>
        public ushort Original { get; } = original;

        /// <summary>Gets the Windows PUA replacement codepoint.</summary>
        public ushort WinPua { get; } = winPua;

        /// <summary>Gets the Mac PUA replacement codepoint.</summary>
        public ushort MacPua { get; } = macPua;
    }
}
