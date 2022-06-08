// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Decodes the commands and numbers making up a Type 2 CharString. A Type 2 CharString extends on the Type 1 CharString format.
    /// Compared to the Type 1 format, the Type 2 encoding offers smaller size and an opportunity for better rendering quality and
    /// performance. The Type 2 charstring operators are (with one exception) a superset of the Type 1 operators.
    /// </summary>
    /// <remarks>
    /// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
    /// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner
    /// </remarks>
    internal ref struct CffEvaluationEngine
    {
        private static readonly Random Random = new();
        private float? width;
        private int nStems;
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
        private readonly Dictionary<int, float> trans = new();
        private bool isDisposed;

        public CffEvaluationEngine(
            ReadOnlySpan<byte> charStrings,
            ReadOnlySpan<byte[]> globalSubrBuffers,
            ReadOnlySpan<byte[]> localSubrBuffers,
            int nominalWidthX)
        {
            this.transforming = default;
            this.charStrings = charStrings;
            this.globalSubrBuffers = globalSubrBuffers;
            this.localSubrBuffers = localSubrBuffers;
            this.nominalWidthX = nominalWidthX;

            this.globalBias = CalculateBias(this.globalSubrBuffers.Length);
            this.localBias = CalculateBias(this.localSubrBuffers.Length);

            this.x = 0;
            this.y = 0;
            this.width = null;
            this.nStems = 0;
            this.stack = new(50);
            this.isDisposed = false;
        }

        public Bounds GetBounds()
        {
            this.Reset();

            // TODO: It would be nice to avoid the allocation here.
            CffBoundsFinder finder = new();
            this.transforming = new(Vector2.One, Vector2.Zero, finder);

            // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
            this.Parse(this.charStrings);

            // Some CFF end without closing the latest contour.
            if (this.transforming.IsOpen)
            {
                this.transforming.EndFigure();
            }

            return finder.GetBounds();
        }

        public void RenderTo(IGlyphRenderer renderer, Vector2 scale, Vector2 offset)
        {
            this.Reset();

            this.transforming = new(scale, offset, renderer);

            // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
            this.Parse(this.charStrings);

            // Some CFF end without closing the latest contour.
            if (this.transforming.IsOpen)
            {
                this.transforming.EndFigure();
            }
        }

        private void Parse(ReadOnlySpan<byte> buffer)
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

                    var oneByteOperator = (Type2Operator1)b0;
                    switch (oneByteOperator)
                    {
                        case Type2Operator1.Hstem:
                        case Type2Operator1.Vstem:
                        case Type2Operator1.Hstemhm:
                        case Type2Operator1.Vstemhm:

                            this.ParseStems();
                            break;

                        case Type2Operator1.Vmoveto:

                            if (this.stack.Length > 1)
                            {
                                this.CheckWidth();
                            }

                            this.y += this.stack.Shift();
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

                            if (subr.Length > 0)
                            {
                                this.Parse(subr);
                            }

                            break;

                        case Type2Operator1.Return:

                            // TODO: CFF2
                            return;

                        case Type2Operator1.Endchar:

                            // TODO: CFF2
                            if (this.stack.Length > 0)
                            {
                                this.CheckWidth();
                            }

                            endCharEncountered = true;
                            break;

                        case Type2Operator1.Reserved15_:

                            // TODO: CFF2
                            break;
                        case Type2Operator1.Reserved16_:

                            // TODO: CFF2
                            break;
                        case Type2Operator1.Hintmask:
                        case Type2Operator1.Cntrmask:

                            this.ParseStems();
                            reader.Position += (this.nStems + 7) >> 3;

                            break;

                        case Type2Operator1.Rmoveto:

                            if (this.stack.Length > 2)
                            {
                                this.CheckWidth();
                            }

                            this.x += this.stack.Shift();
                            this.y += this.stack.Shift();
                            this.transforming.MoveTo(new Vector2(this.x, this.y));

                            this.stack.Clear();
                            break;

                        case Type2Operator1.Hmoveto:

                            if (this.stack.Length > 1)
                            {
                                this.CheckWidth();
                            }

                            this.x += this.stack.Shift();
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

                            if (subr.Length > 0)
                            {
                                this.Parse(subr);
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
                            if (twoByteOperator < 38)
                            {
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
                            }
                            else
                            {
                                ThrowInvalidOperator(twoByteOperator);
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

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.stack.Dispose();
            this.isDisposed = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateBias(int count)
        {
            if (count == 0)
            {
                return 0;
            }

            return (count < 1240) ? 107 : (count < 33900) ? 1131 : 32768;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ParseStems()
        {
            if (this.stack.Length % 2 != 0)
            {
                this.CheckWidth();
            }

            this.nStems += this.stack.Length >> 1;
            this.stack.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWidth()
        {
            if (this.width == null)
            {
                this.width = this.stack.Shift() + this.nominalWidthX;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            this.x = 0;
            this.y = 0;
            this.width = null;
            this.nStems = 0;
            this.stack.Clear();
            this.trans.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperator(byte @operator) => throw new InvalidFontFileException($"Unknown operator:{@operator}");
    }
}
