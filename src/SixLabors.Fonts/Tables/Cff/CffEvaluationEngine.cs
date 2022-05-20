// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SixLabors.Fonts.Tables.Cff
{
    /// <summary>
    /// Evaluates and translates the Type2 Instruction set for a given CFF glyph.
    /// </summary>
    internal static class CffEvaluationEngine
    {
        public static void Run(ref IGlyphRenderer renderer, ReadOnlySpan<Type2Instruction> instructions, Vector2 scale, Vector2 offset, FontRectangle bounds, GlyphRendererParameters parameters)
        {
            // TODO: There's likely no need to track these
            double currentX = 0;
            double currentY = 0;
            TransformingGlyphRenderer scalingGlyphRenderer = new(scale, offset, renderer);
            if (scalingGlyphRenderer.BeginGlyph(bounds, parameters))
            {
                Run(ref scalingGlyphRenderer, instructions, ref currentX, ref currentY);

                // Some CFF end without closing the latest contour.
                if (scalingGlyphRenderer.IsOpen)
                {
                    scalingGlyphRenderer.EndFigure();
                }
            }
        }

        private static void Run(ref TransformingGlyphRenderer renderer, ReadOnlySpan<Type2Instruction> instructionList, ref double currentX, ref double currentY)
        {
            using Type2EvaluationStack evalStack = new(renderer, currentX, currentY);
            for (int i = 0; i < instructionList.Length; ++i)
            {
                Type2Instruction inst = instructionList[i];

                // This part is our extension to the original
                int mergeFlags = inst.Op >> 6; // Upper 2 bits is our extension flags
                switch (mergeFlags)
                {
                    case 0: // Nothing
                        break;
                    case 1:
                        evalStack.Push(inst.Value);
                        break;
                    case 2:
                        evalStack.Push((short)(inst.Value >> 16));
                        evalStack.Push((short)(inst.Value >> 0));
                        break;
                    case 3:
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        evalStack.Push((sbyte)(inst.Value >> 0));
                        break;
                }

                // We use only 6 lower bits for op_name
                switch ((OperatorName)(inst.Op & 0b11_1111))
                {
                    default:
                        throw new NotSupportedException();
                    case OperatorName.GlyphWidth:

                        // TODO:
                        break;
                    case OperatorName.LoadInt:
                        evalStack.Push(inst.Value);
                        break;
                    case OperatorName.LoadSbyte4:
                        // 4 consecutive sbyte
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        evalStack.Push((sbyte)(inst.Value >> 0));
                        break;
                    case OperatorName.LoadSbyte3:
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        break;
                    case OperatorName.LoadShort2:
                        evalStack.Push((short)(inst.Value >> 16));
                        evalStack.Push((short)(inst.Value >> 0));
                        break;
                    case OperatorName.LoadFloat:
                        evalStack.Push(inst.ReadValueAsFixed1616());
                        break;
                    case OperatorName.endchar:
                        evalStack.EndChar();
                        break;
                    case OperatorName.flex:
                        evalStack.Flex();
                        break;
                    case OperatorName.hflex:
                        evalStack.H_Flex();
                        break;
                    case OperatorName.hflex1:
                        evalStack.H_Flex1();
                        break;
                    case OperatorName.flex1:
                        evalStack.Flex1();
                        break;

                    //-------------------------
                    // 4.4: Arithmetic Operators
                    case OperatorName.abs:
                        evalStack.Op_Abs();
                        break;
                    case OperatorName.add:
                        evalStack.Op_Add();
                        break;
                    case OperatorName.sub:
                        evalStack.Op_Sub();
                        break;
                    case OperatorName.div:
                        evalStack.Op_Div();
                        break;
                    case OperatorName.neg:
                        evalStack.Op_Neg();
                        break;
                    case OperatorName.random:
                        evalStack.Op_Random();
                        break;
                    case OperatorName.mul:
                        evalStack.Op_Mul();
                        break;
                    case OperatorName.sqrt:
                        evalStack.Op_Sqrt();
                        break;
                    case OperatorName.drop:
                        evalStack.Op_Drop();
                        break;
                    case OperatorName.exch:
                        evalStack.Op_Exch();
                        break;
                    case OperatorName.index:
                        evalStack.Op_Index();
                        break;
                    case OperatorName.roll:
                        evalStack.Op_Roll();
                        break;
                    case OperatorName.dup:
                        evalStack.Op_Dup();
                        break;

                    //-------------------------
                    // 4.5: Storage Operators
                    case OperatorName.put:
                        evalStack.Put();
                        break;
                    case OperatorName.get:
                        evalStack.Get();
                        break;

                    //-------------------------
                    // 4.6: Conditional
                    case OperatorName.and:
                        evalStack.Op_And();
                        break;
                    case OperatorName.or:
                        evalStack.Op_Or();
                        break;
                    case OperatorName.not:
                        evalStack.Op_Not();
                        break;
                    case OperatorName.eq:
                        evalStack.Op_Eq();
                        break;
                    case OperatorName.ifelse:
                        evalStack.Op_IfElse();
                        break;
                    case OperatorName.rlineto:
                        evalStack.R_LineTo();
                        break;
                    case OperatorName.hlineto:
                        evalStack.H_LineTo();
                        break;
                    case OperatorName.vlineto:
                        evalStack.V_LineTo();
                        break;
                    case OperatorName.rrcurveto:
                        evalStack.RR_CurveTo();
                        break;
                    case OperatorName.hhcurveto:
                        evalStack.HH_CurveTo();
                        break;
                    case OperatorName.hvcurveto:
                        evalStack.HV_CurveTo();
                        break;
                    case OperatorName.rcurveline:
                        evalStack.R_CurveLine();
                        break;
                    case OperatorName.rlinecurve:
                        evalStack.R_LineCurve();
                        break;
                    case OperatorName.vhcurveto:
                        evalStack.VH_CurveTo();
                        break;
                    case OperatorName.vvcurveto:
                        evalStack.VV_CurveTo();
                        break;
                    case OperatorName.rmoveto:
                        evalStack.R_MoveTo();
                        break;
                    case OperatorName.hmoveto:
                        evalStack.H_MoveTo();
                        break;
                    case OperatorName.vmoveto:
                        evalStack.V_MoveTo();
                        break;

                    //-------------------------------------------------------------------
                    // 4.3 Hint Operators
                    case OperatorName.hstem:
                        evalStack.H_Stem();
                        break;
                    case OperatorName.vstem:
                        evalStack.V_Stem();
                        break;
                    case OperatorName.vstemhm:
                        evalStack.V_StemHM();
                        break;
                    case OperatorName.hstemhm:
                        evalStack.H_StemHM();
                        break;
                    case OperatorName.hintmask1:
                        evalStack.HintMask1(inst.Value);
                        break;
                    case OperatorName.hintmask2:
                        evalStack.HintMask2(inst.Value);
                        break;
                    case OperatorName.hintmask3:
                        evalStack.HintMask3(inst.Value);
                        break;
                    case OperatorName.hintmask4:
                        evalStack.HintMask4(inst.Value);
                        break;
                    case OperatorName.hintmask_bits:
                        evalStack.HintMaskBits(inst.Value);
                        break;

                    //------------------------------
                    case OperatorName.cntrmask1:
                        evalStack.CounterSpaceMask1(inst.Value);
                        break;
                    case OperatorName.cntrmask2:
                        evalStack.CounterSpaceMask2(inst.Value);
                        break;
                    case OperatorName.cntrmask3:
                        evalStack.CounterSpaceMask3(inst.Value);
                        break;
                    case OperatorName.cntrmask4:
                        evalStack.CounterSpaceMask4(inst.Value);
                        break;
                    case OperatorName.cntrmask_bits:
                        evalStack.CounterSpaceMaskBits(inst.Value);
                        break;

                    //-------------------------
                    // 4.7: Subroutine Operators
                    case OperatorName._return:

                        // TODO: I don't think we need to actually track XY values here.
                        // Don't forget to return evalStack's currentX, currentY to prev eval context
                        currentX = evalStack.CurrentX;
                        currentY = evalStack.CurrentY;
                        evalStack.Ret();
                        break;

                    // Should not occur!-> since we replace this in parsing step
                    case OperatorName.callgsubr:
                    case OperatorName.callsubr:
                        throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Used to apply a transform against any glyphs rendered by the engine.
        /// </summary>
        private struct TransformingGlyphRenderer : IGlyphRenderer
        {
            private Vector2 scale;
            private Vector2 offset;
            private readonly IGlyphRenderer renderer;

            public TransformingGlyphRenderer(Vector2 scale, Vector2 offset, IGlyphRenderer renderer)
            {
                this.scale = scale;
                this.offset = offset;
                this.renderer = renderer;
                this.IsOpen = false;
            }

            public bool IsOpen { get; set; }

            public void BeginFigure()
            {
                this.IsOpen = true;
                this.renderer.BeginFigure();
            }

            public bool BeginGlyph(FontRectangle bounds, GlyphRendererParameters parameters)
            {
                this.IsOpen = true;
                return this.renderer.BeginGlyph(bounds, parameters);
            }

            public void BeginText(FontRectangle bounds)
            {
                this.IsOpen = true;
                this.renderer.BeginText(bounds);
            }

            public void EndFigure()
            {
                this.IsOpen = false;
                this.renderer.EndFigure();
            }

            public void EndGlyph()
            {
                this.IsOpen = false;
                this.renderer.EndGlyph();
            }

            public void EndText()
            {
                this.IsOpen = false;
                this.renderer.EndText();
            }

            public void LineTo(Vector2 point)
            {
                this.IsOpen = true;
                this.renderer.LineTo(this.Transform(point));
            }

            public void MoveTo(Vector2 point)
            {
                this.IsOpen = true;
                this.renderer.MoveTo(this.Transform(point));
            }

            public void CubicBezierTo(Vector2 secondControlPoint, Vector2 thirdControlPoint, Vector2 point)
            {
                this.IsOpen = true;
                this.renderer.CubicBezierTo(this.Transform(secondControlPoint), this.Transform(thirdControlPoint), this.Transform(point));
            }

            public void QuadraticBezierTo(Vector2 secondControlPoint, Vector2 point)
            {
                this.IsOpen = true;
                this.renderer.QuadraticBezierTo(this.Transform(secondControlPoint), this.Transform(point));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Vector2 Transform(Vector2 point) => (point * this.scale) + this.offset;
        }
    }
}
