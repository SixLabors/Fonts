// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.TrueType.Hinting;

/// <summary>
/// Code adapted from
/// <see href="https://github.com/MikePopoloski/SharpFont/blob/b28555e8fae94c57f1b5ccd809cdd1260f0eb55f/SharpFont/Internal/Interpreter.cs"/>.
///
/// Reference material:
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM05/Chap5.html"/> –
/// the original TrueType instruction set and execution model.
/// <see href="https://learn.microsoft.com/en-us/typography/cleartype/truetypecleartype"/> –
/// details on how Microsoft's ClearType rasterizer interprets TrueType hints.
/// <see href="https://freetype.org/freetype2/docs/hinting/subpixel-hinting.html"/> –
/// documentation of FreeType's subpixel hinting engines, including the v40 "minimal" interpreter.
///
/// <para>
/// This implementation matches the behavior of FreeType's v40 subpixel hinting interpreter,
/// with horizontal hinting disabled and full vertical TrueType instruction processing preserved.
/// It follows the v40 model in which outlines are adjusted primarily along the Y-axis and
/// instructions operate without backward compatibility constraints. This corresponds to
/// FreeType's configuration where <c>TT_CONFIG_OPTION_SUBPIXEL_HINTING</c> selects the
/// minimal (v40) engine and <c>backward_compatibility</c> is forced to zero.
/// </para>
///
/// <para>
/// Modern ClearType-hinted fonts are designed for this style of processing and will render
/// consistently under this interpreter. Legacy CRT-era fonts such as Arial or Times New Roman
/// also render cleanly under v40 semantics, though without legacy bi-level horizontal snapping,
/// which v40 intentionally omits.
/// </para>
/// </summary>
internal partial class TrueTypeInterpreter
{
    // Current and saved graphics state. cvtState is captured after the prep (CVT) program
    // runs so that each glyph program begins with a consistent baseline.
    private GraphicsState state;
    private GraphicsState cvtState;

    private readonly ExecutionStack stack;
    private readonly InstructionStream[] functions;
    private readonly InstructionStream[] instructionDefs;

    // Control Value Table: baseControlValueTable holds the scaled values after prep execution;
    // controlValueTable is a working copy restored at the start of each glyph program.
    private float[] baseControlValueTable;
    private float[] controlValueTable;

    // Storage area shared between prep and glyph programs. prepStorage holds the reference
    // to the storage array as it was after prep execution. Glyph programs use copy-on-write
    // (see WS instruction) so that prep state is preserved across glyphs.
    private int[] storage;
    private int[]? prepStorage;
    private bool inGlyphProgram;

    private IReadOnlyList<ushort> contours;
    private float scale;
    private int ppem;
    private int callStackSize;

    // Dot product of freedom and projection vectors, used to decompose
    // scalar distances into movement along the freedom vector.
    private float fdotp;

    // Super-rounding parameters set by SROUND/S45ROUND.
    private float roundThreshold;
    private float roundPhase;
    private float roundPeriod;

    // IUP tracking — once both axes have been interpolated, further IUP calls are skipped
    // and v40 backward compatibility blocks Y movement (post-IUP restriction).
    private bool iupXCalled;
    private bool iupYCalled;
    private bool isComposite;

    // Normalized variation axis coordinates for variable fonts, used by GETVARIATION/GETINFO.
    private float[]? normalizedAxisCoordinates;

    // FreeType TT_RunIns safety counters to prevent pathological fonts
    // from hanging the interpreter. Limits are computed per-glyph based on
    // point count and CVT size.
    private long insCounter;
    private long loopcallCounter;
    private long negJumpCounter;
    private long loopcallCounterMax;
    private long negJumpCounterMax;

    // Zone pointers: zp0/zp1/zp2 are the three zone pointer registers (ZP0-ZP2).
    // They can reference either the glyph zone (points) or the twilight zone.
    private Zone zp0;
    private Zone zp1;
    private Zone zp2;
    private Zone points;
    private Zone twilight;

    private static readonly float Sqrt2Over2 = (float)(Math.Sqrt(2) / 2);
    private const int MaxCallStack = 128;
    private const long MaxRunnableOpcodes = 1_000_000;
    private const float Epsilon = 0.000001F;

#if DEBUG
    private readonly List<OpCode> debugList = [];
#endif

#if HINTING_TRACE
    private readonly System.Text.StringBuilder traceLog = new();
    private int traceGlyphIndex;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="TrueTypeInterpreter"/> class
    /// with resource limits sourced from the font's <c>maxp</c> table.
    /// </summary>
    /// <param name="maxStack">Maximum stack depth.</param>
    /// <param name="maxStorage">Number of storage area locations.</param>
    /// <param name="maxFunctions">Number of function definition slots (FDEF).</param>
    /// <param name="maxInstructionDefs">Number of instruction definition slots (IDEF). When non-zero, a full 256-entry lookup table is allocated.</param>
    /// <param name="maxTwilightPoints">Number of points in the twilight zone.</param>
    public TrueTypeInterpreter(int maxStack, int maxStorage, int maxFunctions, int maxInstructionDefs, int maxTwilightPoints)
    {
        this.stack = new ExecutionStack(maxStack);
        this.storage = new int[maxStorage];
        this.functions = new InstructionStream[maxFunctions];
        this.instructionDefs = new InstructionStream[maxInstructionDefs > 0 ? 256 : 0];
        this.state = default;
        this.cvtState = default;
        this.twilight = new Zone(maxTwilightPoints, isTwilight: true);
        this.controlValueTable = [];
        this.baseControlValueTable = [];
        this.contours = [];
    }

    /// <summary>
    /// Sets the normalized axis coordinates for variable font hinting.
    /// These are used by the GETVARIATION and GETINFO instructions.
    /// </summary>
    /// <param name="coordinates">Normalized axis coordinates in the range [-1, 1], or <see langword="null"/> for non-variable fonts.</param>
    public void SetNormalizedAxisCoordinates(float[]? coordinates)
        => this.normalizedAxisCoordinates = coordinates;

    /// <summary>
    /// Executes the font program (fpgm) to populate function definitions (FDEF/IDEF).
    /// This must be called once per font before any CVT or glyph programs are executed.
    /// </summary>
    /// <param name="instructions">The raw font program bytecode.</param>
    public void InitializeFunctionDefs(byte[] instructions)
        => this.Execute(new StackInstructionStream(instructions, 0), false, true);

    /// <summary>
    /// Scales the Control Value Table and executes the prep (CVT) program.
    /// The prep program typically sets up the graphics state and may modify CVT entries
    /// for the current pixel size. The resulting state is saved and restored for each
    /// subsequent glyph program execution.
    /// </summary>
    /// <param name="cvt">The raw CVT entries from the font, or <see langword="null"/> if absent.</param>
    /// <param name="scale">The scale factor to apply to CVT entries (units-per-em to pixels).</param>
    /// <param name="ppem">The pixels-per-em value at the current size.</param>
    /// <param name="cvProgram">The raw prep program bytecode, or <see langword="null"/> if absent.</param>
    public void SetControlValueTable(short[]? cvt, float scale, float ppem, byte[]? cvProgram)
    {
        if (this.scale == scale || cvt == null)
        {
            return;
        }
        else
        {
            if (this.controlValueTable.Length == 0 && cvt.Length > 0)
            {
                this.controlValueTable = new float[cvt.Length];
            }

            for (int i = 0; i < cvt.Length; i++)
            {
                this.controlValueTable[i] = cvt[i] * scale;
            }
        }

        this.scale = scale;
        this.ppem = (int)Math.Round(ppem);
        this.zp0 = this.zp1 = this.zp2 = this.points;
        this.state.Reset();
        this.stack.Clear();

        if (cvProgram != null)
        {
            // Initialize safety counters for the prep program (no glyph points yet).
            this.insCounter = 0;
            this.loopcallCounter = 0;
            this.negJumpCounter = 0;
            int cvtSize = this.controlValueTable.Length;
            this.loopcallCounterMax = 300 + (22 * (long)cvtSize);
            this.negJumpCounterMax = this.loopcallCounterMax;

            this.Execute(new StackInstructionStream(cvProgram, 0), false, false);

            // Save prep program storage state so glyph programs can read it (copy-on-write in WS).
            this.prepStorage = this.storage;

            // save off the CVT graphics state so that we can restore it for each glyph we hint
            if ((this.state.InstructionControl & InstructionControlFlags.UseDefaultGraphicsState) != 0)
            {
                this.cvtState.Reset();
            }
            else
            {
                // always reset a few fields; copy the reset
                this.cvtState = this.state;
                this.cvtState.Freedom = Vector2.UnitX;
                this.cvtState.Projection = Vector2.UnitX;
                this.cvtState.DualProjection = Vector2.UnitX;
                this.cvtState.RoundState = RoundMode.ToGrid;
                this.cvtState.Loop = 1;
            }
        }

        if (this.controlValueTable.Length > 0)
        {
            if (this.baseControlValueTable.Length != this.controlValueTable.Length)
            {
                this.baseControlValueTable = new float[this.controlValueTable.Length];
            }

            Array.Copy(this.controlValueTable, this.baseControlValueTable, this.controlValueTable.Length);
        }
        else
        {
            this.baseControlValueTable = [];
        }
    }

    /// <summary>
    /// Attempts to apply TrueType hinting instructions to the specified glyph outline.
    /// </summary>
    /// <remarks>
    /// Hinting will not be applied if the instructions buffer is empty or if grid fitting is
    /// inhibited by the current interpreter state. If the instructions are malformed or an error occurs during
    /// execution, the method returns <see langword="false"/> and the glyph outline remains unhinted.
    /// </remarks>
    /// <param name="controlPoints">An array of control points representing the glyph's outline to be hinted.</param>
    /// <param name="endPoints">A read-only list of indices indicating the end points of each contour in the glyph.</param>
    /// <param name="instructions">A read-only memory buffer containing the TrueType hinting instructions to execute.</param>
    /// <param name="isComposite">Indicates whether the glyph is a composite glyph. Set to <see langword="true"/> for composite glyphs; otherwise, <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if hinting was successfully applied; otherwise, <see langword="false"/>.</returns>
    public bool TryHintGlyph(
        ControlPoint[] controlPoints,
        IReadOnlyList<ushort> endPoints,
        ReadOnlyMemory<byte> instructions,
        bool isComposite)
    {
        if (instructions.Length == 0)
        {
            return false;
        }

        // Check if the CVT program disabled hinting
        if ((this.state.InstructionControl & InstructionControlFlags.InhibitGridFitting) != 0)
        {
            return false;
        }

        try
        {
            // Save contours and points
            this.contours = endPoints;
            this.zp0 = this.zp1 = this.zp2 = this.points = new Zone(controlPoints, isTwilight: false);

            // reset all of our shared state
            this.state = this.cvtState;
            this.callStackSize = 0;

            // FreeType preserves prep program storage via copy-on-write in WS.
            // Restore the prep storage pointer; if glyph writes, WS will copy first.
            if (this.prepStorage != null)
            {
                this.storage = this.prepStorage;
            }
            else
            {
                Array.Clear(this.storage, 0, this.storage.Length);
            }

            this.inGlyphProgram = true;

            if (this.baseControlValueTable.Length > 0)
            {
                if (this.controlValueTable.Length != this.baseControlValueTable.Length)
                {
                    this.controlValueTable = new float[this.baseControlValueTable.Length];
                }

                Array.Copy(this.baseControlValueTable, this.controlValueTable, this.baseControlValueTable.Length);
            }
            else
            {
                this.controlValueTable = [];
            }

            this.ResetTwilightZone();

#if DEBUG
            this.debugList.Clear();
#endif

#if HINTING_TRACE
            this.traceLog.Clear();
            this.traceLog.AppendLine(System.FormattableString.Invariant($"=== GLYPH {this.traceGlyphIndex++} pts={controlPoints.Length - 4} composite={isComposite} ==="));
#endif

            this.stack.Clear();
            this.OnVectorsUpdated();
            this.iupXCalled = false;
            this.iupYCalled = false;
            this.isComposite = isComposite;

            // FreeType TT_RunIns — initialize safety counters.
            this.insCounter = 0;
            this.loopcallCounter = 0;
            this.negJumpCounter = 0;
            int nPoints = controlPoints.Length;
            int cvtSize = this.controlValueTable.Length;
            if (nPoints > 0)
            {
                this.loopcallCounterMax = Math.Max(50, 10 * (long)nPoints) + Math.Max(50, cvtSize / 10);
            }
            else
            {
                this.loopcallCounterMax = 300 + (22 * (long)cvtSize);
            }

            this.negJumpCounterMax = this.loopcallCounterMax;

            // normalize the round state settings
            switch (this.state.RoundState)
            {
                case RoundMode.Super:
                    this.SetSuperRound(1.0f);
                    break;
                case RoundMode.Super45:
                    this.SetSuperRound(Sqrt2Over2);
                    break;
            }

            this.Execute(new StackInstructionStream(instructions, 0), false, false);

#if HINTING_TRACE
            System.Console.Error.Write(this.traceLog);
#endif

            return true;
        }
        catch (Exception)
        {
#if HINTING_TRACE
            System.Console.Error.Write(this.traceLog);

            // Rethrow to diagnose hinting failures.
            throw;
#endif
            return false;
        }
    }

