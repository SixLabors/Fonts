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
        public static Bounds GetBounds(ReadOnlySpan<Type2Instruction> instructions, Vector2 scale, Vector2 offset)
        {
            // TODO: There's likely no need to track these
            double currentX = 0;
            double currentY = 0;
            var finder = CffBoundsFinder.Create();
            TransformingGlyphRenderer scalingGlyphRenderer = new(scale, offset, finder);

            // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
            RenderTo(ref scalingGlyphRenderer, instructions, ref currentX, ref currentY);

            // Some CFF end without closing the latest contour.
            if (scalingGlyphRenderer.IsOpen)
            {
                scalingGlyphRenderer.EndFigure();
            }

            return finder.GetBounds();
        }

        public static void RenderTo(ref IGlyphRenderer renderer, ReadOnlySpan<Type2Instruction> instructions, Vector2 scale, Vector2 offset)
        {
            // TODO: There's likely no need to track these
            double currentX = 0;
            double currentY = 0;
            TransformingGlyphRenderer scalingGlyphRenderer = new(scale, offset, renderer);

            // Boolean IGlyphRenderer.BeginGlyph(..) is handled by the caller.
            RenderTo(ref scalingGlyphRenderer, instructions, ref currentX, ref currentY);

            // Some CFF end without closing the latest contour.
            if (scalingGlyphRenderer.IsOpen)
            {
                scalingGlyphRenderer.EndFigure();
            }
        }

        private static void RenderTo(ref TransformingGlyphRenderer renderer, ReadOnlySpan<Type2Instruction> instructionList, ref double currentX, ref double currentY)
        {
            using Type2EvaluationStack evalStack = new(renderer, currentX, currentY);
            for (int i = 0; i < instructionList.Length; ++i)
            {
                Type2Instruction instruction = instructionList[i];

                // TODO: What is this? I can't figure why it exists.
                // This part is our extension to the original
                int mergeFlags = instruction.Operator >> 6; // Upper 2 bits is our extension flags
                switch (mergeFlags)
                {
                    case 0: // Nothing
                        break;
                    case 1:
                        evalStack.Push(instruction.Value);
                        break;
                    case 2:
                        evalStack.Push((short)(instruction.Value >> 16));
                        evalStack.Push((short)(instruction.Value >> 0));
                        break;
                    case 3:
                        evalStack.Push((sbyte)(instruction.Value >> 24));
                        evalStack.Push((sbyte)(instruction.Value >> 16));
                        evalStack.Push((sbyte)(instruction.Value >> 8));
                        evalStack.Push((sbyte)(instruction.Value >> 0));
                        break;
                }

                // We use only 6 lower bits for op_name
                switch ((Type2InstructionKind)(instruction.Operator & 0b11_1111))
                {
                    default:
                        throw new NotSupportedException();
                    case Type2InstructionKind.GlyphWidth:

                        // TODO:
                        break;
                    case Type2InstructionKind.LoadInt:
                        evalStack.Push(instruction.Value);
                        break;
                    case Type2InstructionKind.LoadSbyte4:
                        // 4 consecutive sbyte
                        evalStack.Push((sbyte)(instruction.Value >> 24));
                        evalStack.Push((sbyte)(instruction.Value >> 16));
                        evalStack.Push((sbyte)(instruction.Value >> 8));
                        evalStack.Push((sbyte)(instruction.Value >> 0));
                        break;
                    case Type2InstructionKind.LoadSbyte3:
                        evalStack.Push((sbyte)(instruction.Value >> 24));
                        evalStack.Push((sbyte)(instruction.Value >> 16));
                        evalStack.Push((sbyte)(instruction.Value >> 8));
                        break;
                    case Type2InstructionKind.LoadShort2:
                        evalStack.Push((short)(instruction.Value >> 16));
                        evalStack.Push((short)(instruction.Value >> 0));
                        break;
                    case Type2InstructionKind.LoadFloat:
                        evalStack.Push(instruction.ReadValueAsFixed1616());
                        break;
                    case Type2InstructionKind.Endchar:
                        evalStack.EndChar();
                        break;
                    case Type2InstructionKind.Flex:
                        evalStack.Flex();
                        break;
                    case Type2InstructionKind.Hflex:
                        evalStack.H_Flex();
                        break;
                    case Type2InstructionKind.Hflex1:
                        evalStack.H_Flex1();
                        break;
                    case Type2InstructionKind.Flex1:
                        evalStack.Flex1();
                        break;

                    //-------------------------
                    // 4.4: Arithmetic Operators
                    case Type2InstructionKind.Abs:
                        evalStack.Op_Abs();
                        break;
                    case Type2InstructionKind.Add:
                        evalStack.Op_Add();
                        break;
                    case Type2InstructionKind.Sub:
                        evalStack.Op_Sub();
                        break;
                    case Type2InstructionKind.Div:
                        evalStack.Op_Div();
                        break;
                    case Type2InstructionKind.Neg:
                        evalStack.Op_Neg();
                        break;
                    case Type2InstructionKind.Random:
                        evalStack.Op_Random();
                        break;
                    case Type2InstructionKind.Mul:
                        evalStack.Op_Mul();
                        break;
                    case Type2InstructionKind.Sqrt:
                        evalStack.Op_Sqrt();
                        break;
                    case Type2InstructionKind.Drop:
                        evalStack.Op_Drop();
                        break;
                    case Type2InstructionKind.Exch:
                        evalStack.Op_Exch();
                        break;
                    case Type2InstructionKind.Index:
                        evalStack.Op_Index();
                        break;
                    case Type2InstructionKind.Roll:
                        evalStack.Op_Roll();
                        break;
                    case Type2InstructionKind.Dup:
                        evalStack.Op_Dup();
                        break;

                    //-------------------------
                    // 4.5: Storage Operators
                    case Type2InstructionKind.Put:
                        evalStack.Put();
                        break;
                    case Type2InstructionKind.Get:
                        evalStack.Get();
                        break;

                    //-------------------------
                    // 4.6: Conditional
                    case Type2InstructionKind.And:
                        evalStack.Op_And();
                        break;
                    case Type2InstructionKind.Or:
                        evalStack.Op_Or();
                        break;
                    case Type2InstructionKind.Not:
                        evalStack.Op_Not();
                        break;
                    case Type2InstructionKind.Eq:
                        evalStack.Op_Eq();
                        break;
                    case Type2InstructionKind.Ifelse:
                        evalStack.Op_IfElse();
                        break;
                    case Type2InstructionKind.Rlineto:
                        evalStack.R_LineTo();
                        break;
                    case Type2InstructionKind.Hlineto:
                        evalStack.H_LineTo();
                        break;
                    case Type2InstructionKind.Vlineto:
                        evalStack.V_LineTo();
                        break;
                    case Type2InstructionKind.Rrcurveto:
                        evalStack.RR_CurveTo();
                        break;
                    case Type2InstructionKind.Hhcurveto:
                        evalStack.HH_CurveTo();
                        break;
                    case Type2InstructionKind.Hvcurveto:
                        evalStack.HV_CurveTo();
                        break;
                    case Type2InstructionKind.Rcurveline:
                        evalStack.R_CurveLine();
                        break;
                    case Type2InstructionKind.Rlinecurve:
                        evalStack.R_LineCurve();
                        break;
                    case Type2InstructionKind.Vhcurveto:
                        evalStack.VH_CurveTo();
                        break;
                    case Type2InstructionKind.Vvcurveto:
                        evalStack.VV_CurveTo();
                        break;
                    case Type2InstructionKind.Rmoveto:
                        evalStack.R_MoveTo();
                        break;
                    case Type2InstructionKind.Hmoveto:
                        evalStack.H_MoveTo();
                        break;
                    case Type2InstructionKind.Vmoveto:
                        evalStack.V_MoveTo();
                        break;

                    //-------------------------------------------------------------------
                    // 4.3 Hint Operators
                    case Type2InstructionKind.Hstem:
                        evalStack.H_Stem();
                        break;
                    case Type2InstructionKind.Vstem:
                        evalStack.V_Stem();
                        break;
                    case Type2InstructionKind.Vstemhm:
                        evalStack.V_StemHM();
                        break;
                    case Type2InstructionKind.Hstemhm:
                        evalStack.H_StemHM();
                        break;
                    case Type2InstructionKind.Hintmask1:
                        evalStack.HintMask1(instruction.Value);
                        break;
                    case Type2InstructionKind.Hintmask2:
                        evalStack.HintMask2(instruction.Value);
                        break;
                    case Type2InstructionKind.Hintmask3:
                        evalStack.HintMask3(instruction.Value);
                        break;
                    case Type2InstructionKind.Hintmask4:
                        evalStack.HintMask4(instruction.Value);
                        break;
                    case Type2InstructionKind.Hintmask_bits:
                        evalStack.HintMaskBits(instruction.Value);
                        break;

                    //------------------------------
                    case Type2InstructionKind.Cntrmask1:
                        evalStack.CounterSpaceMask1(instruction.Value);
                        break;
                    case Type2InstructionKind.Cntrmask2:
                        evalStack.CounterSpaceMask2(instruction.Value);
                        break;
                    case Type2InstructionKind.Cntrmask3:
                        evalStack.CounterSpaceMask3(instruction.Value);
                        break;
                    case Type2InstructionKind.Cntrmask4:
                        evalStack.CounterSpaceMask4(instruction.Value);
                        break;
                    case Type2InstructionKind.Cntrmask_bits:
                        evalStack.CounterSpaceMaskBits(instruction.Value);
                        break;

                    //-------------------------
                    // 4.7: Subroutine Operators
                    case Type2InstructionKind.Return:

                        // TODO: I don't think we need to actually track XY values here.
                        // Don't forget to return evalStack's currentX, currentY to prev eval context
                        currentX = evalStack.CurrentX;
                        currentY = evalStack.CurrentY;
                        evalStack.Ret();
                        break;

                    // Should not occur, since we replace this in parsing step
                    case Type2InstructionKind.Callgsubr:
                    case Type2InstructionKind.Callsubr:
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
