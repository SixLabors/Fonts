// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Numerics;
using System.Runtime.CompilerServices;
using SixLabors.Fonts.Rendering;
using SixLabors.Fonts.Tables.AdvancedTypographic.Variations;

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Decodes the commands and numbers making up a Type 2 CharString. A Type 2 CharString extends on the Type 1 CharString format.
/// Compared to the Type 1 format, the Type 2 encoding offers smaller size and an opportunity for better rendering quality and
/// performance. The Type 2 charstring operators are (with one exception) a superset of the Type 1 operators.
/// </summary>
/// <remarks>
/// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
/// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner.
/// </remarks>
internal ref struct CffEvaluationEngine
{
    // Appendix B of both Type 2 and CFF2 CharString specifications limits nested local and global
    // subroutine calls to 10, which also provides a fixed stack bound for malformed cyclic programs.
    private const int MaxSubroutineNesting = 10;

    private static readonly Random Random = new();
    private float? width;
    private int nStems;
    private List<float>? horizontalStemEdges;
    private List<float>? verticalStemEdges;
    private List<CffHintRegion>? hintRegions;
    private List<CffCounterMask>? counterMasks;
    private CffOutlineBuilder? pointSink;
    private int initialStemCount;
    private bool initialHintsActivated;
    private bool lockFixMapOk;
    private float x;
    private float y;
    private RefStack<float> stack;
    private readonly ReadOnlySpan<byte> charStrings;
    private readonly ReadOnlySpan<byte[]> globalSubrBuffers;
    private readonly ReadOnlySpan<byte[]> localSubrBuffers;
    private TransformingGlyphRenderer transforming;
    private readonly int nominalWidthX;
    private readonly int globalBias;
    private readonly int localBias;
    private readonly Dictionary<int, float> trans;
    private bool isDisposed;
    private readonly int version;
    private readonly GlyphVariationProcessor? glyphVariationProcessor;
    private int vsIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CffEvaluationEngine"/> struct.
    /// </summary>
    /// <param name="charStrings">The raw charstring byte data for the glyph.</param>
    /// <param name="globalSubrBuffers">The global subroutine buffers.</param>
    /// <param name="localSubrBuffers">The local subroutine buffers.</param>
    /// <param name="nominalWidthX">The nominal width used as a bias for charstring width values.</param>
    /// <param name="version">The CFF version (1 or 2).</param>
    /// <param name="itemVariationStore">The optional item variation store for CFF2 blend operations.</param>
    /// <param name="fVar">The optional font variations table.</param>
    /// <param name="aVar">The optional axis variations table.</param>
    /// <param name="vsIndex">The variation store index for blend operations.</param>
    public CffEvaluationEngine(
        ReadOnlySpan<byte> charStrings,
        ReadOnlySpan<byte[]> globalSubrBuffers,
        ReadOnlySpan<byte[]> localSubrBuffers,
        int nominalWidthX,
        int version,
        ItemVariationStore? itemVariationStore = null,
        FVarTable? fVar = null,
        AVarTable? aVar = null,
        int vsIndex = 0)
    {
        this.transforming = default;
        this.charStrings = charStrings;
        this.globalSubrBuffers = globalSubrBuffers;
        this.localSubrBuffers = localSubrBuffers;
        this.nominalWidthX = nominalWidthX;

        this.globalBias = CalculateBias(this.globalSubrBuffers.Length);
        this.localBias = CalculateBias(this.localSubrBuffers.Length);
        this.trans = [];

        this.x = 0;
        this.y = 0;
        this.width = null;
        this.nStems = 0;
        this.initialStemCount = 0;
        this.initialHintsActivated = false;
        this.lockFixMapOk = true;
        this.stack = new RefStack<float>(50);
        this.isDisposed = false;
        this.version = version;
        this.glyphVariationProcessor = null;

        if (itemVariationStore != null)
        {
            if (fVar is null)
            {
                throw new InvalidFontFileException("missing fVar table required for glyph variations processing");
            }

            this.glyphVariationProcessor = new GlyphVariationProcessor(itemVariationStore, fVar, aVar);
        }

        this.vsIndex = vsIndex;
    }

    /// <summary>
    /// Gets the number of stems that become active together at the first movement operator.
    /// Later declarations do not change this count.
    /// </summary>
    public int InitialStemCount => this.initialStemCount;

    /// <summary>
    /// Gets a value indicating whether GDI permits its post-lock overlap fixup for this
    /// charstring. Hint-substitution operators disable that fixup for the whole glyph.
    /// </summary>
    public bool LockFixMapOk => this.lockFixMapOk;

    /// <summary>
    /// Computes the bounding box of the glyph by evaluating the charstring program.
    /// </summary>
    /// <returns>The <see cref="Bounds"/> of the glyph.</returns>
    public Bounds GetBounds()
    {
        this.Reset();

        // TODO: It would be nice to avoid the allocation here.
        CffBoundsFinder finder = new();

        // Note: scale is passed with negative Y to flip the Y axis.
        this.transforming = new TransformingGlyphRenderer(finder, Vector2.Zero, new Vector2(1, -1), Vector2.Zero, Matrix3x2.Identity);

        // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
        this.Parse(this.charStrings, 0);

        // Some CFF end without closing the latest contour.
        if (this.transforming.IsOpen)
        {
            this.transforming.EndFigure();
        }

        return finder.GetBounds();
    }

    /// <summary>
    /// Evaluates the charstring program and renders the glyph outline to the specified renderer.
    /// </summary>
    /// <param name="renderer">The glyph renderer to output path operations to.</param>
    /// <param name="origin">The origin point for rendering.</param>
    /// <param name="scale">The scale factor to apply.</param>
    /// <param name="offset">The offset to apply.</param>
    /// <param name="transform">The transformation matrix to apply.</param>
    public void RenderTo(IGlyphRenderer renderer, Vector2 origin, Vector2 scale, Vector2 offset, Matrix3x2 transform)
    {
        this.Reset();

        this.transforming = new TransformingGlyphRenderer(renderer, origin, scale, offset, transform);

        // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
        this.Parse(this.charStrings, 0);

        // Some CFF end without closing the latest contour.
        if (this.transforming.IsOpen)
        {
            this.transforming.EndFigure();
        }
    }

    /// <summary>
    /// Parses and interprets a Type 2 charstring byte buffer, executing operators and accumulating operands.
    /// </summary>
    /// <param name="buffer">The charstring byte data to parse.</param>
    /// <param name="subroutineDepth">The number of active local and global subroutine calls.</param>
    private void Parse(ReadOnlySpan<byte> buffer, int subroutineDepth)
    {
        SimpleBinaryReader reader = new(buffer);
        bool endCharEncountered = false;
        while (!endCharEncountered && reader.CanRead())
        {
            byte b0 = reader.ReadByte();
            if (b0 < 32)
            {
                int index;
                ReadOnlySpan<byte> subr;
                bool phase;
                float c1x;
                float c1y;
                float c2x;
                float c2y;

                Type2Operator1 oneByteOperator = (Type2Operator1)b0;
                switch (oneByteOperator)
                {
                    case Type2Operator1.Hstem:

                        this.ParseStems(this.horizontalStemEdges);
                        break;

                    case Type2Operator1.Hstemhm:

                        // GDI clears lockFixMapOk whenever hstemhm is executed, including
                        // from a subroutine, before consuming the same stem operands.
                        this.lockFixMapOk = false;
                        this.ParseStems(this.horizontalStemEdges);
                        break;

                    case Type2Operator1.Vstem:

                        this.ParseStems(this.verticalStemEdges);
                        break;

                    case Type2Operator1.Vstemhm:

                        // vstemhm has the same glyph-wide effect as hstemhm in the native
                        // interpreter; plain vstem leaves the post-lock fixup enabled.
                        this.lockFixMapOk = false;
                        this.ParseStems(this.verticalStemEdges);
                        break;

                    case Type2Operator1.Vmoveto:

                        if (this.stack.Length > 1)
                        {
                            this.CheckWidth();
                        }

                        this.y += this.stack.Shift();
                        this.ActivateInitialHints();
                        this.transforming.MoveTo(new Vector2(this.x, this.y));

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Rlineto:

                        while (this.stack.Length >= 2)
                        {
                            this.x += this.stack.Shift();
                            this.y += this.stack.Shift();
                            this.transforming.LineTo(new Vector2(this.x, this.y));
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Hlineto:
                    case Type2Operator1.Vlineto:
                        phase = oneByteOperator == Type2Operator1.Hlineto;

                        while (this.stack.Length >= 1)
                        {
                            if (phase)
                            {
                                this.x += this.stack.Shift();
                            }
                            else
                            {
                                this.y += this.stack.Shift();
                            }

                            this.transforming.LineTo(new Vector2(this.x, this.y));
                            phase = !phase;
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Rrcurveto:

                        while (this.stack.Length > 0)
                        {
                            this.transforming.CubicBezierTo(
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()),
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()),
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()));
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Callsubr:
                        index = (int)this.stack.Pop() + this.localBias;
                        subr = this.localSubrBuffers[index];

                        // The over-limit call contributes no outline, matching how cyclic TrueType components
                        // degrade to empty while allowing the enclosing charstring to continue normally.
                        if (subr.Length > 0 && subroutineDepth < MaxSubroutineNesting)
                        {
                            this.Parse(subr, subroutineDepth + 1);
                        }

                        break;

                    case Type2Operator1.Return:

                        if (this.version >= 2)
                        {
                            break;
                        }

                        return;

                    case Type2Operator1.Endchar:

                        if (this.version >= 2)
                        {
                            break;
                        }

                        if (this.stack.Length > 0)
                        {
                            this.CheckWidth();
                        }

                        if (this.transforming.IsOpen)
                        {
                            this.transforming.EndFigure();
                        }

                        endCharEncountered = true;
                        break;

                    case Type2Operator1.VsIndex:
                        if (this.version < 2)
                        {
                            throw new NotSupportedException("blend operator is not supported in CFF v1");
                        }

                        this.vsIndex = (int)this.stack.Pop();
                        break;
                    case Type2Operator1.Blend:
                        if (this.version < 2)
                        {
                            throw new NotSupportedException("blend operator is not supported in CFF v1");
                        }

                        if (this.glyphVariationProcessor is null)
                        {
                            throw new NotSupportedException("blend operator in non-variation font");
                        }

                        float[] blendVector = this.glyphVariationProcessor.BlendVector(this.vsIndex);
                        float numBlends = this.stack.Pop();
                        float numOperands = numBlends * blendVector.Length;
                        int delta = this.stack.Length - (int)numOperands;
                        int basis = delta - (int)numBlends;

                        for (int i = 0; i < numBlends; i++)
                        {
                            float sum = this.stack[basis + i];
                            for (int j = 0; j < blendVector.Length; j++)
                            {
                                sum += blendVector[j] * this.stack[delta++];
                            }

                            this.stack[basis + i] = sum;
                        }

                        while (numOperands-- > 0)
                        {
                            this.stack.Pop();
                        }

                        break;

                    case Type2Operator1.Hintmask:
                    case Type2Operator1.Cntrmask:
                    {
                        // Operands pending when a mask operator arrives are implicit
                        // vertical stems whose vstem operator was elided.
                        this.ParseStems(this.verticalStemEdges);

                        if (oneByteOperator == Type2Operator1.Hintmask && this.initialHintsActivated)
                        {
                            // GDI permits an initial pre-path hintmask, but a hintmask
                            // encountered after the first movement disables map fixup.
                            this.lockFixMapOk = false;
                        }

                        int maskBytes = (this.nStems + 7) >> 3;

                        // A hintmask states which of the declared stems are live for the
                        // points that follow. Recording it against the current point count
                        // lets the fitter build one map per region rather than one map for
                        // the whole glyph, which is what makes crowded glyphs fit.
                        if (this.hintRegions is not null && this.pointSink is not null)
                        {
                            uint first = 0;
                            uint second = 0;
                            uint third = 0;
                            for (int maskByte = 0; maskByte < maskBytes; maskByte++)
                            {
                                byte value = reader.PeekAt(reader.Position + maskByte);
                                for (int bit = 0; bit < 8; bit++)
                                {
                                    int stem = (maskByte * 8) + bit;
                                    if ((value & (0x80 >> bit)) == 0)
                                    {
                                        continue;
                                    }

                                    // Type 2 mask bytes are most-significant-bit first, while the
                                    // fixed words retain declaration index as the bit index.
                                    if (stem < 32)
                                    {
                                        first |= 1U << stem;
                                    }
                                    else if (stem < 64)
                                    {
                                        second |= 1U << (stem - 32);
                                    }
                                    else if (stem < 96)
                                    {
                                        third |= 1U << (stem - 64);
                                    }
                                }
                            }

                            CffHintMask mask = new(first, second, third);

                            if (oneByteOperator == Type2Operator1.Hintmask)
                            {
                                this.hintRegions.Add(new CffHintRegion(this.pointSink.PointCount, mask, this.nStems));
                            }
                            else
                            {
                                this.counterMasks?.Add(new CffCounterMask(mask, this.nStems, this.counterMasks.Count));
                            }
                        }

                        reader.Position += maskBytes;

                        break;
                    }

                    case Type2Operator1.Rmoveto:

                        if (this.stack.Length > 2)
                        {
                            this.CheckWidth();
                        }

                        this.x += this.stack.Shift();
                        this.y += this.stack.Shift();
                        this.ActivateInitialHints();
                        this.transforming.MoveTo(new Vector2(this.x, this.y));

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Hmoveto:

                        if (this.stack.Length > 1)
                        {
                            this.CheckWidth();
                        }

                        this.x += this.stack.Shift();
                        this.ActivateInitialHints();
                        this.transforming.MoveTo(new Vector2(this.x, this.y));

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Rcurveline:

                        while (this.stack.Length >= 8)
                        {
                            this.transforming.CubicBezierTo(
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()),
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()),
                                new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()));
                        }

                        this.transforming.LineTo(new Vector2(this.x += this.stack.Shift(), this.y += this.stack.Shift()));

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Rlinecurve:

                        while (this.stack.Length >= 8)
                        {
                            this.x += this.stack.Shift();
                            this.y += this.stack.Shift();
                            this.transforming.LineTo(new Vector2(this.x, this.y));
                        }

                        c1x = this.x + this.stack.Shift();
                        c1y = this.y + this.stack.Shift();
                        c2x = c1x + this.stack.Shift();
                        c2y = c1y + this.stack.Shift();
                        this.x = c2x + this.stack.Shift();
                        this.y = c2y + this.stack.Shift();

                        this.transforming.CubicBezierTo(
                            new Vector2(c1x, c1y),
                            new Vector2(c2x, c2y),
                            new Vector2(this.x, this.y));

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Vvcurveto:

                        if (this.stack.Length % 2 != 0)
                        {
                            this.x += this.stack.Shift();
                        }

                        while (this.stack.Length >= 4)
                        {
                            c1x = this.x;
                            c1y = this.y + this.stack.Shift();
                            c2x = c1x + this.stack.Shift();
                            c2y = c1y + this.stack.Shift();
                            this.x = c2x;
                            this.y = c2y + this.stack.Shift();

                            this.transforming.CubicBezierTo(
                                new Vector2(c1x, c1y),
                                new Vector2(c2x, c2y),
                                new Vector2(this.x, this.y));
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Hhcurveto:

                        if (this.stack.Length % 2 != 0)
                        {
                            this.y += this.stack.Shift();
                        }

                        while (this.stack.Length >= 4)
                        {
                            c1x = this.x + this.stack.Shift();
                            c1y = this.y;
                            c2x = c1x + this.stack.Shift();
                            c2y = c1y + this.stack.Shift();
                            this.x = c2x + this.stack.Shift();
                            this.y = c2y;

                            this.transforming.CubicBezierTo(
                                new Vector2(c1x, c1y),
                                new Vector2(c2x, c2y),
                                new Vector2(this.x, this.y));
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Shortint:

                        this.stack.Push(reader.ReadInt16BE());
                        break;

                    case Type2Operator1.Callgsubr:

                        index = (int)this.stack.Pop() + this.globalBias;
                        subr = this.globalSubrBuffers[index];

                        // Local and global subroutines share the same nesting stack and therefore the same
                        // format limit and empty-outline fallback behavior.
                        if (subr.Length > 0 && subroutineDepth < MaxSubroutineNesting)
                        {
                            this.Parse(subr, subroutineDepth + 1);
                        }

                        break;

                    case Type2Operator1.Vhcurveto:
                    case Type2Operator1.Hvcurveto:

                        phase = oneByteOperator == Type2Operator1.Hvcurveto;
                        while (this.stack.Length >= 4)
                        {
                            if (phase)
                            {
                                c1x = this.x + this.stack.Shift();
                                c1y = this.y;
                                c2x = c1x + this.stack.Shift();
                                c2y = c1y + this.stack.Shift();
                                this.y = c2y + this.stack.Shift();
                                this.x = c2x + (this.stack.Length == 1 ? this.stack.Shift() : 0);
                            }
                            else
                            {
                                c1x = this.x;
                                c1y = this.y + this.stack.Shift();
                                c2x = c1x + this.stack.Shift();
                                c2y = c1y + this.stack.Shift();
                                this.x = c2x + this.stack.Shift();
                                this.y = c2y + (this.stack.Length == 1 ? this.stack.Shift() : 0);
                            }

                            this.transforming.CubicBezierTo(new Vector2(c1x, c1y), new Vector2(c2x, c2y), new Vector2(this.x, this.y));
                            phase = !phase;
                        }

                        this.stack.Clear();
                        break;

                    case Type2Operator1.Escape:

                        bool a;
                        bool b;
                        byte twoByteOperator = reader.ReadByte();
                        if (twoByteOperator >= 38)
                        {
                            ThrowInvalidOperator(twoByteOperator);
                            return;
                        }

                        switch ((Type2Operator2)twoByteOperator)
                        {
                            case Type2Operator2.And:

                                a = this.stack.Pop() != 0;
                                b = this.stack.Pop() != 0;
                                this.stack.Push((a && b) ? 1 : 0);
                                break;

                            case Type2Operator2.Or:

                                a = this.stack.Pop() != 0;
                                b = this.stack.Pop() != 0;
                                this.stack.Push((a || b) ? 1 : 0);
                                break;

                            case Type2Operator2.Not:

                                a = this.stack.Pop() != 0;
                                this.stack.Push(a ? 1 : 0);
                                break;

                            case Type2Operator2.Abs:

                                this.stack.Push(Math.Abs(this.stack.Pop()));
                                break;

                            case Type2Operator2.Add:

                                this.stack.Push(this.stack.Pop() + this.stack.Pop());
                                break;

                            case Type2Operator2.Sub:

                                this.stack.Push(this.stack.Pop() - this.stack.Pop());
                                break;

                            case Type2Operator2.Div:

                                this.stack.Push(this.stack.Pop() / this.stack.Pop());
                                break;

                            case Type2Operator2.Neg:

                                this.stack.Push(-this.stack.Pop());
                                break;

                            case Type2Operator2.Eq:

                                this.stack.Push(this.stack.Pop() == this.stack.Pop() ? 1 : 0);
                                break;

                            case Type2Operator2.Drop:

                                this.stack.Pop();
                                break;

                            case Type2Operator2.Put:

                                float val = this.stack.Pop();
                                int idx = (int)this.stack.Pop();

                                this.trans[idx] = val;
                                break;

                            case Type2Operator2.Get:

                                idx = (int)this.stack.Pop();
                                this.trans.TryGetValue(idx, out float v);
                                this.stack.Push(v);
                                this.trans.Remove(idx);
                                break;

                            case Type2Operator2.Ifelse:

                                float s1 = this.stack.Pop();
                                float s2 = this.stack.Pop();
                                float v1 = this.stack.Pop();
                                float v2 = this.stack.Pop();

                                this.stack.Push(v1 <= v2 ? s1 : s2);
                                break;

                            case Type2Operator2.Random:
                                this.stack.Push((float)Random.NextDouble());
                                break;

                            case Type2Operator2.Mul:

                                this.stack.Push(this.stack.Pop() * this.stack.Pop());
                                break;

                            case Type2Operator2.Sqrt:

                                this.stack.Push(MathF.Sqrt(this.stack.Pop()));
                                break;

                            case Type2Operator2.Dup:

                                float m = this.stack.Pop();
                                this.stack.Push(m);
                                this.stack.Push(m);
                                break;

                            case Type2Operator2.Exch:

                                float ex = this.stack.Pop();
                                float ch = this.stack.Pop();
                                this.stack.Push(ch);
                                this.stack.Push(ex);
                                break;

                            case Type2Operator2.Index:

                                idx = (int)this.stack.Pop();
                                if (idx < 0)
                                {
                                    idx = 0;
                                }
                                else if (idx > this.stack.Length - 1)
                                {
                                    idx = this.stack.Length - 1;
                                }

                                this.stack.Push(this.stack[idx]);
                                break;

                            case Type2Operator2.Roll:

                                int n = (int)this.stack.Pop();
                                float j = this.stack.Pop();

                                if (j >= 0)
                                {
                                    while (j > 0)
                                    {
                                        float t = this.stack[n - 1];
                                        for (int i = n - 2; i >= 0; i--)
                                        {
                                            this.stack[i + 1] = this.stack[i];
                                        }

                                        this.stack[0] = t;
                                        j--;
                                    }
                                }
                                else
                                {
                                    while (j < 0)
                                    {
                                        float t = this.stack[0];
                                        for (int i = 0; i <= n; i++)
                                        {
                                            this.stack[i] = this.stack[i + 1];
                                        }

                                        this.stack[n - 1] = t;
                                        j++;
                                    }
                                }

                                break;

                            case Type2Operator2.Hflex:

                                c1x = this.x + this.stack.Shift();
                                c1y = this.y;
                                c2x = c1x + this.stack.Shift();
                                c2y = c1y + this.stack.Shift();
                                float c3x = c2x + this.stack.Shift();
                                float c3y = c2y;
                                float c4x = c3x + this.stack.Shift();
                                float c4y = c3y;
                                float c5x = c4x + this.stack.Shift();
                                float c5y = c4y;
                                float c6x = c5x + this.stack.Shift();
                                float c6y = c5y;
                                this.x = c6x;
                                this.y = c6y;

                                this.transforming.CubicBezierTo(new Vector2(c1x, c1y), new Vector2(c2x, c2y), new Vector2(c3x, c3y));
                                this.transforming.CubicBezierTo(new Vector2(c4x, c4y), new Vector2(c5x, c5y), new Vector2(c6x, c6y));

                                this.stack.Clear();
                                break;

                            case Type2Operator2.Flex:

                                this.transforming.CubicBezierTo(new Vector2(this.stack.Shift(), this.stack.Shift()), new Vector2(this.stack.Shift(), this.stack.Shift()), new Vector2(this.stack.Shift(), this.stack.Shift()));
                                this.transforming.CubicBezierTo(new Vector2(this.stack.Shift(), this.stack.Shift()), new Vector2(this.stack.Shift(), this.stack.Shift()), new Vector2(this.stack.Shift(), this.stack.Shift()));

                                this.stack.Shift();

                                this.stack.Clear();
                                break;

                            case Type2Operator2.Hflex1:

                                c1x = this.x + this.stack.Shift();
                                c1y = this.y + this.stack.Shift();
                                c2x = c1x + this.stack.Shift();
                                c2y = c1y + this.stack.Shift();
                                c3x = c2x + this.stack.Shift();
                                c3y = c2y;
                                c4x = c3x + this.stack.Shift();
                                c4y = c3y;
                                c5x = c4x + this.stack.Shift();
                                c5y = c4y + this.stack.Shift();
                                c6x = c5x + this.stack.Shift();
                                c6y = c5y;
                                this.x = c6x;
                                this.y = c6y;

                                this.transforming.CubicBezierTo(new Vector2(c1x, c1y), new Vector2(c2x, c2y), new Vector2(c3x, c3y));
                                this.transforming.CubicBezierTo(new Vector2(c4x, c4y), new Vector2(c5x, c5y), new Vector2(c6x, c6y));

                                this.stack.Clear();
                                break;

                            case Type2Operator2.Flex1:

                                float startX = this.x;
                                float startY = this.y;

                                c1x = this.x + this.stack.Shift();
                                c1y = this.y + this.stack.Shift();

                                c2x = c1x + this.stack.Shift();
                                c2y = c1y + this.stack.Shift();

                                c3x = c2x + this.stack.Shift();
                                c3y = c2y + this.stack.Shift();

                                c4x = c3x + this.stack.Shift();
                                c4y = c3y + this.stack.Shift();

                                c5x = c4x + this.stack.Shift();
                                c5y = c4y + this.stack.Shift();

                                if (MathF.Abs(this.x - startX) > Math.Abs(this.y - startY))
                                {
                                    // horizontal
                                    c6x = c5x + this.stack.Shift();
                                    c6y = startY;
                                }
                                else
                                {
                                    c6x = startX;
                                    c6y = c5y + this.stack.Shift();
                                }

                                this.x = c6x;
                                this.y = c6y;

                                this.transforming.CubicBezierTo(new Vector2(c1x, c1y), new Vector2(c2x, c2y), new Vector2(c3x, c3y));
                                this.transforming.CubicBezierTo(new Vector2(c4x, c4y), new Vector2(c5x, c5y), new Vector2(c6x, c6y));

                                this.stack.Clear();
                                break;
                        }

                        break;
                }
            }
            else if (b0 < 247)
            {
                this.stack.Push(b0 - 139);
            }
            else if (b0 < 251)
            {
                byte b1 = reader.ReadByte();
                this.stack.Push(((b0 - 247) * 256) + b1 + 108);
            }
            else if (b0 < 255)
            {
                byte b1 = reader.ReadByte();
                this.stack.Push((-(b0 - 251) * 256) - b1 - 108);
            }
            else
            {
                this.stack.Push(reader.ReadFloatFixed1616());
            }
        }
    }

    /// <summary>
    /// Releases the resources used by the evaluation engine stack.
    /// </summary>
    public void Dispose()
    {
        if (this.isDisposed)
        {
            return;
        }

        this.stack.Dispose();
        this.isDisposed = true;
    }

    /// <summary>
    /// Calculates the subroutine bias based on the number of subroutines, as specified in the Type 2 charstring format.
    /// </summary>
    /// <param name="count">The number of subroutines in the INDEX.</param>
    /// <returns>The bias value to add to subroutine indices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateBias(int count)
    {
        if (count == 0)
        {
            return 0;
        }

        return (count < 1240) ? 107 : (count < 33900) ? 1131 : 32768;
    }

    /// <summary>
    /// Directs the engine to collect declared stem zones into the given lists during
    /// evaluation. Stem edges are recorded in charstring units as low and high pairs;
    /// ghost stems keep their negative widths so consumers can recognize edge hints.
    /// When not enabled, stem operators are consumed exactly as before at no cost.
    /// </summary>
    /// <param name="horizontal">Receives the horizontal stem zone edge pairs, in Y coordinates.</param>
    /// <param name="vertical">Receives the vertical stem zone edge pairs, in X coordinates.</param>
    /// <param name="regions">Receives the hint mask regions, each tagged with the point it starts at.</param>
    /// <param name="counters">Receives cntrmask events in declaration order, including the stem count at each operator.</param>
    /// <param name="sink">The outline builder whose point count tags each region.</param>
    public void CollectStems(List<float> horizontal, List<float> vertical, List<CffHintRegion> regions, List<CffCounterMask> counters, CffOutlineBuilder sink)
    {
        this.horizontalStemEdges = horizontal;
        this.verticalStemEdges = vertical;
        this.hintRegions = regions;
        this.counterMasks = counters;
        this.pointSink = sink;
    }

    /// <summary>
    /// Captures the implicit initial hint activation at the first movement operator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ActivateInitialHints()
    {
        if (!this.initialHintsActivated)
        {
            // GDI activates every stem declared when the first movement starts. Keeping
            // this count separate from later explicit masks preserves later declarations.
            this.initialStemCount = this.nStems;
            this.initialHintsActivated = true;
        }
    }

    /// <summary>
    /// Parses stem hint operators, consuming width if present and counting hint pairs.
    /// Operands encode each stem's low edge relative to the previous stem's high edge
    /// followed by its width, so absolute zones accumulate across the operand run.
    /// </summary>
    /// <param name="target">Receives decoded absolute edge pairs, or <see langword="null"/> when collection is disabled.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseStems(List<float>? target)
    {
        if (this.stack.Length % 2 != 0)
        {
            this.CheckWidth();
        }

        if (target is not null)
        {
            float running = 0F;
            for (int i = 0; i + 1 < this.stack.Length; i += 2)
            {
                float low = running + this.stack[i];
                float high = low + this.stack[i + 1];
                target.Add(low);
                target.Add(high);
                running = high;
            }
        }

        this.nStems += this.stack.Length >> 1;
        this.stack.Clear();
    }

    /// <summary>
    /// Checks whether a glyph width value is present at the bottom of the stack and consumes it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckWidth()
        => this.width ??= this.stack.Shift() + this.nominalWidthX;

    /// <summary>
    /// Resets the evaluation engine state for a new rendering pass.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Reset()
    {
        this.x = 0;
        this.y = 0;
        this.width = null;
        this.nStems = 0;
        this.initialStemCount = 0;
        this.initialHintsActivated = false;
        this.lockFixMapOk = true;
        this.stack.Clear();
        this.trans.Clear();
    }

    /// <summary>
    /// Throws an <see cref="InvalidFontFileException"/> for an unrecognized charstring operator.
    /// </summary>
    /// <param name="operator">The unrecognized operator byte value.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperator(byte @operator)
        => throw new InvalidFontFileException($"Unknown operator:{@operator}");
}