    /// <summary>
    /// Resets all twilight zone points to the origin and clears their touch state,
    /// preventing stale data from leaking between glyph programs.
    /// </summary>
    private void ResetTwilightZone()
    {
        // In FreeType, twilight points are defined to have original coordinates at (0,0).
        // Reset both original and current coordinates, and clear touch state, to avoid state leaking between glyphs.
        ControlPoint[] twCurrent = this.twilight.Current;
        ControlPoint[] twOriginal = this.twilight.Original;

        int len = twCurrent.Length;
        for (int i = 0; i < len; i++)
        {
            twCurrent[i].Point = default;
            twOriginal[i].Point = default;
        }

        Array.Clear(this.twilight.TouchState, 0, this.twilight.TouchState.Length);
    }

    /// <summary>
    /// Core instruction dispatch loop. Reads and executes opcodes from the given
    /// instruction stream until the stream is exhausted or an error terminates execution.
    /// </summary>
    /// <param name="stream">The instruction stream to execute.</param>
    /// <param name="inFunction">
    /// <see langword="true"/> when executing inside a CALL/LOOPCALL function body.
    /// Controls whether ENDF returns to the caller or exits execution.
    /// </param>
    /// <param name="allowFunctionDefs">
    /// <see langword="true"/> when executing the font program (fpgm), which permits
    /// FDEF and IDEF instructions. Glyph and prep programs set this to <see langword="false"/>.
    /// </param>
    private void Execute(StackInstructionStream stream, bool inFunction, bool allowFunctionDefs)
    {
        while (!stream.Done)
        {
            int rawOpcode = stream.NextByte();
            OpCode opcode = (OpCode)rawOpcode;

#if DEBUG
            this.debugList.Add(opcode);
#endif

            // FreeType TT_RunIns — global instruction counter to prevent infinite loops.
            if (++this.insCounter > MaxRunnableOpcodes)
            {
                return;
            }

            // FreeType TT_RunIns — pre-validate stack depth before dispatch.
            byte popPush = PopPushCount[rawOpcode];
            int pops = popPush >> 4;
            int pushes = popPush & 0xF;

#if HINTING_TRACE
            int preStackCount = this.stack.Count;
            this.TracePreInstruction(opcode, pops);
#endif

            // Underflow: push zeroes to fill missing args (FreeType non-pedantic mode).
            if (this.stack.Count < pops)
            {
                int missing = pops - this.stack.Count;
                this.stack.Clear();
                for (int z = 0; z < pops; z++)
                {
                    this.stack.Push(0);
                }
            }

            // Overflow: exit the run loop (FreeType non-pedantic: set error and return).
            if (this.stack.Count - pops + pushes > this.stack.Capacity)
            {
                return;
            }

            switch (opcode)
            {
                // ==== PUSH INSTRUCTIONS ====
                case OpCode.NPUSHB:
                case OpCode.PUSHB1:
                case OpCode.PUSHB2:
                case OpCode.PUSHB3:
                case OpCode.PUSHB4:
                case OpCode.PUSHB5:
                case OpCode.PUSHB6:
                case OpCode.PUSHB7:
                case OpCode.PUSHB8:
                {
                    int count = opcode == OpCode.NPUSHB ? stream.NextByte() : opcode - OpCode.PUSHB1 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        this.stack.Push(stream.NextByte());
                    }
                }

                break;
                case OpCode.NPUSHW:
                case OpCode.PUSHW1:
                case OpCode.PUSHW2:
                case OpCode.PUSHW3:
                case OpCode.PUSHW4:
                case OpCode.PUSHW5:
                case OpCode.PUSHW6:
                case OpCode.PUSHW7:
                case OpCode.PUSHW8:
                {
                    int count = opcode == OpCode.NPUSHW ? stream.NextByte() : opcode - OpCode.PUSHW1 + 1;
                    for (int i = 0; i < count; i++)
                    {
                        this.stack.Push(stream.NextWord());
                    }
                }

                break;

                // ==== STORAGE MANAGEMENT ====
                case OpCode.RS:
                {
                    int loc = this.stack.Pop();
                    if ((uint)loc >= (uint)this.storage.Length)
                    {
                        this.stack.Push(0);
                    }
                    else
                    {
                        this.stack.Push(this.storage[loc]);
                    }
                }

                break;
                case OpCode.WS:
                {
                    int value = this.stack.Pop();
                    int loc = this.stack.Pop();
                    if ((uint)loc < (uint)this.storage.Length)
                    {
                        // FreeType copy-on-write: when glyph program first writes to storage,
                        // make a private copy so prep program state is preserved for other glyphs.
                        if (this.inGlyphProgram && this.storage == this.prepStorage)
                        {
                            int[] glyphStorage = new int[this.storage.Length];
                            Array.Copy(this.storage, glyphStorage, this.storage.Length);
                            this.storage = glyphStorage;
                        }

                        this.storage[loc] = value;
                    }
                }

                break;

                // ==== CONTROL VALUE TABLE ====
                case OpCode.WCVTP:
                {
                    float value = this.stack.PopFloat();
                    int loc = this.stack.Pop();
                    if ((uint)loc < (uint)this.controlValueTable.Length)
                    {
                        this.controlValueTable[loc] = value;
                    }
                }

                break;
                case OpCode.WCVTF:
                {
                    int value = this.stack.Pop();
                    int loc = this.stack.Pop();
                    if ((uint)loc < (uint)this.controlValueTable.Length)
                    {
                        this.controlValueTable[loc] = value * this.scale;
                    }
                }

                break;
                case OpCode.RCVT:
                {
                    int loc = this.stack.Pop();
                    if ((uint)loc >= (uint)this.controlValueTable.Length)
                    {
                        this.stack.Push(0);
                    }
                    else
                    {
                        this.stack.Push(this.controlValueTable[loc]);
                    }

                    break;
                }

                // ==== STATE VECTORS ====
                case OpCode.SVTCA0:
                case OpCode.SVTCA1:
                {
                    byte axis = opcode - OpCode.SVTCA0;
                    this.SetFreedomVectorToAxis(axis);
                    this.SetProjectionVectorToAxis(axis);
                }

                break;
                case OpCode.SFVTPV:
                {
                    this.state.Freedom = this.state.Projection;
                    this.OnVectorsUpdated();
                    break;
                }

                case OpCode.SPVTCA0:
                case OpCode.SPVTCA1:
                {
                    this.SetProjectionVectorToAxis(opcode - OpCode.SPVTCA0);
                    break;
                }

                case OpCode.SFVTCA0:
                case OpCode.SFVTCA1:
                {
                    this.SetFreedomVectorToAxis(opcode - OpCode.SFVTCA0);
                    break;
                }

                case OpCode.SPVTL0:
                case OpCode.SPVTL1:
                case OpCode.SFVTL0:
                case OpCode.SFVTL1:
                {
                    this.SetVectorToLine(opcode - OpCode.SPVTL0, false);
                    break;
                }

                case OpCode.SDPVTL0:
                case OpCode.SDPVTL1:
                {
                    this.SetVectorToLine(opcode - OpCode.SDPVTL0, true);
                    break;
                }

                case OpCode.SPVFS:
                case OpCode.SFVFS:
                {
                    int y = this.stack.Pop();
                    int x = this.stack.Pop();
                    Vector2 vec = Vector2.Normalize(new Vector2(F2Dot14ToFloat(x), F2Dot14ToFloat(y)));
                    if (opcode == OpCode.SFVFS)
                    {
                        this.state.Freedom = vec;
                    }
                    else
                    {
                        this.state.Projection = vec;
                        this.state.DualProjection = vec;
                    }

                    this.OnVectorsUpdated();
                }

                break;
                case OpCode.GPV:
                case OpCode.GFV:
                {
                    Vector2 vec = opcode == OpCode.GPV ? this.state.Projection : this.state.Freedom;
                    this.stack.Push(FloatToF2Dot14(vec.X));
                    this.stack.Push(FloatToF2Dot14(vec.Y));
                }

                break;

                // ==== GRAPHICS STATE ====
                case OpCode.SRP0:
                {
                    this.state.Rp0 = this.stack.Pop();
                    break;
                }

                case OpCode.SRP1:
                {
                    this.state.Rp1 = this.stack.Pop();
                    break;
                }

                case OpCode.SRP2:
                {
                    this.state.Rp2 = this.stack.Pop();
                    break;
                }

                case OpCode.SZP0:
                {
                    if (this.TryGetZoneFromStack(out Zone szp0Zone))
                    {
                        this.zp0 = szp0Zone;
                    }

                    break;
                }

                case OpCode.SZP1:
                {
                    if (this.TryGetZoneFromStack(out Zone szp1Zone))
                    {
                        this.zp1 = szp1Zone;
                    }

                    break;
                }

                case OpCode.SZP2:
                {
                    if (this.TryGetZoneFromStack(out Zone szp2Zone))
                    {
                        this.zp2 = szp2Zone;
                    }

                    break;
                }

                case OpCode.SZPS:
                {
                    if (this.TryGetZoneFromStack(out Zone szpsZone))
                    {
                        this.zp0 = this.zp1 = this.zp2 = szpsZone;
                    }

                    break;
                }

                case OpCode.RTHG:
                {
                    this.state.RoundState = RoundMode.ToHalfGrid;
                    break;
                }

                case OpCode.RTG:
                {
                    this.state.RoundState = RoundMode.ToGrid;
                    break;
                }

                case OpCode.RTDG:
                {
                    this.state.RoundState = RoundMode.ToDoubleGrid;
                    break;
                }

                case OpCode.RDTG:
                {
                    this.state.RoundState = RoundMode.DownToGrid;
                    break;
                }

                case OpCode.RUTG:
                {
                    this.state.RoundState = RoundMode.UpToGrid;
                    break;
                }

                case OpCode.ROFF:
                {
                    this.state.RoundState = RoundMode.Off;
                    break;
                }

                case OpCode.SROUND:
                {
                    this.state.RoundState = RoundMode.Super;
                    this.SetSuperRound(1.0f);
                    break;
                }

                case OpCode.S45ROUND:
                {
                    this.state.RoundState = RoundMode.Super45;
                    this.SetSuperRound(Sqrt2Over2);
                    break;
                }

                case OpCode.INSTCTRL:
                {
                    // FreeType Ins_INSTCTRL.
                    // Always pop both arguments to keep the stack balanced.
                    int selector = this.stack.Pop();
                    int value = this.stack.Pop();

                    // FreeType restricts selectors 1-2 to the prep (CVT) program only.
                    // Selector 3 (NativeClearType) can also be set during prep.
                    // Glyph programs cannot modify instruction control flags.
                    if (selector is >= 1 and <= 3 && !this.inGlyphProgram)
                    {
                        int bit = 1 << (selector - 1);

                        // FreeType validates: if value != 0, it must equal the expected bit.
                        if (value == 0)
                        {
                            this.state.InstructionControl = (InstructionControlFlags)((int)this.state.InstructionControl & ~bit);
                        }
                        else if (value == bit)
                        {
                            this.state.InstructionControl = (InstructionControlFlags)((int)this.state.InstructionControl | bit);
                        }
                    }
                }

                break;
                case OpCode.SCANCTRL: /* instruction unspported */
                case OpCode.SCANTYPE: /* instruction unspported */
                case OpCode.SANGW: /* instruction unspported */
                {
                    this.stack.Pop();
                    break;
                }

                case OpCode.SLOOP:
                {
                    int loop = this.stack.Pop();
                    if (loop < 0)
                    {
                        // FreeType sets Bad_Argument error and returns without modifying state.
                        break;
                    }

                    // FreeType heuristically caps loop count at 16 bits.
                    this.state.Loop = loop > 0xFFFF ? 0xFFFF : loop;
                    break;
                }

                case OpCode.SMD:
                {
                    this.state.MinDistance = this.stack.PopFloat();
                    break;
                }

                case OpCode.SCVTCI:
                {
                    this.state.ControlValueCutIn = this.stack.PopFloat();
                    break;
                }

                case OpCode.SSWCI:
                {
                    this.state.SingleWidthCutIn = this.stack.PopFloat();
                    break;
                }

                case OpCode.SSW:
                {
                    this.state.SingleWidthValue = this.stack.Pop() * this.scale;
                    break;
                }

                case OpCode.FLIPON:
                {
                    this.state.AutoFlip = true;
                    break;
                }

                case OpCode.FLIPOFF:
                {
                    this.state.AutoFlip = false;
                    break;
                }

                case OpCode.SDB:
                {
                    this.state.DeltaBase = this.stack.Pop();
                    break;
                }

                case OpCode.SDS:
                {
                    this.state.DeltaShift = this.stack.Pop();
                    break;
                }

                // ==== POINT MEASUREMENT ====
                case OpCode.GC0:
                {
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp2.Current.Length)
                    {
                        this.stack.Push(0);
                        break;
                    }

                    this.stack.Push(this.Project(this.zp2.GetCurrent(pointIndex)));
                    break;
                }

                case OpCode.GC1:
                {
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp2.Current.Length)
                    {
                        this.stack.Push(0);
                        break;
                    }

                    this.stack.Push(this.DualProject(this.zp2.GetOriginal(pointIndex)));
                    break;
                }

