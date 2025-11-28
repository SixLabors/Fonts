// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using SixLabors.Fonts.Tables.TrueType.Glyphs;

namespace SixLabors.Fonts.Tables.TrueType.Hinting;

/// <summary>
/// Code adapted from <see href="https://github.com/MikePopoloski/SharpFont/blob/b28555e8fae94c57f1b5ccd809cdd1260f0eb55f/SharpFont/FontFace.cs"/>
/// For further information
/// <see href="https://developer.apple.com/fonts/TrueType-Reference-Manual/RM05/Chap5.html"/>
/// <see href="https://learn.microsoft.com/en-us/typography/cleartype/truetypecleartype"/>
/// <see href="https://freetype.org/freetype2/docs/hinting/subpixel-hinting.html"/>
/// </summary>
internal class TrueTypeInterpreter
{
    private GraphicsState state;
    private GraphicsState cvtState;
    private readonly ExecutionStack stack;
    private readonly InstructionStream[] functions;
    private readonly InstructionStream[] instructionDefs;
    private float[] controlValueTable;
    private readonly int[] storage;
    private IReadOnlyList<ushort> contours;
    private float scale;
    private int ppem;
    private int callStackSize;
    private float fdotp;
    private float roundThreshold;
    private float roundPhase;
    private float roundPeriod;

    private bool iupXCalled;
    private bool iupYCalled;
    private bool isComposite;

    private Zone zp0;
    private Zone zp1;
    private Zone zp2;
    private Zone points;
    private Zone twilight;

    private static readonly float Sqrt2Over2 = (float)(Math.Sqrt(2) / 2);
    private const int MaxCallStack = 128;
    private const float Epsilon = 0.000001F;

    private readonly List<OpCode> debugList = new();

    public TrueTypeInterpreter(int maxStack, int maxStorage, int maxFunctions, int maxInstructionDefs, int maxTwilightPoints)
    {
        this.stack = new ExecutionStack(maxStack);
        this.storage = new int[maxStorage];
        this.functions = new InstructionStream[maxFunctions];
        this.instructionDefs = new InstructionStream[maxInstructionDefs > 0 ? 256 : 0];
        this.state = default;
        this.cvtState = default;
        this.twilight = new Zone(maxTwilightPoints, isTwilight: true);
        this.controlValueTable = Array.Empty<float>();
        this.contours = Array.Empty<ushort>();
    }

    public void InitializeFunctionDefs(byte[] instructions)
        => this.Execute(new StackInstructionStream(instructions, 0), false, true);

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