                case OpCode.SCFS:
                {
                    float value = this.stack.PopFloat();
                    int index = this.stack.Pop();
                    if ((uint)index >= (uint)this.zp2.Current.Length)
                    {
                        break;
                    }

                    Vector2 point = this.zp2.GetCurrent(index);
                    this.MovePoint(this.zp2, index, value - this.Project(point));

                    // Moving twilight points moves their "original" value also
                    if (this.zp2.IsTwilight)
                    {
                        this.zp2.Original[index].Point = this.zp2.Current[index].Point;
                    }
                }

                break;
                case OpCode.MD0:
                {
                    int i0 = this.stack.Pop();
                    int i1 = this.stack.Pop();
                    if ((uint)i0 >= (uint)this.zp1.Current.Length ||
                        (uint)i1 >= (uint)this.zp0.Current.Length)
                    {
                        this.stack.Push(0);
                        break;
                    }

                    this.stack.Push(this.DualProject(this.zp0.GetOriginal(i1) - this.zp1.GetOriginal(i0)));
                }

                break;
                case OpCode.MD1:
                {
                    int i0 = this.stack.Pop();
                    int i1 = this.stack.Pop();
                    if ((uint)i0 >= (uint)this.zp1.Current.Length ||
                        (uint)i1 >= (uint)this.zp0.Current.Length)
                    {
                        this.stack.Push(0);
                        break;
                    }

                    this.stack.Push(this.Project(this.zp0.GetCurrent(i1) - this.zp1.GetCurrent(i0)));
                }

                break;
                case OpCode.MPS: // MPS should return point size, but we assume DPI so it's the same as pixel size
                case OpCode.MPPEM:
                {
                    this.stack.Push(this.ppem);
                    break;
                }

                case OpCode.AA: /* deprecated instruction */
                {
                    this.stack.Pop();
                    break;
                }

                // ==== POINT MODIFICATION ====
                case OpCode.FLIPPT:
                {
                    // FreeType: FLIP instructions skip when backward_compatibility == 0x7.
                    bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
                    bool blocked = !nativeClearType && this.iupXCalled && this.iupYCalled;
                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int index = this.stack.Pop();
                        if (blocked || (uint)index >= (uint)this.points.Current.Length)
                        {
                            continue;
                        }

                        this.points.Current[index].OnCurve ^= true;
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.FLIPRGON:
                {
                    bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
                    bool blocked = !nativeClearType && this.iupXCalled && this.iupYCalled;
                    int end = this.stack.Pop();
                    int start = this.stack.Pop();
                    if (blocked ||
                        (uint)end >= (uint)this.points.Current.Length ||
                        (uint)start >= (uint)this.points.Current.Length)
                    {
                        break;
                    }

                    for (int i = start; i <= end; i++)
                    {
                        this.points.Current[i].OnCurve = true;
                    }
                }

                break;
                case OpCode.FLIPRGOFF:
                {
                    bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
                    bool blocked = !nativeClearType && this.iupXCalled && this.iupYCalled;
                    int end = this.stack.Pop();
                    int start = this.stack.Pop();
                    if (blocked ||
                        (uint)end >= (uint)this.points.Current.Length ||
                        (uint)start >= (uint)this.points.Current.Length)
                    {
                        break;
                    }

                    for (int i = start; i <= end; i++)
                    {
                        this.points.Current[i].OnCurve = false;
                    }
                }

                break;
                case OpCode.SHP0:
                case OpCode.SHP1:
                {
                    // FreeType Ins_SHP: uses Move_Zp2_Point for each point.
                    if (!this.TryComputeDisplacement((int)opcode, out _, out _, out Vector2 displacement))
                    {
                        // FreeType: Compute_Point_Displacement failure returns (no Fail label, loop NOT reset).
                        for (int i = 0; i < this.state.Loop; i++)
                        {
                            this.stack.Pop();
                        }

                        this.state.Loop = 1;
                        break;
                    }

                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        if ((uint)pointIndex < (uint)this.zp2.Current.Length)
                        {
                            this.MoveZp2Point(this.zp2, pointIndex, displacement.X, displacement.Y, true);
                        }
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.SHPIX:
                {
                    // FreeType Ins_SHPIX: v40 backward compatibility gating.
                    float magnitude = this.stack.PopFloat();
                    float dx = magnitude * this.state.Freedom.X;
                    float dy = magnitude * this.state.Freedom.Y;
                    bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
                    bool postIUP = this.iupXCalled && this.iupYCalled;
                    bool inTwilight = this.zp0.IsTwilight || this.zp1.IsTwilight || this.zp2.IsTwilight;

                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        if ((uint)pointIndex >= (uint)this.zp2.Current.Length)
                        {
                            continue;
                        }

                        if (!nativeClearType)
                        {
                            // Backward compat mode: gated Y-only movement.
                            // Twilight zone always allowed; otherwise need composite+freeY or Y-touched.
                            // Post-IUP (0x7): nothing moves (MoveZp2Point blocks Y at post-IUP).
                            if (inTwilight ||
                                (!postIUP &&
                                 ((this.isComposite && this.state.Freedom.Y != 0) ||
                                  ((this.zp2.TouchState[pointIndex] & TouchState.Y) == TouchState.Y))))
                            {
                                this.MoveZp2Point(this.zp2, pointIndex, 0, dy, true);
                            }
                        }
                        else
                        {
                            // Native ClearType: move freely on both axes.
                            this.MoveZp2Point(this.zp2, pointIndex, dx, dy, true);
                        }
                    }

                    this.state.Loop = 1;
                    break;
                }

                case OpCode.SHC0:
                case OpCode.SHC1:
                {
                    if (!this.TryComputeDisplacement((int)opcode, out Zone zone, out int point, out Vector2 displacement))
                    {
                        this.stack.Pop();
                        break;
                    }

                    int contour = this.stack.Pop();
                    int bounds = this.zp2.IsTwilight ? 1 : this.contours.Count;
                    if ((uint)contour >= (uint)bounds)
                    {
                        break;
                    }

                    int start = contour == 0 ? 0 : this.contours[contour - 1] + 1;
                    int count = this.zp2.IsTwilight ? this.zp2.Current.Length : this.contours[contour] + 1;
                    ControlPoint[] current = this.zp2.Current;
                    TouchState[] states = this.zp2.TouchState;

                    for (int i = start; i < count; i++)
                    {
                        // Don't move the reference point
                        if (zone.Current != current || point != i)
                        {
                            this.MoveZp2Point(this.zp2, i, displacement.X, displacement.Y, true);
                        }
                    }
                }

                break;
                case OpCode.SHZ0:
                case OpCode.SHZ1:
                {
                    // FreeType Ins_SHZ: pop zone index first, then compute displacement.
                    int shzZone = this.stack.Pop();
                    if ((uint)shzZone >= 2)
                    {
                        break;
                    }

                    if (!this.TryComputeDisplacement((int)opcode, out Zone zone, out int point, out Vector2 displacement))
                    {
                        break;
                    }

                    int count = 0;
                    if (this.zp2.IsTwilight)
                    {
                        count = this.zp2.Current.Length;
                    }
                    else if (this.contours.Count > 0)
                    {
                        count = this.contours[this.contours.Count - 1] + 1;
                    }

                    ControlPoint[] current = this.zp2.Current;
                    for (int i = 0; i < count; i++)
                    {
                        // Don't move the reference point
                        if (zone.Current != current || point != i)
                        {
                            this.MoveZp2Point(this.zp2, i, displacement.X, displacement.Y, false);
                        }
                    }
                }

                break;
                case OpCode.MIAP0:
                case OpCode.MIAP1:
                {
                    float distance = this.ReadCvt();
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp0.Current.Length)
                    {
                        // FreeType Fail label: still sets rp0/rp1.
                        this.state.Rp0 = pointIndex;
                        this.state.Rp1 = pointIndex;
                        break;
                    }

                    // this instruction is used in the CVT to set up twilight points with original values
                    if (this.zp0.IsTwilight)
                    {
                        Vector2 original = this.state.Freedom * distance;
                        this.zp0.Original[pointIndex].Point = original;
                        this.zp0.Current[pointIndex].Point = original;
                    }

                    // current position of the point along the projection vector
                    Vector2 point = this.zp0.GetCurrent(pointIndex);
                    float currentPos = this.Project(point);
                    if (opcode == OpCode.MIAP1)
                    {
                        // only use the CVT if we are above the cut-in point
                        if (Math.Abs(distance - currentPos) > this.state.ControlValueCutIn)
                        {
                            distance = currentPos;
                        }

                        distance = this.Round(distance);
                    }

                    this.MovePoint(this.zp0, pointIndex, distance - currentPos);
                    this.state.Rp0 = pointIndex;
                    this.state.Rp1 = pointIndex;
                }

                break;
                case OpCode.MDAP0:
                case OpCode.MDAP1:
                {
                    // FreeType Ins_MDAP: bounds check before access.
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp0.Current.Length)
                    {
                        break;
                    }

                    Vector2 point = this.zp0.GetCurrent(pointIndex);
                    float distance = 0.0f;
                    if (opcode == OpCode.MDAP1)
                    {
                        distance = this.Project(point);
                        distance = this.Round(distance) - distance;
                    }