            // TODO: How about SIMD here? Will the JIT vectorize this?
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
            this.Execute(new StackInstructionStream(cvProgram, 0), false, false);

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
    }

    public void HintGlyph(
        ControlPoint[] controlPoints,
        IReadOnlyList<ushort> endPoints,
        ReadOnlyMemory<byte> instructions,
        bool isComposite)
    {
        if (instructions.Length == 0)
        {
            return;
        }

        // check if the CVT program disabled hinting
        if ((this.state.InstructionControl & InstructionControlFlags.InhibitGridFitting) != 0)
        {
            return;
        }

        // save contours and points
        this.contours = endPoints;
        this.zp0 = this.zp1 = this.zp2 = this.points = new Zone(controlPoints, isTwilight: false);

        // reset all of our shared state
        this.state = this.cvtState;
        this.callStackSize = 0;
        this.debugList.Clear();
        this.stack.Clear();
        this.OnVectorsUpdated();
        this.iupXCalled = false;
        this.iupYCalled = false;
        this.isComposite = isComposite;

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
    }

    private void Execute(StackInstructionStream stream, bool inFunction, bool allowFunctionDefs)
    {
        // dispatch each instruction in the stream
        while (!stream.Done)
        {
            OpCode opcode = stream.NextOpCode();
            this.debugList.Add(opcode);
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
                    int loc = CheckIndex(this.stack.Pop(), this.storage.Length);
                    this.stack.Push(this.storage[loc]);
                }

                break;
                case OpCode.WS:
                {
                    int value = this.stack.Pop();
                    int loc = CheckIndex(this.stack.Pop(), this.storage.Length);
                    this.storage[loc] = value;
                }

                break;

                // ==== CONTROL VALUE TABLE ====
                case OpCode.WCVTP:
                {
                    float value = this.stack.PopFloat();
                    int loc = CheckIndex(this.stack.Pop(), this.controlValueTable.Length);
                    this.controlValueTable[loc] = value;
                }

                break;
                case OpCode.WCVTF:
                {
                    int value = this.stack.Pop();
                    int loc = CheckIndex(this.stack.Pop(), this.controlValueTable.Length);
                    this.controlValueTable[loc] = value * this.scale;
                }

                break;
                case OpCode.RCVT:
                {
                    this.stack.Push(this.ReadCvt());
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
                    var vec = Vector2.Normalize(new Vector2(F2Dot14ToFloat(x), F2Dot14ToFloat(y)));
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
                    this.zp0 = this.GetZoneFromStack();
                    break;
                }

                case OpCode.SZP1:
                {
                    this.zp1 = this.GetZoneFromStack();
                    break;
                }

                case OpCode.SZP2:
                {
                    this.zp2 = this.GetZoneFromStack();
                    break;
                }

                case OpCode.SZPS:
                {
                    this.zp0 = this.zp1 = this.zp2 = this.GetZoneFromStack();
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
                    int selector = this.stack.Pop();
                    if (selector is >= 1 and <= 2)
                    {
                        // value is false if zero, otherwise shift the right bit into the flags
                        int bit = 1 << (selector - 1);
                        if (this.stack.Pop() == 0)
                        {
                            this.state.InstructionControl = (InstructionControlFlags)((int)this.state.InstructionControl & ~bit);
                        }
                        else
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
                    this.state.Loop = this.stack.Pop();
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
                    this.stack.Push(this.Project(this.zp2.GetCurrent(this.stack.Pop())));
                    break;
                }

                case OpCode.GC1:
                {
                    this.stack.Push(this.DualProject(this.zp2.GetOriginal(this.stack.Pop())));
                    break;
                }

                case OpCode.SCFS:
                {
                    float value = this.stack.PopFloat();
                    int index = this.stack.Pop();
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
                    Vector2 p1 = this.zp1.GetOriginal(this.stack.Pop());
                    Vector2 p2 = this.zp0.GetOriginal(this.stack.Pop());
                    this.stack.Push(this.DualProject(p2 - p1));
                }

                break;
                case OpCode.MD1:
                {
                    Vector2 p1 = this.zp1.GetCurrent(this.stack.Pop());
                    Vector2 p2 = this.zp0.GetCurrent(this.stack.Pop());
                    this.stack.Push(this.Project(p2 - p1));
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
                    // FLIPRGON, FLIPRGOFF, and FLIPPT don't execute post-IUP.  This
                    // prevents dents in e.g. Arial-Regular's `D' and `G' glyphs at
                    // various sizes.
                    // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
                    bool postIUP = this.iupXCalled && this.iupYCalled;
                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int index = this.stack.Pop();
                        if (postIUP)
                        {
                            continue;
                        }

                        this.points.Current[i].OnCurve ^= true;
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.FLIPRGON:
                {
                    // FLIPRGON, FLIPRGOFF, and FLIPPT don't execute post-IUP.  This
                    // prevents dents in e.g. Arial-Regular's `D' and `G' glyphs at
                    // various sizes.
                    // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
                    bool postIUP = this.iupXCalled && this.iupYCalled;
                    int end = this.stack.Pop();
                    for (int i = this.stack.Pop(); i <= end; i++)
                    {
                        if (postIUP)
                        {
                            continue;
                        }

                        this.points.Current[i].OnCurve = true;
                    }
                }

                break;
                case OpCode.FLIPRGOFF:
                {
                    // FLIPRGON, FLIPRGOFF, and FLIPPT don't execute post-IUP.  This
                    // prevents dents in e.g. Arial-Regular's `D' and `G' glyphs at
                    // various sizes.
                    // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
                    bool postIUP = this.iupXCalled && this.iupYCalled;
                    int end = this.stack.Pop();
                    for (int i = this.stack.Pop(); i <= end; i++)
                    {
                        if (postIUP)
                        {
                            continue;
                        }

                        this.points.Current[i].OnCurve = false;
                    }
                }

                break;
                case OpCode.SHP0:
                case OpCode.SHP1:
                {
                    Vector2 displacement = this.ComputeDisplacement((int)opcode, out Zone zone, out int point);
                    this.ShiftPoints(displacement);
                }

                break;
                case OpCode.SHPIX:
                {
                    this.ShiftPoints(this.stack.PopFloat() * this.state.Freedom);
                    break;
                }

                case OpCode.SHC0:
                case OpCode.SHC1:
                {
                    Vector2 displacement = this.ComputeDisplacement((int)opcode, out Zone zone, out int point);
                    TouchState touch = this.GetTouchState();
                    int contour = this.stack.Pop();
                    int start = contour == 0 ? 0 : this.contours[contour - 1] + 1;
                    int count = this.zp2.IsTwilight ? this.zp2.Current.Length : this.contours[contour] + 1;
                    ControlPoint[] current = this.zp2.Current;
                    TouchState[] states = this.zp2.TouchState;

                    for (int i = start; i < count; i++)
                    {
                        // Don't move the reference point
                        if (zone.Current != current || point != i)
                        {
                            current[i].Point += displacement;
                            states[i] |= touch;
                        }
                    }
                }

                break;
                case OpCode.SHZ0:
                case OpCode.SHZ1:
                {
                    Vector2 displacement = this.ComputeDisplacement((int)opcode, out Zone zone, out int point);
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
                            current[i].Point += displacement;
                        }
                    }
                }

                break;
                case OpCode.MIAP0:
                case OpCode.MIAP1:
                {
                    float distance = this.ReadCvt();
                    int pointIndex = this.stack.Pop();

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
                    int pointIndex = this.stack.Pop();
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
                    Vector2 originalBase = this.zp0.GetOriginal(this.state.Rp1);
                    Vector2 currentBase = this.zp0.GetCurrent(this.state.Rp1);
                    float originalRange = this.DualProject(this.zp1.GetOriginal(this.state.Rp2) - originalBase);
                    float currentRange = this.Project(this.zp1.GetCurrent(this.state.Rp2) - currentBase);

                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
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
                    for (int i = 0; i < this.state.Loop; i++)
                    {
                        int pointIndex = this.stack.Pop();
                        Vector2 p1 = this.zp1.GetCurrent(pointIndex);
                        Vector2 p2 = this.zp0.GetCurrent(this.state.Rp0);
                        this.MovePoint(this.zp1, pointIndex, -this.Project(p1 - p2));
                    }

                    this.state.Loop = 1;
                }

                break;
                case OpCode.ALIGNPTS:
                {
                    int p1 = this.stack.Pop();
                    int p2 = this.stack.Pop();
                    float distance = this.Project(this.zp0.GetCurrent(p2) - this.zp1.GetCurrent(p1)) / 2;
                    this.MovePoint(this.zp1, p1, distance);
                    this.MovePoint(this.zp0, p2, -distance);
                }

                break;
                case OpCode.UTP:
                {
                    this.zp0.TouchState[this.stack.Pop()] &= ~this.GetTouchState();
                    break;
                }

                case OpCode.IUP0:
                case OpCode.IUP1:
                {
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
                                    this.iupYCalled = true;
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
                    Vector2 b1 = this.zp0.GetCurrent(this.stack.Pop());
                    Vector2 b0 = this.zp0.GetCurrent(this.stack.Pop());
                    Vector2 a1 = this.zp1.GetCurrent(this.stack.Pop());
                    Vector2 a0 = this.zp1.GetCurrent(this.stack.Pop());
                    int index = this.stack.Pop();

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
                        stream.Jump(this.stack.Pop() - 1);
                    }
                    else
                    {
                        this.stack.Pop();    // ignore the offset
                    }
                }

                break;
                case OpCode.JMPR:
                {
                    stream.Jump(this.stack.Pop() - 1);
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
                    if (b == 0)
                    {
                        throw new InvalidOperationException("Division by zero.");
                    }

                    int a = this.stack.Pop();
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
                        throw new FontException("Can't define functions here.");
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
                        throw new FontException("Can't define functions here.");
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
                        throw new FontException("Found invalid ENDF marker outside of a function definition.");
                    }

                    return;
                }

                case OpCode.CALL:
                case OpCode.LOOPCALL:
                {
                    this.callStackSize++;
                    if (this.callStackSize > MaxCallStack)
                    {
                        throw new FontException("Stack overflow; infinite recursion?");
                    }

                    InstructionStream function = this.functions[this.stack.Pop()];
                    int count = opcode == OpCode.LOOPCALL ? this.stack.Pop() : 1;
                    for (int i = 0; i < count; i++)
                    {
                        this.Execute(function.ToStack(), true, false);
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

                            // update the CVT
                            CheckIndex(cvtIndex, this.controlValueTable.Length);
                            this.controlValueTable[cvtIndex] += F26Dot6ToFloat(amount);
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

                            // SHPIX and DELTAP don't execute unless moving a composite on the
                            // y axis or moving a previously y touched point.
                            TouchState state = this.zp0.TouchState[pointIndex];
                            if (!postIUP && ((composite && this.state.Freedom.Y != 0) || ((state & TouchState.Y) == TouchState.Y)))
                            {
                                this.MovePoint(this.zp0, pointIndex, F26Dot6ToFloat(amount));
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
                    int selector = this.stack.Pop();
                    int result = 0;
                    if ((selector & 0x1) != 0)
                    {
                        // pretend we are MS Rasterizer v35
                        result = 35;
                    }

                    // TODO: rotation and stretching
                    // if ((selector & 0x2) != 0)
                    // if ((selector & 0x4) != 0)

                    // we're always rendering in grayscale
                    if ((selector & 0x20) != 0)
                    {
                        result |= 1 << 12;
                    }

                    // TODO: ClearType flags
                    this.stack.Push(result);
                }

                break;

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
                            throw new FontException("Unknown opcode in font program.");
                        }

                        this.callStackSize++;
                        if (this.callStackSize > MaxCallStack)
                        {
                            throw new FontException("Stack overflow; infinite recursion?");
                        }

                        this.Execute(this.instructionDefs[index].ToStack(), true, false);
                        this.callStackSize--;
                    }

                    break;
                }
            }
        }
    }

    private static int CheckIndex(int index, int length)
    {
        Guard.MustBeBetweenOrEqualTo(index, 0, length - 1, nameof(index));
        return index;
    }

    private float ReadCvt() => this.controlValueTable[CheckIndex(this.stack.Pop(), this.controlValueTable.Length)];

    private void OnVectorsUpdated()
    {
        this.fdotp = Vector2.Dot(this.state.Freedom, this.state.Projection);
        if (Math.Abs(this.fdotp) < Epsilon)
        {
            this.fdotp = 1.0f;
        }
    }

    private void SetFreedomVectorToAxis(int axis)
    {
        this.state.Freedom = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
        this.OnVectorsUpdated();
    }

    private void SetProjectionVectorToAxis(int axis)
    {
        this.state.Projection = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
        this.state.DualProjection = this.state.Projection;

        this.OnVectorsUpdated();
    }

    private void SetVectorToLine(int mode, bool dual)
    {
        // mode here should be as follows:
        // 0: SPVTL0
        // 1: SPVTL1
        // 2: SFVTL0
        // 3: SFVTL1
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
            p2 = this.zp2.GetOriginal(index2);
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

    private Zone GetZoneFromStack()
        => this.stack.Pop() switch
        {
            0 => this.twilight,
            1 => this.points,
            _ => throw new FontException("Invalid zone pointer."),
        };

    private void SetSuperRound(float period)
    {
        // mode is a bunch of packed flags
        // bits 7-6 are the period multiplier
        int mode = this.stack.Pop();
        this.roundPeriod = (mode & 0xC0) switch
        {
            0 => period / 2,
            0x40 => period,
            0x80 => period * 2,
            _ => throw new FontException("Unknown rounding period multiplier."),
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

    private void MoveIndirectRelative(int flags)
    {
        // this instruction tries to make the current distance between a given point
        // and the reference point rp0 be equivalent to the same distance in the original outline
        // there are a bunch of flags that control how that distance is measured
        float cvt = this.ReadCvt();
        int pointIndex = this.stack.Pop();

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

    private void MoveDirectRelative(int flags)
    {
        // determine the original distance between the two reference points
        int pointIndex = this.stack.Pop();
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

    private Vector2 ComputeDisplacement(int mode, out Zone zone, out int point)
    {
        // compute displacement of the reference point
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

        float distance = this.Project(zone.GetCurrent(point) - zone.GetOriginal(point));
        return distance * this.state.Freedom / this.fdotp;
    }

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

    private void ShiftPoints(Vector2 displacement)
    {
        // SHPIX and DELTAP don't execute unless moving a composite on the
        // y axis or moving a previously y touched point.
        // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
        bool postIUP = this.iupXCalled && this.iupYCalled;
        bool composite = this.isComposite;
        ControlPoint[] current = this.zp2.Current;
        bool inTwilight = this.zp0.IsTwilight && this.zp1.IsTwilight && this.zp2.IsTwilight;

        for (int i = 0; i < this.state.Loop; i++)
        {
            // Special case: allow SHPIX to move points in the twilight zone.
            // Otherwise, treat SHPIX the same as DELTAP.  Unbreaks various
            // fonts such as older versions of Rokkitt and DTL Argo T Light
            // that would glitch severely after calling ALIGNRP after a
            // blocked SHPIX.
            int pointIndex = this.stack.Pop();
            ref TouchState state = ref this.zp2.TouchState[pointIndex];
            if (inTwilight || (!postIUP && ((composite && this.state.Freedom.Y != 0) || ((state & TouchState.Y) == TouchState.Y))))
            {
                // Copy FreeType Interpreter V40 and ignore instructions on the x-axis.
                // This prevents outline distortion on legacy fonts.
                // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
                current[pointIndex].Point.Y += displacement.Y;
                state |= TouchState.Y;
            }
        }

        this.state.Loop = 1;
    }

    private void MovePoint(Zone zone, int index, float distance)
    {
        if (this.isComposite)
        {
            Vector2 point = zone.GetCurrent(index) + (distance * this.state.Freedom / this.fdotp);
            TouchState touch = this.GetTouchState();
            zone.Current[index].Point = point;
            zone.TouchState[index] |= touch;
        }
        else
        {
            // Copy FreeType Interpreter V40 and ignore instructions on the x-axis.
            // This increases resolution on the x-axis and prevents glyph explosions on legacy fonts.
            // https://github.com/freetype/freetype/blob/3ab1875cd22536b3d715b3b104b7fb744b9c25c5/src/truetype/ttinterp.h#L298
            Vector2 point = zone.GetCurrent(index) + (distance * this.state.Freedom / this.fdotp);
            zone.Current[index].Point.Y = point.Y;
            zone.TouchState[index] |= TouchState.Y;
        }
    }

    private float Round(float value)
    {
        switch (this.state.RoundState)
        {
            case RoundMode.ToGrid:
                return value >= 0 ? (float)Math.Round(value) : -(float)Math.Round(-value);
            case RoundMode.ToHalfGrid:
                return value >= 0 ? (float)Math.Floor(value) + 0.5f : -((float)Math.Floor(-value) + 0.5f);
            case RoundMode.ToDoubleGrid:
                return value >= 0 ? (float)(Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2) : -(float)(Math.Round(-value * 2, MidpointRounding.AwayFromZero) / 2);
            case RoundMode.DownToGrid:
                return value >= 0 ? (float)Math.Floor(value) : -(float)Math.Floor(-value);
            case RoundMode.UpToGrid:
                return value >= 0 ? (float)Math.Ceiling(value) : -(float)Math.Ceiling(-value);
            case RoundMode.Super:
            case RoundMode.Super45:
                float result;
                if (value >= 0)
                {
                    result = value - this.roundPhase + this.roundThreshold;
                    result = (float)Math.Truncate(result / this.roundPeriod) * this.roundPeriod;
                    result += this.roundPhase;
                    if (result < 0)
                    {
                        result = this.roundPhase;
                    }
                }
                else
                {
                    result = -value - this.roundPhase + this.roundThreshold;
                    result = -(float)Math.Truncate(result / this.roundPeriod) * this.roundPeriod;
                    result -= this.roundPhase;
                    if (result > 0)
                    {
                        result = -this.roundPhase;
                    }
                }

                return result;

            default:
                return value;
        }
    }

    private float Project(Vector2 point) => Vector2.Dot(point, this.state.Projection);

    private float DualProject(Vector2 point) => Vector2.Dot(point, this.state.DualProjection);

    private static OpCode SkipNext(ref StackInstructionStream stream)
    {
        // grab the next opcode, and if it's one of the push instructions skip over its arguments
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

    private static float F2Dot14ToFloat(int value) => (short)value / 16384.0f;

    private static int FloatToF2Dot14(float value) => (int)(uint)(short)Math.Round(value * 16384.0f);

    private static float F26Dot6ToFloat(int value) => value / 64.0f;

    private static int FloatToF26Dot6(float value) => (int)Math.Round(value * 64.0f);

    private static unsafe float* GetPoint(byte* data, int index) => (float*)(data + (sizeof(ControlPoint) * index));

#pragma warning disable SA1201 // Elements should appear in the correct order
    private enum RoundMode
#pragma warning restore SA1201 // Elements should appear in the correct order
    {
        ToHalfGrid,
        ToGrid,
        ToDoubleGrid,
        DownToGrid,
        UpToGrid,
        Off,
        Super,
        Super45
    }

    [Flags]
    private enum InstructionControlFlags
    {
        None,
        InhibitGridFitting = 0x1,
        UseDefaultGraphicsState = 0x2
    }

    [Flags]
    private enum TouchState
    {
        None = 0,
        X = 0x1,
        Y = 0x2,
        Both = X | Y
    }

    private enum OpCode : byte
    {
        SVTCA0,
        SVTCA1,
        SPVTCA0,
        SPVTCA1,
        SFVTCA0,
        SFVTCA1,
        SPVTL0,
        SPVTL1,
        SFVTL0,
        SFVTL1,
        SPVFS,
        SFVFS,
        GPV,
        GFV,
        SFVTPV,
        ISECT,
        SRP0,
        SRP1,
        SRP2,
        SZP0,
        SZP1,
        SZP2,
        SZPS,
        SLOOP,
        RTG,
        RTHG,
        SMD,
        ELSE,
        JMPR,
        SCVTCI,
        SSWCI,
        SSW,
        DUP,
        POP,
        CLEAR,
        SWAP,
        DEPTH,
        CINDEX,
        MINDEX,
        ALIGNPTS,
        /* unused: 0x28 */
        UTP = 0x29,
        LOOPCALL,
        CALL,
        FDEF,
        ENDF,
        MDAP0,
        MDAP1,
        IUP0,
        IUP1,
        SHP0,
        SHP1,
        SHC0,
        SHC1,
        SHZ0,
        SHZ1,
        SHPIX,
        IP,
        MSIRP0,
        MSIRP1,
        ALIGNRP,
        RTDG,
        MIAP0,
        MIAP1,
        NPUSHB,
        NPUSHW,
        WS,
        RS,
        WCVTP,
        RCVT,
        GC0,
        GC1,
        SCFS,
        MD0,
        MD1,
        MPPEM,
        MPS,
        FLIPON,
        FLIPOFF,
        DEBUG,
        LT,
        LTEQ,
        GT,
        GTEQ,
        EQ,
        NEQ,
        ODD,
        EVEN,
        IF,
        EIF,
        AND,
        OR,
        NOT,
        DELTAP1,
        SDB,
        SDS,
        ADD,
        SUB,
        DIV,
        MUL,
        ABS,
        NEG,
        FLOOR,
        CEILING,
        ROUND0,
        ROUND1,
        ROUND2,
        ROUND3,
        NROUND0,
        NROUND1,
        NROUND2,
        NROUND3,
        WCVTF,
        DELTAP2,
        DELTAP3,
        DELTAC1,
        DELTAC2,
        DELTAC3,
        SROUND,
        S45ROUND,
        JROT,
        JROF,
        ROFF,
        /* unused: 0x7B */
        RUTG = 0x7C,
        RDTG,
        SANGW,
        AA,
        FLIPPT,
        FLIPRGON,
        FLIPRGOFF,
        /* unused: 0x83 - 0x84 */
        SCANCTRL = 0x85,
        SDPVTL0,
        SDPVTL1,
        GETINFO,
        IDEF,
        ROLL,
        MAX,
        MIN,
        SCANTYPE,
        INSTCTRL,
        /* unused: 0x8F - 0xAF */
        PUSHB1 = 0xB0,
        PUSHB2,
        PUSHB3,
        PUSHB4,
        PUSHB5,
        PUSHB6,
        PUSHB7,
        PUSHB8,
        PUSHW1,
        PUSHW2,
        PUSHW3,
        PUSHW4,
        PUSHW5,
        PUSHW6,
        PUSHW7,
        PUSHW8,
        MDRP, // range of 32 values, 0xC0 - 0xDF,
        MIRP = 0xE0 // range of 32 values, 0xE0 - 0xFF
    }

    private readonly struct InstructionStream
    {
        private readonly ReadOnlyMemory<byte> instructions;
        private readonly int ip;

        public InstructionStream(ReadOnlyMemory<byte> instructions, int offset)
        {
            this.instructions = instructions;
            this.ip = offset;
        }

        public bool IsValid => !this.instructions.IsEmpty;

        public StackInstructionStream ToStack() => new(this.instructions, this.ip);
    }

    private ref struct StackInstructionStream
    {
        private readonly ReadOnlyMemory<byte> origin;
        private readonly ReadOnlySpan<byte> instructions;
        private int ip;

        public StackInstructionStream(ReadOnlyMemory<byte> instructions, int offset)
        {
            this.origin = instructions;
            this.instructions = instructions.Span;
            this.ip = offset;
        }

        public readonly bool IsValid => !this.instructions.IsEmpty;

        public readonly bool Done => this.ip >= this.instructions.Length;

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

        public void Skip(int count)
        {
            this.ip += count;
            if ((uint)this.ip >= (uint)this.instructions.Length)
            {
                ThrowEndOfInstructions();
            }
        }

        public OpCode NextOpCode() => (OpCode)this.NextByte();

        public int NextWord() => (short)(ushort)((this.NextByte() << 8) | this.NextByte());

        public void SkipWord(int count) => this.Skip(count * 2);

        public void Jump(int offset) => this.ip += offset;

        public readonly InstructionStream ToMemory() => new(this.origin, this.ip);

        private static void ThrowEndOfInstructions() => throw new FontException("no more instructions");
    }

    private struct GraphicsState
    {
        public Vector2 Freedom;
        public Vector2 DualProjection;
        public Vector2 Projection;
        public InstructionControlFlags InstructionControl;
        public RoundMode RoundState;
        public float MinDistance;
        public float ControlValueCutIn;
        public float SingleWidthCutIn;
        public float SingleWidthValue;
        public int DeltaBase;
        public int DeltaShift;
        public int Loop;
        public int Rp0;
        public int Rp1;
        public int Rp2;
        public bool AutoFlip;

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

    private struct Zone
    {
        public ControlPoint[] Current;
        public ControlPoint[] Original;
        public TouchState[] TouchState;
        public bool IsTwilight;

        public Zone(int maxTwilightPoints, bool isTwilight)
        {
            this.IsTwilight = isTwilight;
            this.Current = new ControlPoint[maxTwilightPoints];
            this.Original = new ControlPoint[maxTwilightPoints];
            this.TouchState = new TouchState[maxTwilightPoints];
        }

        public Zone(ControlPoint[] controlPoints, bool isTwilight)
        {
            this.IsTwilight = isTwilight;
            this.Current = controlPoints;

            var original = new ControlPoint[controlPoints.Length];
            controlPoints.AsSpan().CopyTo(original);
            this.Original = original;
            this.TouchState = new TouchState[controlPoints.Length];
        }

        public readonly Vector2 GetCurrent(int index) => this.Current[index].Point;

        public readonly Vector2 GetOriginal(int index) => this.Original[index].Point;
    }

    private class ExecutionStack
    {
        private readonly int[] s;
        private int count;

        public ExecutionStack(int maxStack) => this.s = new int[maxStack];

        public int Peek() => this.Peek(0);

        public bool PopBool() => this.Pop() != 0;

        public float PopFloat() => F26Dot6ToFloat(this.Pop());

        public void Push(bool value) => this.Push(value ? 1 : 0);

        public void Push(float value) => this.Push(FloatToF26Dot6(value));

        public void Clear() => this.count = 0;

        public void Depth() => this.Push(this.count);

        public void Duplicate() => this.Push(this.Peek());

        public void Copy() => this.Copy(this.Pop() - 1);

        public void Copy(int index) => this.Push(this.Peek(index));

        public void Move() => this.Move(this.Pop() - 1);

        public void Roll() => this.Move(2);

        public void Move(int index)
        {
            int c = this.count;
            int[] a = this.s;
            int val = this.Peek(index);
            for (int i = c - index - 1; i < c - 1; i++)
            {
                a[i] = a[i + 1];
            }

            a[c - 1] = val;
        }

        public void Swap()
        {
            int c = this.count;
            if (c < 2)
            {
                ThrowStackOverflow();
            }

            int[] a = this.s;
            (a[c - 2], a[c - 1]) = (a[c - 1], a[c - 2]);
        }

        public void Push(int value)
        {
            if (this.count == this.s.Length)
            {
                ThrowStackOverflow();
            }

            this.s[this.count++] = value;
        }

        public int Pop()
        {
            if (this.count == 0)
            {
                ThrowStackOverflow();
            }

            return this.s[--this.count];
        }

        public int Peek(int index)
        {
            if (index < 0 || index >= this.count)
            {
                ThrowStackOverflow();
            }

            return this.s[this.count - index - 1];
        }

        private static void ThrowStackOverflow() => throw new FontException("stack overflow");
    }
}