                    this.MovePoint(this.zp0, pointIndex, distance);
                    this.state.Rp0 = pointIndex;
                    this.state.Rp1 = pointIndex;
                }

                break;
                case OpCode.MSIRP0:
                case OpCode.MSIRP1:
                {
                    float targetDistance = this.stack.PopFloat();
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp1.Current.Length ||
                        (uint)this.state.Rp0 >= (uint)this.zp0.Current.Length)
                    {
                        break;
                    }

                    // if we're operating on the twilight zone, initialize the points
                    if (this.zp1.IsTwilight)
                    {
                        ControlPoint[] zp0Original = this.zp0.Original;
                        ControlPoint[] zp1Current = this.zp1.Current;
                        ControlPoint[] zp1Original = this.zp1.Original;
                        zp1Original[pointIndex].Point = zp0Original[this.state.Rp0].Point + (targetDistance * this.state.Freedom / this.fdotp);
                        zp1Current[pointIndex].Point = zp1Original[pointIndex].Point;
                    }

                    float currentDistance = this.Project(this.zp1.GetCurrent(pointIndex) - this.zp0.GetCurrent(this.state.Rp0));
                    this.MovePoint(this.zp1, pointIndex, targetDistance - currentDistance);

                    this.state.Rp1 = this.state.Rp0;
                    this.state.Rp2 = pointIndex;
                    if (opcode == OpCode.MSIRP1)
                    {
                        this.state.Rp0 = pointIndex;
                    }
                }

                break;
                case OpCode.IP:
                {
                    // FreeType Ins_IP: bounds check rp1 first.
                    if ((uint)this.state.Rp1 >= (uint)this.zp0.Current.Length)
                    {
                        // Fail label: drain stack and reset loop.
                        for (int i = 0; i < this.state.Loop; i++)
                        {
                            this.stack.Pop();
                        }

                        this.state.Loop = 1;
                        break;
                    }

                    Vector2 originalBase = this.zp0.GetOriginal(this.state.Rp1);
                    Vector2 currentBase = this.zp0.GetCurrent(this.state.Rp1);

                    // FreeType: if rp2 fails, set ranges to 0 but continue.
                    float originalRange = 0;
                    float currentRange = 0;
                    if ((uint)this.state.Rp2 < (uint)this.zp1.Current.Length)
                    {
                        originalRange = this.DualProject(this.zp1.GetOriginal(this.state.Rp2) - originalBase);
                        currentRange = this.Project(this.zp1.GetCurrent(this.state.Rp2) - currentBase);
                    }

                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        if ((uint)pointIndex >= (uint)this.zp2.Current.Length)
                        {
                            continue;
                        }

                        Vector2 point = this.zp2.GetCurrent(pointIndex);
                        float currentDistance = this.Project(point - currentBase);
                        float originalDistance = this.DualProject(this.zp2.GetOriginal(pointIndex) - originalBase);

                        float newDistance = 0.0f;
                        if (originalDistance != 0.0f)
                        {
                            // a range of 0.0f is invalid according to the spec (would result in a div by zero)
                            if (originalRange == 0.0f)
                            {
                                newDistance = originalDistance;
                            }
                            else
                            {
                                newDistance = originalDistance * currentRange / originalRange;
                            }
                        }

                        this.MovePoint(this.zp2, pointIndex, newDistance - currentDistance);
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.ALIGNRP:
                {
                    // FreeType Ins_ALIGNRP: bounds check rp0 first.
                    if ((uint)this.state.Rp0 >= (uint)this.zp0.Current.Length)
                    {
                        for (int i = 0; i < this.state.Loop; i++)
                        {
                            this.stack.Pop();
                        }

                        this.state.Loop = 1;
                        break;
                    }

                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        if ((uint)pointIndex >= (uint)this.zp1.Current.Length)
                        {
                            continue;
                        }

                        Vector2 p1 = this.zp1.GetCurrent(pointIndex);
                        Vector2 p2 = this.zp0.GetCurrent(this.state.Rp0);
                        this.MovePoint(this.zp1, pointIndex, -this.Project(p1 - p2));
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.ALIGNPTS:
                {
                    // FreeType Ins_ALIGNPTS: args[1] (top) = p2 in zp0, args[0] (deeper) = p1 in zp1.
                    int p2 = this.stack.Pop();
                    int p1 = this.stack.Pop();
                    if ((uint)p1 >= (uint)this.zp1.Current.Length ||
                        (uint)p2 >= (uint)this.zp0.Current.Length)
                    {
                        break;
                    }

                    float distance = this.Project(this.zp0.GetCurrent(p2) - this.zp1.GetCurrent(p1)) / 2;
                    this.MovePoint(this.zp1, p1, distance);
                    this.MovePoint(this.zp0, p2, -distance);
                }

                break;
                case OpCode.UTP:
                {
                    int pointIndex = this.stack.Pop();
                    if ((uint)pointIndex >= (uint)this.zp0.Current.Length)
                    {
                        break;
                    }

                    this.zp0.TouchState[pointIndex] &= ~this.GetTouchState();
                    break;
                }

                case OpCode.IUP0:
                case OpCode.IUP1:
                {
                    // FreeType: IUP returns immediately once both axes have been processed.
                    if (this.iupXCalled && this.iupYCalled)
                    {
                        break;
                    }

                    unsafe
                    {
                        // bail if no contours (empty outline)
                        if (this.contours.Count == 0)
                        {
                            break;
                        }

                        fixed (ControlPoint* currentPtr = this.points.Current)
                        {
                            fixed (ControlPoint* originalPtr = this.points.Original)
                            {
                                // opcode controls whether we care about X or Y direction
                                // do some pointer trickery so we can operate on the
                                // points in a direction-agnostic manner
                                TouchState touchMask;
                                byte* current;
                                byte* original;
                                if (opcode == OpCode.IUP0)
                                {
                                    this.iupYCalled = true;
                                    touchMask = TouchState.Y;
                                    current = (byte*)&currentPtr->Point.Y;
                                    original = (byte*)&originalPtr->Point.Y;
                                }
                                else
                                {
                                    this.iupXCalled = true;
                                    touchMask = TouchState.X;
                                    current = (byte*)&currentPtr->Point.X;
                                    original = (byte*)&originalPtr->Point.X;
                                }

                                int point = 0;
                                for (int i = 0; i < this.contours.Count; i++)
                                {
                                    ushort endPoint = this.contours[i];
                                    int firstPoint = point;
                                    int firstTouched = -1;
                                    int lastTouched = -1;

                                    for (; point <= endPoint; point++)
                                    {
                                        // check whether this point has been touched
                                        if ((this.points.TouchState[point] & touchMask) != 0)
                                        {
                                            // if this is the first touched point in the contour, note it and continue
                                            if (firstTouched < 0)
                                            {
                                                firstTouched = point;
                                                lastTouched = point;
                                                continue;
                                            }

                                            // otherwise, interpolate all untouched points
                                            // between this point and our last touched point
                                            InterpolatePoints(current, original, lastTouched + 1, point - 1, lastTouched, point);
                                            lastTouched = point;
                                        }
                                    }

                                    // check if we had any touched points at all in this contour
                                    if (firstTouched >= 0)
                                    {
                                        // there are two cases left to handle:
                                        // 1. there was only one touched point in the whole contour, in
                                        //    which case we want to shift everything relative to that one
                                        // 2. several touched points, in which case handle the gap from the
                                        //    beginning to the first touched point and the gap from the last
                                        //    touched point to the end of the contour
                                        if (lastTouched == firstTouched)
                                        {
                                            float delta = *GetPoint(current, lastTouched) - *GetPoint(original, lastTouched);
                                            if (delta != 0.0f)
                                            {
                                                for (int j = firstPoint; j < lastTouched; j++)
                                                {
                                                    *GetPoint(current, j) += delta;
                                                }

                                                for (int j = lastTouched + 1; j <= endPoint; j++)
                                                {
                                                    *GetPoint(current, j) += delta;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            InterpolatePoints(current, original, lastTouched + 1, endPoint, lastTouched, firstTouched);
                                            if (firstTouched > 0)
                                            {
                                                InterpolatePoints(current, original, firstPoint, firstTouched - 1, lastTouched, firstTouched);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    break;
                }

                case OpCode.ISECT:
                {
                    // move point P to the intersection of lines A and B
                    int ib1 = this.stack.Pop();
                    int ib0 = this.stack.Pop();
                    int ia1 = this.stack.Pop();
                    int ia0 = this.stack.Pop();
                    int index = this.stack.Pop();
                    if ((uint)ib0 >= (uint)this.zp0.Current.Length ||
                        (uint)ib1 >= (uint)this.zp0.Current.Length ||
                        (uint)ia0 >= (uint)this.zp1.Current.Length ||
                        (uint)ia1 >= (uint)this.zp1.Current.Length ||
                        (uint)index >= (uint)this.zp2.Current.Length)
                    {
                        break;
                    }

                    Vector2 b1 = this.zp0.GetCurrent(ib1);
                    Vector2 b0 = this.zp0.GetCurrent(ib0);
                    Vector2 a1 = this.zp1.GetCurrent(ia1);
                    Vector2 a0 = this.zp1.GetCurrent(ia0);

                    // calculate intersection using determinants: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection#Given_two_points_on_each_line
                    Vector2 da = a0 - a1;
                    Vector2 db = b0 - b1;
                    float den = (da.X * db.Y) - (da.Y * db.X);
                    if (Math.Abs(den) <= Epsilon)
                    {
                        // parallel lines; spec says to put the point "into the middle of the two lines"
                        this.zp2.Current[index].Point = (a0 + a1 + b0 + b1) / 4;
                    }
                    else
                    {
                        float t = (a0.X * a1.Y) - (a0.Y * a1.X);
                        float u = (b0.X * b1.Y) - (b0.Y * b1.X);
                        Vector2 p = new((t * db.X) - (da.X * u), (t * db.Y) - (da.Y * u));
                        this.zp2.Current[index].Point = p / den;
                    }

                    this.zp2.TouchState[index] = TouchState.Both;
                }

                break;

                // ==== STACK MANAGEMENT ====
                case OpCode.DUP:
                {
                    this.stack.Duplicate();
                    break;
                }

                case OpCode.POP:
                {
                    this.stack.Pop();
                    break;
                }

                case OpCode.CLEAR:
                {
                    this.stack.Clear();
                    break;
                }

                case OpCode.SWAP:
                {
                    this.stack.Swap();
                    break;
                }

                case OpCode.DEPTH:
                {
                    this.stack.Depth();
                    break;
                }

                case OpCode.CINDEX:
                {
                    this.stack.Copy();
                    break;
                }

                case OpCode.MINDEX:
                {
                    this.stack.Move();
                    break;
                }

                case OpCode.ROLL:
                {
                    this.stack.Roll();
                    break;
                }

                // ==== FLOW CONTROL ====
                case OpCode.IF:
                {
                    // value is false; jump to the next else block or endif marker
                    // otherwise, we don't have to do anything; we'll keep executing this block
                    if (!this.stack.PopBool())
                    {
                        int indent = 1;
                        while (indent > 0)
                        {
                            opcode = SkipNext(ref stream);
                            switch (opcode)
                            {
                                case OpCode.IF:
                                    indent++;
                                    break;
                                case OpCode.EIF:
                                    indent--;
                                    break;
                                case OpCode.ELSE:
                                    if (indent == 1)
                                    {
                                        indent = 0;
                                    }

                                    break;
                            }
                        }
                    }
                }

                break;
                case OpCode.ELSE:
                {
                    // assume we hit the true statement of some previous if block
                    // if we had hit false, we would have jumped over this
                    int indent = 1;
                    while (indent > 0)
                    {
                        opcode = SkipNext(ref stream);
                        switch (opcode)
                        {
                            case OpCode.IF:
                                indent++;
                                break;
                            case OpCode.EIF:
                                indent--;
                                break;
                        }
                    }
                }

                break;
                case OpCode.EIF: /* nothing to do */
                {
                    break;
                }

                case OpCode.JROT:
                case OpCode.JROF:
                {
                    if (this.stack.PopBool() == (opcode == OpCode.JROT))
                    {
                        int offset = this.stack.Pop();
                        if (offset < 0 && ++this.negJumpCounter > this.negJumpCounterMax)
                        {
                            return;
                        }

                        stream.Jump(offset - 1);
                    }
                    else
                    {
                        this.stack.Pop();    // ignore the offset
                    }
                }

                break;
                case OpCode.JMPR:
                {
                    int offset = this.stack.Pop();
                    if (offset < 0 && ++this.negJumpCounter > this.negJumpCounterMax)
                    {
                        // FreeType sets Execution_Too_Long error and returns.
                        return;
                    }

                    stream.Jump(offset - 1);
                    break;
                }

                // ==== LOGICAL OPS ====
                case OpCode.LT:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a < b);
                }

                break;
                case OpCode.LTEQ:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a <= b);
                }

                break;
                case OpCode.GT:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a > b);
                }

                break;
                case OpCode.GTEQ:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a >= b);
                }

                break;
                case OpCode.EQ:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a == b);
                }

                break;
                case OpCode.NEQ:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a != b);
                }

                break;
                case OpCode.AND:
                {
                    bool b = this.stack.PopBool();
                    bool a = this.stack.PopBool();
                    this.stack.Push(a && b);
                }

                break;
                case OpCode.OR:
                {
                    bool b = this.stack.PopBool();
                    bool a = this.stack.PopBool();
                    this.stack.Push(a || b);
                }

                break;
                case OpCode.NOT:
                {
                    this.stack.Push(!this.stack.PopBool());
                    break;
                }

                case OpCode.ODD:
                {
                    int value = (int)this.Round(this.stack.PopFloat());
                    this.stack.Push(value % 2 != 0);
                }

                break;
                case OpCode.EVEN:
                {
                    int value = (int)this.Round(this.stack.PopFloat());
                    this.stack.Push(value % 2 == 0);
                }

                break;

                // ==== ARITHMETIC ====
                case OpCode.ADD:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a + b);
                }

                break;
                case OpCode.SUB:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    this.stack.Push(a - b);
                }

                break;
                case OpCode.DIV:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    if (b == 0)
                    {
                        // FreeType sets Divide_By_Zero error and returns.
                        return;
                    }

                    long result = ((long)a << 6) / b;
                    this.stack.Push((int)result);
                }

                break;
                case OpCode.MUL:
                {
                    int b = this.stack.Pop();
                    int a = this.stack.Pop();
                    long result = ((long)a * b) >> 6;
                    this.stack.Push((int)result);
                }

                break;
                case OpCode.ABS:
                {
                    this.stack.Push(Math.Abs(this.stack.Pop()));
                    break;
                }

                case OpCode.NEG:
                {
                    this.stack.Push(-this.stack.Pop());
                    break;
                }

                case OpCode.FLOOR:
                {
                    this.stack.Push(this.stack.Pop() & ~63);
                    break;
                }

                case OpCode.CEILING:
                {
                    this.stack.Push((this.stack.Pop() + 63) & ~63);
                    break;
                }

                case OpCode.MAX:
                {
                    this.stack.Push(Math.Max(this.stack.Pop(), this.stack.Pop()));
                    break;
                }

                case OpCode.MIN:
                {
                    this.stack.Push(Math.Min(this.stack.Pop(), this.stack.Pop()));
                    break;
                }

                // ==== FUNCTIONS ====
                case OpCode.FDEF:
                {
                    if (!allowFunctionDefs || inFunction)
                    {
                        return;
                    }

                    this.functions[this.stack.Pop()] = stream.ToMemory();
                    while (SkipNext(ref stream) != OpCode.ENDF)
                    {
                    }
                }

                break;
                case OpCode.IDEF:
                {
                    if (!allowFunctionDefs || inFunction)
                    {
                        return;
                    }

                    this.instructionDefs[this.stack.Pop()] = stream.ToMemory();
                    while (SkipNext(ref stream) != OpCode.ENDF)
                    {
                    }
                }

                break;
                case OpCode.ENDF:
                {
                    if (!inFunction)
                    {
                        return;
                    }

                    return;
                }

                case OpCode.CALL:
                case OpCode.LOOPCALL:
                {
                    this.callStackSize++;
                    if (this.callStackSize > MaxCallStack)
                    {
                        // FreeType sets Stack_Overflow error and returns.
                        return;
                    }

                    int funcIndex = this.stack.Pop();
                    if ((uint)funcIndex >= (uint)this.functions.Length)
                    {
                        // FreeType sets Invalid_Reference error and returns.
                        return;
                    }

                    InstructionStream function = this.functions[funcIndex];
                    int count = opcode == OpCode.LOOPCALL ? this.stack.Pop() : 1;

                    // FreeType: only LOOPCALL increments the loopcall counter, not CALL.
                    if (opcode == OpCode.LOOPCALL)
                    {
                        this.loopcallCounter += count;
                        if (this.loopcallCounter > this.loopcallCounterMax)
                        {
                            // FreeType sets Execution_Too_Long error and returns.
                            return;
                        }
                    }

                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            this.Execute(function.ToStack(), true, false);
                        }
                    }

                    this.callStackSize--;
                }

                break;

                // ==== ROUNDING ====
                // we don't have "engine compensation" so the variants are unnecessary
                case OpCode.ROUND0:
                case OpCode.ROUND1:
                case OpCode.ROUND2:
                case OpCode.ROUND3:
                {
                    this.stack.Push(this.Round(this.stack.PopFloat()));
                    break;
                }

                case OpCode.NROUND0:
                case OpCode.NROUND1:
                case OpCode.NROUND2:
                case OpCode.NROUND3:
                {
                    break;
                }

                // ==== DELTA EXCEPTIONS ====
                case OpCode.DELTAC1:
                case OpCode.DELTAC2:
                case OpCode.DELTAC3:
                {
                    int last = this.stack.Pop();
                    for (int i = 1; i <= last; i++)
                    {
                        int cvtIndex = this.stack.Pop();
                        int arg = this.stack.Pop();

                        // upper 4 bits of the 8-bit arg is the relative ppem
                        // the opcode specifies the base to add to the ppem
                        int triggerPpem = (arg >> 4) & 0xF;
                        triggerPpem += (opcode - OpCode.DELTAC1) * 16;
                        triggerPpem += this.state.DeltaBase;

                        // if the current ppem matches the trigger, apply the exception
                        if (this.ppem == triggerPpem)
                        {
                            // the lower 4 bits of the arg is the amount to shift
                            // it's encoded such that 0 isn't an allowable value (who wants to shift by 0 anyway?)
                            int amount = (arg & 0xF) - 8;
                            if (amount >= 0)
                            {
                                amount++;
                            }

                            amount *= 1 << (6 - this.state.DeltaShift);

                            // update the CVT (FreeType non-pedantic: silently ignore out-of-bounds)
                            if ((uint)cvtIndex < (uint)this.controlValueTable.Length)
                            {
                                this.controlValueTable[cvtIndex] += F26Dot6ToFloat(amount);
                            }
                        }
                    }
                }

                break;
                case OpCode.DELTAP1:
                case OpCode.DELTAP2:
                case OpCode.DELTAP3:
                {
                    // SHPIX and DELTAP don't execute unless moving a composite on the
                    // y axis or moving a previously y touched point.
                    // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
                    bool postIUP = this.iupXCalled && this.iupYCalled;
                    bool composite = this.isComposite;
                    int last = this.stack.Pop();
                    for (int i = 1; i <= last; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        int arg = this.stack.Pop();
                        if ((uint)pointIndex >= (uint)this.zp0.Current.Length)
                        {
                            continue;
                        }

                        // upper 4 bits of the 8-bit arg is the relative ppem
                        // the opcode specifies the base to add to the ppem
                        int triggerPpem = (arg >> 4) & 0xF;
                        triggerPpem += this.state.DeltaBase;
                        if (opcode != OpCode.DELTAP1)
                        {
                            triggerPpem += (opcode - OpCode.DELTAP2 + 1) * 16;
                        }

                        // if the current ppem matches the trigger, apply the exception
                        if (this.ppem == triggerPpem)
                        {
                            // the lower 4 bits of the arg is the amount to shift
                            // it's encoded such that 0 isn't an allowable value (who wants to shift by 0 anyway?)
                            int amount = (arg & 0xF) - 8;
                            if (amount >= 0)
                            {
                                amount++;
                            }

                            amount *= 1 << (6 - this.state.DeltaShift);

                            // FreeType Ins_DELTAP: v40 backward compatibility gating.
                            bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
                            if (nativeClearType)
                            {
                                this.MovePoint(this.zp0, pointIndex, F26Dot6ToFloat(amount));
                            }
                            else
                            {
                                // Compat mode: gate on !postIUP AND (composite+freeY or Y-touched).
                                TouchState state = this.zp0.TouchState[pointIndex];
                                if (!postIUP &&
                                    ((composite && this.state.Freedom.Y != 0) ||
                                     ((state & TouchState.Y) == TouchState.Y)))
                                {
                                    this.MovePoint(this.zp0, pointIndex, F26Dot6ToFloat(amount));
                                }
                            }
                        }
                    }
                }

                break;

                // ==== MISCELLANEOUS ====
                case OpCode.DEBUG:
                {
                    this.stack.Pop();
                    break;
                }

                case OpCode.GETINFO:
                {
                    // FreeType Ins_GETINFO.
                    // Report v40 interpreter identity and ClearType capability flags.
                    int selector = this.stack.Pop();
                    int result = 0;

                    // Selector bit 0: interpreter version.
                    if ((selector & 0x1) != 0)
                    {
                        result = 40;
                    }

                    // Selector bits 1-2: rotation/stretching — always false in v40.

                    // Selector bit 3: variation glyph (FreeType Ins_GETINFO).
                    // Set result bit 10 when the font is a variable font instance.
                    if ((selector & 0x8) != 0 && this.normalizedAxisCoordinates is not null)
                    {
                        result |= 1 << 10;
                    }

                    // Selector bit 5: grayscale rendering.
                    // FreeType v40 sets grayscale = FALSE, so this bit is NOT set.

                    // Selector bit 6: subpixel hinting is available (v40 default).
                    if ((selector & 0x40) != 0)
                    {
                        result |= 1 << 13;
                    }

                    // Selector bit 10: subpixel positioned.
                    if ((selector & 0x400) != 0)
                    {
                        result |= 1 << 17;
                    }

                    // Selector bit 11: symmetrical smoothing.
                    if ((selector & 0x800) != 0)
                    {
                        result |= 1 << 18;
                    }

                    this.stack.Push(result);
                }

                break;

                case OpCode.GETVARIATION:
                {
                    // FreeType Ins_GETVARIATION.
                    // Push normalized axis coordinates as F2Dot14 integers.
                    // FreeType stores coords as F16Dot16 and does >> 2 to get F2Dot14.
                    // We store floats in [-1,1], so multiply by 16384 to get F2Dot14.
                    if (this.normalizedAxisCoordinates is not null)
                    {
                        for (int i = 0; i < this.normalizedAxisCoordinates.Length; i++)
                        {
                            this.stack.Push((int)Math.Round(this.normalizedAxisCoordinates[i] * 16384));
                        }
                    }

                    break;
                }

                case OpCode.GETDATA:
                {
                    // FreeType Ins_GETDATA.
                    // Always returns 17.
                    this.stack.Push(17);
                    break;
                }

                default:
                {
                    if (opcode >= OpCode.MIRP)
                    {
                        this.MoveIndirectRelative(opcode - OpCode.MIRP);
                    }
                    else if (opcode >= OpCode.MDRP)
                    {
                        this.MoveDirectRelative(opcode - OpCode.MDRP);
                    }
                    else
                    {
                        // check if this is a runtime-defined opcode
                        int index = (int)opcode;
                        if (index > this.instructionDefs.Length || !this.instructionDefs[index].IsValid)
                        {
                            // FreeType sets Invalid_Opcode error and terminates execution.
                            return;
                        }

                        this.callStackSize++;
                        if (this.callStackSize > MaxCallStack)
                        {
                            return;
                        }

                        this.Execute(this.instructionDefs[index].ToStack(), true, false);
                        this.callStackSize--;
                    }

                    break;
                }
            }

#if HINTING_TRACE
            this.TracePostInstruction(opcode, pops, pushes, preStackCount);
#endif
        }
    }

    /// <summary>
    /// Pops a CVT index from the stack and returns the corresponding value.
    /// Returns 0 for out-of-bounds indices (FreeType non-pedantic behavior).
    /// </summary>
    private float ReadCvt()
    {
        int loc = this.stack.Pop();
        if ((uint)loc >= (uint)this.controlValueTable.Length)
        {
            return 0;
        }

        return this.controlValueTable[loc];
    }

    /// <summary>
    /// Recomputes the cached dot product of the freedom and projection vectors.
    /// Must be called whenever either vector changes.
    /// </summary>
    private void OnVectorsUpdated()
    {
        this.fdotp = Vector2.Dot(this.state.Freedom, this.state.Projection);
        if (Math.Abs(this.fdotp) < Epsilon)
        {
            this.fdotp = 1.0f;
        }
    }

    /// <summary>
    /// Sets the freedom vector to one of the coordinate axes (SFVTCA).
    /// </summary>
    /// <param name="axis">0 for the Y-axis, 1 for the X-axis.</param>
    private void SetFreedomVectorToAxis(int axis)
    {
        this.state.Freedom = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
        this.OnVectorsUpdated();
    }

    /// <summary>
    /// Sets the projection and dual-projection vectors to one of the coordinate axes (SPVTCA).
    /// </summary>
    /// <param name="axis">0 for the Y-axis, 1 for the X-axis.</param>
    private void SetProjectionVectorToAxis(int axis)
    {
        this.state.Projection = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
        this.state.DualProjection = this.state.Projection;

        this.OnVectorsUpdated();
    }

    /// <summary>
    /// Sets a projection or freedom vector to the direction of a line between two points
    /// (SPVTL/SFVTL/SDPVTL). The mode's low bit selects the perpendicular direction.
    /// </summary>
    /// <param name="mode">0=SPVTL0, 1=SPVTL1, 2=SFVTL0, 3=SFVTL1.</param>
    /// <param name="dual">When <see langword="true"/>, also sets the dual-projection vector from original coordinates.</param>
    private void SetVectorToLine(int mode, bool dual)
    {
        int index1 = this.stack.Pop();
        int index2 = this.stack.Pop();
        Vector2 p1 = this.zp2.GetCurrent(index1);
        Vector2 p2 = this.zp1.GetCurrent(index2);

        Vector2 line = p2 - p1;
        if (line.LengthSquared() == 0)
        {
            // invalid; just set to whatever
            if (mode >= 2)
            {
                this.state.Freedom = Vector2.UnitX;
            }
            else
            {
                this.state.Projection = Vector2.UnitX;
                this.state.DualProjection = Vector2.UnitX;
            }
        }
        else
        {
            // if mode is 1 or 3, we want a perpendicular vector
            if ((mode & 0x1) != 0)
            {
                line = new Vector2(-line.Y, line.X);
            }

            line = Vector2.Normalize(line);

            if (mode >= 2)
            {
                this.state.Freedom = line;
            }
            else
            {
                this.state.Projection = line;
                this.state.DualProjection = line;
            }
        }

        // set the dual projection vector using original points
        if (dual)
        {
            p1 = this.zp2.GetOriginal(index1);
            p2 = this.zp1.GetOriginal(index2);
            line = p2 - p1;

            if (line.LengthSquared() == 0)
            {
                this.state.DualProjection = Vector2.UnitX;
            }
            else
            {
                if ((mode & 0x1) != 0)
                {
                    line = new Vector2(-line.Y, line.X);
                }

                this.state.DualProjection = Vector2.Normalize(line);
            }
        }

        this.OnVectorsUpdated();
    }

    /// <summary>
    /// Pops a zone index from the stack and returns the corresponding zone.
    /// Returns <see langword="false"/> for invalid indices (FreeType non-pedantic: silently ignores).
    /// </summary>
    private bool TryGetZoneFromStack(out Zone zone)
    {
        int zoneIndex = this.stack.Pop();
        switch (zoneIndex)
        {
            case 0:
                zone = this.twilight;
                return true;
            case 1:
                zone = this.points;
                return true;
            default:
                // FreeType non-pedantic: silently ignore invalid zone pointers.
                zone = default;
                return false;
        }
    }

    /// <summary>
    /// Configures super-rounding parameters from a packed mode byte (SROUND/S45ROUND).
    /// Bits 7-6 select the period multiplier, bits 5-4 the phase, and bits 3-0 the threshold.
    /// </summary>
    /// <param name="period">Base period: 1.0 for SROUND, sqrt(2)/2 for S45ROUND.</param>
    private void SetSuperRound(float period)
    {
        int mode = this.stack.Pop();
        this.roundPeriod = (mode & 0xC0) switch
        {
            0 => period / 2,
            0x40 => period,
            0x80 => period * 2,
            _ => period * 2, // Reserved; FreeType treats as period * 2.
        };

        // bits 5-4 are the phase
        switch (mode & 0x30)
        {
            case 0:
                this.roundPhase = 0;
                break;
            case 0x10:
                this.roundPhase = this.roundPeriod / 4;
                break;
            case 0x20:
                this.roundPhase = this.roundPeriod / 2;
                break;
            case 0x30:
                this.roundPhase = this.roundPeriod * 3 / 4;
                break;
        }

        // bits 3-0 are the threshold
        if ((mode & 0xF) == 0)
        {
            this.roundThreshold = this.roundPeriod - 1;
        }
        else
        {
            this.roundThreshold = ((mode & 0xF) - 4) * this.roundPeriod / 8;
        }
    }

    /// <summary>
    /// Move Indirect Relative Point (MIRP). Moves a point so that its distance from RP0
    /// matches a CVT value, subject to rounding, cut-in, and minimum distance constraints
    /// controlled by the instruction's flag bits.
    /// </summary>
    /// <param name="flags">MIRP flag bits: bit 4=set RP0, bit 3=minimum distance, bit 2=round, bits 1-0=engine compensation.</param>
    private void MoveIndirectRelative(int flags)
    {
        float cvt = this.ReadCvt();
        int pointIndex = this.stack.Pop();
        if ((uint)pointIndex >= (uint)this.zp1.Current.Length ||
            (uint)this.state.Rp0 >= (uint)this.zp0.Current.Length)
        {
            // FreeType Fail label: still sets reference points.
            this.state.Rp1 = this.state.Rp0;
            this.state.Rp2 = pointIndex;
            if ((flags & 0x10) != 0)
            {
                this.state.Rp0 = pointIndex;
            }

            return;
        }

        if (Math.Abs(cvt - this.state.SingleWidthValue) < this.state.SingleWidthCutIn)
        {
            if (cvt >= 0)
            {
                cvt = this.state.SingleWidthValue;
            }
            else
            {
                cvt = -this.state.SingleWidthValue;
            }
        }

        // if we're looking at the twilight zone we need to prepare the points there
        Vector2 originalReference = this.zp0.GetOriginal(this.state.Rp0);
        if (this.zp1.IsTwilight)
        {
            Vector2 initialValue = originalReference + (this.state.Freedom * cvt);
            this.zp1.Original[pointIndex].Point = initialValue;
            this.zp1.Current[pointIndex].Point = initialValue;
        }

        Vector2 point = this.zp1.GetCurrent(pointIndex);
        float originalDistance = this.DualProject(this.zp1.GetOriginal(pointIndex) - originalReference);
        float currentDistance = this.Project(point - this.zp0.GetCurrent(this.state.Rp0));

        if (this.state.AutoFlip && Math.Sign(originalDistance) != Math.Sign(cvt))
        {
            cvt = -cvt;
        }

        // if bit 2 is set, round the distance and look at the cut-in value
        float distance = cvt;
        if ((flags & 0x4) != 0)
        {
            // only perform cut-in tests when both points are in the same zone
            if (this.zp0.IsTwilight == this.zp1.IsTwilight && Math.Abs(cvt - originalDistance) > this.state.ControlValueCutIn)
            {
                cvt = originalDistance;
            }

            distance = this.Round(cvt);
        }

        // if bit 3 is set, constrain to the minimum distance
        if ((flags & 0x8) != 0)
        {
            if (originalDistance >= 0)
            {
                distance = Math.Max(distance, this.state.MinDistance);
            }
            else
            {
                distance = Math.Min(distance, -this.state.MinDistance);
            }
        }

        // move the point
        this.MovePoint(this.zp1, pointIndex, distance - currentDistance);
        this.state.Rp1 = this.state.Rp0;
        this.state.Rp2 = pointIndex;
        if ((flags & 0x10) != 0)
        {
            this.state.Rp0 = pointIndex;
        }
    }

    /// <summary>
    /// Move Direct Relative Point (MDRP). Moves a point so that its distance from RP0
    /// matches the original outline distance, subject to rounding and minimum distance
    /// constraints controlled by the instruction's flag bits.
    /// </summary>
    /// <param name="flags">MDRP flag bits: bit 4=set RP0, bit 3=minimum distance, bit 2=round, bits 1-0=engine compensation.</param>
    private void MoveDirectRelative(int flags)
    {
        int pointIndex = this.stack.Pop();
        if ((uint)pointIndex >= (uint)this.zp1.Current.Length ||
            (uint)this.state.Rp0 >= (uint)this.zp0.Current.Length)
        {
            // FreeType Fail label: still sets reference points.
            this.state.Rp1 = this.state.Rp0;
            this.state.Rp2 = pointIndex;
            if ((flags & 0x10) != 0)
            {
                this.state.Rp0 = pointIndex;
            }

            return;
        }

        Vector2 p1 = this.zp0.GetOriginal(this.state.Rp0);
        Vector2 p2 = this.zp1.GetOriginal(pointIndex);
        float originalDistance = this.DualProject(p2 - p1);

        // single width cut-in test
        if (Math.Abs(originalDistance - this.state.SingleWidthValue) < this.state.SingleWidthCutIn)
        {
            if (originalDistance >= 0)
            {
                originalDistance = this.state.SingleWidthValue;
            }
            else
            {
                originalDistance = -this.state.SingleWidthValue;
            }
        }

        // if bit 2 is set, perform rounding
        float distance = originalDistance;
        if ((flags & 0x4) != 0)
        {
            distance = this.Round(distance);
        }

        // if bit 3 is set, constrain to the minimum distance
        if ((flags & 0x8) != 0)
        {
            if (originalDistance >= 0)
            {
                distance = Math.Max(distance, this.state.MinDistance);
            }
            else
            {
                distance = Math.Min(distance, -this.state.MinDistance);
            }
        }

        // move the point
        originalDistance = this.Project(this.zp1.GetCurrent(pointIndex) - this.zp0.GetCurrent(this.state.Rp0));
        this.MovePoint(this.zp1, pointIndex, distance - originalDistance);
        this.state.Rp1 = this.state.Rp0;
        this.state.Rp2 = pointIndex;
        if ((flags & 0x10) != 0)
        {
            this.state.Rp0 = pointIndex;
        }
    }

    /// <summary>
    /// Computes the displacement vector for SHP/SHC/SHZ instructions by projecting the
    /// movement of the reference point (RP1 or RP2 depending on mode) from its original
    /// to its current position onto the freedom vector.
    /// </summary>
    /// <param name="mode">Opcode value; bit 0 selects RP1 in ZP0 (1) or RP2 in ZP1 (0).</param>
    /// <param name="zone">Receives the reference zone.</param>
    /// <param name="point">Receives the reference point index.</param>
    /// <param name="displacement">Receives the computed displacement vector.</param>
    /// <returns><see langword="true"/> if the reference point is valid; otherwise <see langword="false"/>.</returns>
    private bool TryComputeDisplacement(int mode, out Zone zone, out int point, out Vector2 displacement)
    {
        if ((mode & 1) == 0)
        {
            zone = this.zp1;
            point = this.state.Rp2;
        }
        else
        {
            zone = this.zp0;
            point = this.state.Rp1;
        }

        if ((uint)point >= (uint)zone.Current.Length)
        {
            displacement = default;
            return false;
        }

        float distance = this.Project(zone.GetCurrent(point) - zone.GetOriginal(point));
        displacement = distance * this.state.Freedom / this.fdotp;
        return true;
    }

    /// <summary>
    /// Returns the touch state flags corresponding to the current freedom vector axes.
    /// Used by UTP to selectively clear touch bits.
    /// </summary>
    private TouchState GetTouchState()
    {
        TouchState touch = TouchState.None;
        if (this.state.Freedom.X != 0)
        {
            touch = TouchState.X;
        }

        if (this.state.Freedom.Y != 0)
        {
            touch |= TouchState.Y;
        }

        return touch;
    }

    /// <summary>
    /// Moves a point along the freedom vector by the given distance, applying v40
    /// backward compatibility restrictions: X movement is always blocked in compat mode,
    /// Y movement is blocked only after both IUP passes have completed (post-IUP).
    /// Corresponds to FreeType's <c>Direct_Move</c> / <c>func_move</c>.
    /// </summary>
    private void MovePoint(Zone zone, int index, float distance)
    {
        // X is always blocked in backward compat mode.
        // Y is blocked only when backward_compatibility == 0x7 (post-IUP).
        bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
        bool postIUP = this.iupXCalled && this.iupYCalled;

        if (this.state.Freedom.X != 0)
        {
            if (nativeClearType)
            {
                float dx = distance * this.state.Freedom.X / this.fdotp;
                zone.Current[index].Point.X += dx;
            }

            zone.TouchState[index] |= TouchState.X;
        }

        if (this.state.Freedom.Y != 0)
        {
            if (nativeClearType || !postIUP)
            {
                float dy = distance * this.state.Freedom.Y / this.fdotp;
                zone.Current[index].Point.Y += dy;
            }

            zone.TouchState[index] |= TouchState.Y;
        }

#if HINTING_TRACE
        this.traceLog.AppendLine(System.FormattableString.Invariant($"  -> pt[{index}] = ({zone.Current[index].Point.X:F2}, {zone.Current[index].Point.Y:F2}) dist={distance:F2}"));
#endif
    }

    /// <summary>
    /// Moves a ZP2 point by explicit (dx, dy) deltas with the same v40 backward
    /// compatibility restrictions as <see cref="MovePoint"/>. Used by SHP, SHC, SHZ,
    /// and SHPIX where the displacement is pre-computed rather than derived from a scalar distance.
    /// Corresponds to FreeType's <c>Move_Zp2_Point</c>.
    /// </summary>
    private void MoveZp2Point(Zone zone, int index, float dx, float dy, bool touch)
    {
        // X is always blocked in compat mode.
        // Y is blocked only at backward_compatibility == 0x7 (post-IUP).
        bool nativeClearType = (this.state.InstructionControl & InstructionControlFlags.NativeClearType) != 0;
        bool postIUP = this.iupXCalled && this.iupYCalled;

        if (this.state.Freedom.X != 0)
        {
            if (nativeClearType)
            {
                zone.Current[index].Point.X += dx;
            }

            if (touch)
            {
                zone.TouchState[index] |= TouchState.X;
            }
        }

        if (this.state.Freedom.Y != 0)
        {
            if (nativeClearType || !postIUP)
            {
                zone.Current[index].Point.Y += dy;
            }

            if (touch)
            {
                zone.TouchState[index] |= TouchState.Y;
            }
        }

#if HINTING_TRACE
        this.traceLog.AppendLine(System.FormattableString.Invariant($"  -> zp2[{index}] = ({zone.Current[index].Point.X:F2}, {zone.Current[index].Point.Y:F2}) dx={dx:F2} dy={dy:F2}"));
#endif
    }

    /// <summary>
    /// Rounds a distance value according to the current round state.
    /// FreeType v40 uses zero engine compensation for all modes.
    /// </summary>
    private float Round(float value)
    {
        switch (this.state.RoundState)
        {
            case RoundMode.Off:
                // FreeType's Round_None with compensation = 0.
                return value;

            case RoundMode.ToGrid:
            {
                // Round_To_Grid with compensation = 0.
                if (value >= 0F)
                {
                    float val = (float)Math.Floor(value + 0.5F);
                    if (val < 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
                else
                {
                    float val = -(float)Math.Floor(-value + 0.5F);
                    if (val > 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
            }

            case RoundMode.ToHalfGrid:
            {
                // Round_To_Half_Grid with compensation = 0.
                if (value >= 0F)
                {
                    float val = (float)Math.Floor(value) + 0.5F;
                    if (val < 0F)
                    {
                        val = 0.5F;
                    }

                    return val;
                }
                else
                {
                    float val = -((float)Math.Floor(-value) + 0.5F);
                    if (val > 0F)
                    {
                        val = -0.5F;
                    }

                    return val;
                }
            }

            case RoundMode.DownToGrid:
            {
                // Round_Down_To_Grid with compensation = 0.
                if (value >= 0F)
                {
                    float val = (float)Math.Floor(value);
                    if (val < 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
                else
                {
                    float val = -(float)Math.Floor(-value);
                    if (val > 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
            }

            case RoundMode.UpToGrid:
            {
                // Round_Up_To_Grid with compensation = 0.
                if (value >= 0F)
                {
                    float val = (float)Math.Ceiling(value);
                    if (val < 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
                else
                {
                    float val = -(float)Math.Ceiling(-value);
                    if (val > 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
            }

            case RoundMode.ToDoubleGrid:
            {
                // Round_To_Double_Grid: grid step is 0.5 pixels.
                const float step = 0.5F;

                if (value >= 0F)
                {
                    float val = step * (float)Math.Floor((value / step) + 0.5F);
                    if (val < 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
                else
                {
                    float val = -step * (float)Math.Floor((-value / step) + 0.5F);
                    if (val > 0F)
                    {
                        val = 0F;
                    }

                    return val;
                }
            }

            case RoundMode.Super:
            case RoundMode.Super45:
            {
                // Round_Super / Round_Super_45 with compensation = 0.
                float period = this.roundPeriod;
                float phase = this.roundPhase;
                float threshold = this.roundThreshold;

                if (value >= 0F)
                {
                    float val = value - phase + threshold;
                    val = (float)Math.Floor(val / period) * period;
                    val += phase;

                    if (val < 0F)
                    {
                        val = phase;
                    }

                    return val;
                }
                else
                {
                    float val = -value - phase + threshold;
                    val = (float)Math.Floor(val / period) * period;
                    val = -val - phase;

                    if (val > 0F)
                    {
                        val = -phase;
                    }

                    return val;
                }
            }

            default:
                return value;
        }
    }

    /// <summary>Projects a point difference onto the projection vector.</summary>
    private float Project(Vector2 point) => Vector2.Dot(point, this.state.Projection);

    /// <summary>Projects a point difference onto the dual-projection vector (used for original coordinates).</summary>
    private float DualProject(Vector2 point) => Vector2.Dot(point, this.state.DualProjection);

    /// <summary>
    /// Reads and skips the next instruction in the stream, advancing past any inline
    /// data bytes for push instructions. Used by FDEF/IDEF to scan for ENDF and by
    /// IF/ELSE to skip over conditional blocks.
    /// </summary>
    private static OpCode SkipNext(ref StackInstructionStream stream)
    {
        OpCode opcode = stream.NextOpCode();
        switch (opcode)
        {
            case OpCode.NPUSHB:
            case OpCode.PUSHB1:
            case OpCode.PUSHB2:
            case OpCode.PUSHB3:
            case OpCode.PUSHB4:
            case OpCode.PUSHB5:
            case OpCode.PUSHB6:
            case OpCode.PUSHB7:
            case OpCode.PUSHB8:
            {
                int count = opcode == OpCode.NPUSHB ? stream.NextByte() : opcode - OpCode.PUSHB1 + 1;
                stream.Skip(count);
            }

            break;
            case OpCode.NPUSHW:
            case OpCode.PUSHW1:
            case OpCode.PUSHW2:
            case OpCode.PUSHW3:
            case OpCode.PUSHW4:
            case OpCode.PUSHW5:
            case OpCode.PUSHW6:
            case OpCode.PUSHW7:
            case OpCode.PUSHW8:
            {
                int count = opcode == OpCode.NPUSHW ? stream.NextByte() : opcode - OpCode.PUSHW1 + 1;
                stream.SkipWord(count);
            }

            break;
        }

        return opcode;
    }

    /// <summary>
    /// Interpolates untouched points between two reference points, preserving
    /// their relative positions in the original outline. Used by IUP.
    /// Operates on raw byte pointers to support direction-agnostic X/Y processing.
    /// </summary>
    private static unsafe void InterpolatePoints(byte* current, byte* original, int start, int end, int ref1, int ref2)
    {
        if (start > end)
        {
            return;
        }

        // figure out how much the two reference points
        // have been shifted from their original positions
        float delta1, delta2;
        float lower = *GetPoint(original, ref1);
        float upper = *GetPoint(original, ref2);
        if (lower > upper)
        {
            (upper, lower) = (lower, upper);

            delta1 = *GetPoint(current, ref2) - lower;
            delta2 = *GetPoint(current, ref1) - upper;
        }
        else
        {
            delta1 = *GetPoint(current, ref1) - lower;
            delta2 = *GetPoint(current, ref2) - upper;
        }

        float lowerCurrent = delta1 + lower;
        float upperCurrent = delta2 + upper;
        float scale = (upperCurrent - lowerCurrent) / (upper - lower);

        for (int i = start; i <= end; i++)
        {
            // three cases: if it's to the left of the lower reference point or to
            // the right of the upper reference point, do a shift based on that ref point.
            // otherwise, interpolate between the two of them
            float pos = *GetPoint(original, i);
            if (pos <= lower)
            {
                pos += delta1;
            }
            else if (pos >= upper)
            {
                pos += delta2;
            }
            else
            {
                pos = lowerCurrent + ((pos - lower) * scale);
            }

            *GetPoint(current, i) = pos;
        }
    }

    // Fixed-point conversion helpers.
    // F2Dot14: 2-bit integer + 14-bit fraction, range [-2, ~2). Used for unit vectors.
    // F26Dot6: 26-bit integer + 6-bit fraction. The native format for point coordinates
    // in the TrueType interpreter. Our implementation uses float throughout but converts
    // at the stack boundary to maintain compatibility with instruction semantics.
    private static float F2Dot14ToFloat(int value) => (short)value / 16384.0f;

    private static int FloatToF2Dot14(float value) => (int)(uint)(short)Math.Round(value * 16384.0f);

    private static float F26Dot6ToFloat(int value) => value / 64.0f;

    private static int FloatToF26Dot6(float value) => (int)Math.Round(value * 64.0f);

    private static unsafe float* GetPoint(byte* data, int index) => (float*)(data + (sizeof(ControlPoint) * index));

#if HINTING_TRACE
    private void TracePreInstruction(OpCode opcode, int pops)
    {
        System.Text.StringBuilder sb = this.traceLog;
        sb.Append(System.FormattableString.Invariant($"[{this.insCounter}] {opcode} (stk={this.stack.Count})"));

        // Show the top stack values that this instruction will consume.
        int available = Math.Min(pops, this.stack.Count);
        if (available > 0)
        {
            sb.Append(" args=[");
            for (int i = available - 1; i >= 0; i--)
            {
                if (i < available - 1)
                {
                    sb.Append(", ");
                }

                sb.Append(this.stack.Peek(i));
            }

            sb.Append(']');
        }

        sb.AppendLine();
    }

    private void TracePostInstruction(OpCode opcode, int pops, int pushes, int preStackCount)
    {
        int postStackCount = this.stack.Count;
        int expectedDelta = pushes - pops;
        int actualDelta = postStackCount - preStackCount;

        // Skip variable-pop/push instructions where PopPushCount is not authoritative.
        bool variablePop = opcode is
            OpCode.NPUSHB or OpCode.NPUSHW or
            OpCode.PUSHB1 or OpCode.PUSHB2 or OpCode.PUSHB3 or OpCode.PUSHB4 or
            OpCode.PUSHB5 or OpCode.PUSHB6 or OpCode.PUSHB7 or OpCode.PUSHB8 or
            OpCode.PUSHW1 or OpCode.PUSHW2 or OpCode.PUSHW3 or OpCode.PUSHW4 or
            OpCode.PUSHW5 or OpCode.PUSHW6 or OpCode.PUSHW7 or OpCode.PUSHW8 or
            OpCode.SHP0 or OpCode.SHP1 or
            OpCode.FLIPRGON or OpCode.FLIPRGOFF or
            OpCode.DELTAP1 or OpCode.DELTAP2 or OpCode.DELTAP3 or
            OpCode.DELTAC1 or OpCode.DELTAC2 or OpCode.DELTAC3 or
            OpCode.LOOPCALL or OpCode.CALL or
            OpCode.FDEF or OpCode.IDEF or
            OpCode.GETVARIATION or
            OpCode.ENDF or OpCode.AA;

        if (!variablePop && actualDelta != expectedDelta)
        {
            this.traceLog.AppendLine(
                System.FormattableString.Invariant(
                    $"  *** STACK IMBALANCE: expected delta={expectedDelta} (pop={pops} push={pushes}), actual delta={actualDelta} (pre={preStackCount} post={postStackCount})"));
        }
    }

    /// <summary>
    /// Gets the accumulated trace log for the most recent glyph hinting operation.
    /// Only available when compiled with the HINTING_TRACE constant.
    /// </summary>
    internal string GetTraceLog() => this.traceLog.ToString();
#endif

#pragma warning disable SA1201 // Elements should appear in the correct order
    /// <summary>
    /// Specifies the rounding mode used by the TrueType interpreter.
    /// </summary>
    private enum RoundMode
#pragma warning restore SA1201 // Elements should appear in the correct order
    {
        /// <summary>
        /// Round to the nearest half-grid line.
        /// </summary>
        ToHalfGrid,

        /// <summary>
        /// Round to the nearest grid line.
        /// </summary>
        ToGrid,

        /// <summary>
        /// Round to the nearest double-grid line.
        /// </summary>
        ToDoubleGrid,

        /// <summary>
        /// Round down to the nearest grid line.
        /// </summary>
        DownToGrid,

        /// <summary>
        /// Round up to the nearest grid line.
        /// </summary>
        UpToGrid,

        /// <summary>
        /// No rounding.
        /// </summary>
        Off,

        /// <summary>
        /// Super-rounding with a period of 1.0.
        /// </summary>
        Super,

        /// <summary>
        /// Super-rounding with a period of sqrt(2)/2.
        /// </summary>
        Super45
    }

    /// <summary>
    /// Flags controlling instruction execution behavior, set by the INSTCTRL instruction.
    /// </summary>
    [Flags]
    private enum InstructionControlFlags
    {
        /// <summary>
        /// No special instruction control.
        /// </summary>
        None,

        /// <summary>
        /// Inhibit grid fitting (disables hinting).
        /// </summary>
        InhibitGridFitting = 0x1,

        /// <summary>
        /// Use the default graphics state instead of the state saved by the prep program.
        /// </summary>
        UseDefaultGraphicsState = 0x2,

        /// <summary>
        /// Native ClearType mode is active.
        /// </summary>
        NativeClearType = 0x4
    }

    /// <summary>
    /// Tracks which axes a point has been touched (moved) along by hinting instructions.
    /// Used by IUP (Interpolate Untouched Points) to determine which points need interpolation.
    /// </summary>
    [Flags]
    private enum TouchState
    {
        /// <summary>
        /// The point has not been touched.
        /// </summary>
        None = 0,

        /// <summary>
        /// The point has been touched along the X axis.
        /// </summary>
        X = 0x1,

        /// <summary>
        /// The point has been touched along the Y axis.
        /// </summary>
        Y = 0x2,

        /// <summary>
        /// The point has been touched along both axes.
        /// </summary>
        Both = X | Y
    }

    /// <summary>
    /// An immutable snapshot of an instruction stream position, used to store function
    /// and instruction definitions (FDEF/IDEF) for later execution via CALL/LOOPCALL.
    /// </summary>
    private readonly struct InstructionStream
    {
        private readonly ReadOnlyMemory<byte> instructions;
        private readonly int ip;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstructionStream"/> struct.
        /// </summary>
        /// <param name="instructions">The instruction bytecode buffer.</param>
        /// <param name="offset">The byte offset into the buffer.</param>
        public InstructionStream(ReadOnlyMemory<byte> instructions, int offset)
        {
            this.instructions = instructions;
            this.ip = offset;
        }

        /// <summary>
        /// Gets a value indicating whether this stream references a valid instruction buffer.
        /// </summary>
        public bool IsValid => !this.instructions.IsEmpty;

        /// <summary>
        /// Creates a mutable <see cref="StackInstructionStream"/> positioned at this stream's offset.
        /// </summary>
        /// <returns>A new <see cref="StackInstructionStream"/>.</returns>
        public StackInstructionStream ToStack() => new(this.instructions, this.ip);
    }

    /// <summary>
    /// A mutable, stack-allocated instruction stream that reads TrueType bytecode
    /// sequentially and supports forward/backward jumps.
    /// </summary>
    private ref struct StackInstructionStream
    {
        private readonly ReadOnlyMemory<byte> origin;
        private readonly ReadOnlySpan<byte> instructions;
        private int ip;

        /// <summary>
        /// Initializes a new instance of the <see cref="StackInstructionStream"/> struct.
        /// </summary>
        /// <param name="instructions">The instruction bytecode buffer.</param>
        /// <param name="offset">The byte offset to start reading from.</param>
        public StackInstructionStream(ReadOnlyMemory<byte> instructions, int offset)
        {
            this.origin = instructions;
            this.instructions = instructions.Span;
            this.ip = offset;
        }

        /// <summary>
        /// Gets a value indicating whether this stream references a valid instruction buffer.
        /// </summary>
        public readonly bool IsValid => !this.instructions.IsEmpty;

        /// <summary>
        /// Gets a value indicating whether the instruction pointer has reached the end of the buffer.
        /// </summary>
        public readonly bool Done => this.ip >= this.instructions.Length;

        /// <summary>
        /// Reads the next byte from the stream and advances the instruction pointer.
        /// </summary>
        /// <returns>The byte value.</returns>
        public int NextByte()
        {
            ReadOnlySpan<byte> span = this.instructions;
            int offset = this.ip;
            if ((uint)offset >= (uint)span.Length)
            {
                ThrowEndOfInstructions();
            }

            byte b = span[offset];
            this.ip++;
            return b;
        }

        /// <summary>
        /// Skips the specified number of bytes in the stream.
        /// </summary>
        /// <param name="count">The number of bytes to skip.</param>
        public void Skip(int count)
        {
            this.ip += count;
            if ((uint)this.ip >= (uint)this.instructions.Length)
            {
                ThrowEndOfInstructions();
            }
        }

        /// <summary>
        /// Reads the next byte as an <see cref="OpCode"/>.
        /// </summary>
        /// <returns>The opcode.</returns>
        public OpCode NextOpCode() => (OpCode)this.NextByte();

        /// <summary>
        /// Reads the next two bytes as a signed 16-bit word (big-endian).
        /// </summary>
        /// <returns>The signed word value.</returns>
        public int NextWord() => (short)(ushort)((this.NextByte() << 8) | this.NextByte());

        /// <summary>
        /// Skips the specified number of 16-bit words in the stream.
        /// </summary>
        /// <param name="count">The number of words to skip.</param>
        public void SkipWord(int count) => this.Skip(count * 2);

        /// <summary>
        /// Moves the instruction pointer by the specified byte offset (can be negative for backward jumps).
        /// </summary>
        /// <param name="offset">The byte offset to jump.</param>
        public void Jump(int offset) => this.ip += offset;

        /// <summary>
        /// Creates an immutable <see cref="InstructionStream"/> snapshot at the current position.
        /// </summary>
        /// <returns>A new <see cref="InstructionStream"/>.</returns>
        public readonly InstructionStream ToMemory() => new(this.origin, this.ip);

        private static void ThrowEndOfInstructions() => throw new FontException("no more instructions");
    }

    /// <summary>
    /// Holds the TrueType graphics state registers used during instruction execution.
    /// This includes vector directions, rounding settings, reference points, and control flags.
    /// </summary>
    private struct GraphicsState
    {
        /// <summary>The freedom vector direction.</summary>
        public Vector2 Freedom;

        /// <summary>The dual projection vector, used for original outline measurements.</summary>
        public Vector2 DualProjection;

        /// <summary>The projection vector direction.</summary>
        public Vector2 Projection;

        /// <summary>The instruction control flags set by the INSTCTRL instruction.</summary>
        public InstructionControlFlags InstructionControl;

        /// <summary>The current rounding mode.</summary>
        public RoundMode RoundState;

        /// <summary>The minimum distance value (in pixels, F26Dot6).</summary>
        public float MinDistance;

        /// <summary>The control value cut-in threshold.</summary>
        public float ControlValueCutIn;

        /// <summary>The single width cut-in threshold.</summary>
        public float SingleWidthCutIn;

        /// <summary>The single width value.</summary>
        public float SingleWidthValue;

        /// <summary>The delta base value for DELTAP/DELTAC instructions.</summary>
        public int DeltaBase;

        /// <summary>The delta shift value for DELTAP/DELTAC instructions.</summary>
        public int DeltaShift;

        /// <summary>The loop variable controlling repeated instruction execution.</summary>
        public int Loop;

        /// <summary>Reference point 0.</summary>
        public int Rp0;

        /// <summary>Reference point 1.</summary>
        public int Rp1;

        /// <summary>Reference point 2.</summary>
        public int Rp2;

        /// <summary>Whether auto-flip is enabled for MIAP and MIRP instructions.</summary>
        public bool AutoFlip;

        /// <summary>
        /// Resets all graphics state fields to their default values.
        /// </summary>
        public void Reset()
        {
            this.Freedom = Vector2.UnitX;
            this.Projection = Vector2.UnitX;
            this.DualProjection = Vector2.UnitX;
            this.InstructionControl = InstructionControlFlags.None;
            this.RoundState = RoundMode.ToGrid;
            this.MinDistance = 1.0f;
            this.ControlValueCutIn = 17.0f / 16.0f;
            this.SingleWidthCutIn = 0.0f;
            this.SingleWidthValue = 0.0f;
            this.DeltaBase = 9;
            this.DeltaShift = 3;
            this.Loop = 1;
            this.Rp0 = this.Rp1 = this.Rp2 = 0;
            this.AutoFlip = true;
        }
    }

    /// <summary>
    /// Represents a point zone in the TrueType interpreter. There are two zones:
    /// the glyph zone (containing the glyph's outline points) and the twilight zone
    /// (containing points created by instructions for reference purposes).
    /// </summary>
    private struct Zone
    {
        /// <summary>The current (hinted) control points.</summary>
        public ControlPoint[] Current;

        /// <summary>The original (unhinted) control points.</summary>
        public ControlPoint[] Original;

        /// <summary>Per-point touch state tracking for IUP interpolation.</summary>
        public TouchState[] TouchState;

        /// <summary>Whether this is the twilight zone.</summary>
        public bool IsTwilight;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zone"/> struct for the twilight zone.
        /// </summary>
        /// <param name="maxTwilightPoints">The maximum number of twilight points.</param>
        /// <param name="isTwilight">Whether this is the twilight zone.</param>
        public Zone(int maxTwilightPoints, bool isTwilight)
        {
            this.IsTwilight = isTwilight;
            this.Current = new ControlPoint[maxTwilightPoints];
            this.Original = new ControlPoint[maxTwilightPoints];
            this.TouchState = new TouchState[maxTwilightPoints];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zone"/> struct for the glyph zone,
        /// copying the control points to create an original (unhinted) backup.
        /// </summary>
        /// <param name="controlPoints">The glyph's control points (used as current points; copied for originals).</param>
        /// <param name="isTwilight">Whether this is the twilight zone.</param>
        public Zone(ControlPoint[] controlPoints, bool isTwilight)
        {
            this.IsTwilight = isTwilight;
            this.Current = controlPoints;

            ControlPoint[] original = new ControlPoint[controlPoints.Length];
            controlPoints.AsSpan().CopyTo(original);
            this.Original = original;
            this.TouchState = new TouchState[controlPoints.Length];
        }

        /// <summary>
        /// Gets the current (hinted) position of the point at the specified index.
        /// </summary>
        /// <param name="index">The point index.</param>
        /// <returns>The current position.</returns>
        public readonly Vector2 GetCurrent(int index) => this.Current[index].Point;

        /// <summary>
        /// Gets the original (unhinted) position of the point at the specified index.
        /// </summary>
        /// <param name="index">The point index.</param>
        /// <returns>The original position.</returns>
        public readonly Vector2 GetOriginal(int index) => this.Original[index].Point;
    }

    /// <summary>
    /// A fixed-capacity integer stack used by the TrueType bytecode interpreter.
    /// Values are stored as 32-bit integers; F26Dot6 and F2Dot14 conversions are handled at push/pop time.
    /// </summary>
    private class ExecutionStack
    {
        private readonly int[] s;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionStack"/> class.
        /// </summary>
        /// <param name="maxStack">The maximum stack depth.</param>
        public ExecutionStack(int maxStack) => this.s = new int[maxStack];

        /// <summary>
        /// Gets the current number of elements on the stack.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the maximum capacity of the stack.
        /// </summary>
        public int Capacity => this.s.Length;

        /// <summary>
        /// Peeks at the top element without removing it.
        /// </summary>
        /// <returns>The top element value.</returns>
        public int Peek() => this.Peek(0);

        /// <summary>
        /// Pops the top element and returns it as a boolean (non-zero is <see langword="true"/>).
        /// </summary>
        /// <returns>The boolean value.</returns>
        public bool PopBool() => this.Pop() != 0;

        /// <summary>
        /// Pops the top element and converts it from F26Dot6 to a float.
        /// </summary>
        /// <returns>The float value.</returns>
        public float PopFloat() => F26Dot6ToFloat(this.Pop());

        /// <summary>
        /// Pushes a boolean value onto the stack (1 for <see langword="true"/>, 0 for <see langword="false"/>).
        /// </summary>
        /// <param name="value">The boolean value to push.</param>
        public void Push(bool value) => this.Push(value ? 1 : 0);

        /// <summary>
        /// Pushes a float value onto the stack, converting it to F26Dot6 format.
        /// </summary>
        /// <param name="value">The float value to push.</param>
        public void Push(float value) => this.Push(FloatToF26Dot6(value));

        /// <summary>
        /// Clears all elements from the stack.
        /// </summary>
        public void Clear() => this.Count = 0;

        /// <summary>
        /// Pushes the current stack depth onto the stack.
        /// </summary>
        public void Depth() => this.Push(this.Count);

        /// <summary>
        /// Duplicates the top element on the stack.
        /// </summary>
        public void Duplicate() => this.Push(this.Peek());

        /// <summary>
        /// Copies the element at the index specified by the top stack value.
        /// </summary>
        public void Copy() => this.Copy(this.Pop() - 1);

        /// <summary>
        /// Copies the element at the specified index (from top) and pushes it.
        /// </summary>
        /// <param name="index">The zero-based index from the top of the stack.</param>
        public void Copy(int index) => this.Push(this.Peek(index));

        /// <summary>
        /// Moves the element at the index specified by the top stack value to the top.
        /// </summary>
        public void Move() => this.Move(this.Pop() - 1);

        /// <summary>
        /// Rolls the top three elements (equivalent to Move(2)).
        /// </summary>
        public void Roll() => this.Move(2);

        /// <summary>
        /// Moves the element at the specified index to the top of the stack,
        /// shifting elements above it down by one position.
        /// </summary>
        /// <param name="index">The zero-based index from the top of the stack.</param>
        public void Move(int index)
        {
            int c = this.Count;
            int[] a = this.s;
            int val = this.Peek(index);
            for (int i = c - index - 1; i < c - 1; i++)
            {
                a[i] = a[i + 1];
            }

            a[c - 1] = val;
        }

        /// <summary>
        /// Swaps the top two elements on the stack.
        /// </summary>
        public void Swap()
        {
            int c = this.Count;
            if (c < 2)
            {
                ThrowStackOverflow();
            }

            int[] a = this.s;
            (a[c - 2], a[c - 1]) = (a[c - 1], a[c - 2]);
        }

        /// <summary>
        /// Pushes an integer value onto the stack.
        /// </summary>
        /// <param name="value">The integer value to push.</param>
        public void Push(int value)
        {
            if (this.Count == this.s.Length)
            {
                ThrowStackOverflow();
            }

            this.s[this.Count++] = value;
        }

        /// <summary>
        /// Pops and returns the top element from the stack.
        /// </summary>
        /// <returns>The popped integer value.</returns>
        public int Pop()
        {
            if (this.Count == 0)
            {
                ThrowStackOverflow();
            }

            return this.s[--this.Count];
        }

        /// <summary>
        /// Peeks at the element at the specified index from the top of the stack without removing it.
        /// </summary>
        /// <param name="index">The zero-based index from the top of the stack.</param>
        /// <returns>The integer value at the specified position.</returns>
        public int Peek(int index)
        {
            if (index < 0 || index >= this.Count)
            {
                ThrowStackOverflow();
            }

            return this.s[this.Count - index - 1];
        }

        private static void ThrowStackOverflow() => throw new FontException("stack overflow");
    }
}
