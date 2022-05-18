// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class CffEvaluationEngine
    {
        private float _scale = 1; // default 
        private readonly Stack<Type2EvaluationStack> evaluationStackPool = new();

        private class PxScaleGlyphTx : IGlyphRenderer
        {
            private readonly float _scale;
            private readonly IGlyphRenderer _tx;
            private bool _is_contour_opened;

            public PxScaleGlyphTx(float scale, IGlyphRenderer tx)
            {
                this._scale = scale;
                this._tx = tx;
            }

            public void BeginRead(int contourCount)
            {
                this._tx.BeginRead(contourCount);
            }

            public void CloseContour()
            {
                this._is_contour_opened = false;
                this._tx.CloseContour();
            }

            public void Curve3(float x1, float y1, float x2, float y2)
            {
                this._is_contour_opened = true;
                this._tx.Curve3(x1 * this._scale, y1 * this._scale, x2 * this._scale, y2 * this._scale);
            }

            public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                this._is_contour_opened = true;
                this._tx.Curve4(x1 * this._scale, y1 * this._scale, x2 * this._scale, y2 * this._scale, x3 * this._scale, y3 * this._scale);
            }

            public void EndRead()
            {
                this._tx.EndRead();
            }

            public void LineTo(float x1, float y1)
            {
                this._is_contour_opened = true;
                this._tx.LineTo(x1 * this._scale, y1 * this._scale);
            }

            public void MoveTo(float x0, float y0)
            {
                this._tx.MoveTo(x0 * this._scale, y0 * this._scale);
            }
            //

            public bool IsContourOpened => this._is_contour_opened;
        }

        public void Run(IGlyphRenderer renderer, Cff1GlyphData glyphData, float scale = 1)
            => this.Run(renderer, glyphData.GlyphInstructions, scale);

        internal void Run(IGlyphRenderer renderer, Type2Instruction[] instructionList, float scale = 1)
        {
            // all fields are set to new values*** 

            this._scale = scale;

            double currentX = 0, currentY = 0;


            var scaleTx = new PxScaleGlyphTx(scale, renderer);
            //
            scaleTx.BeginRead(0);// unknown contour count  
            //
            Run(scaleTx, instructionList, ref currentX, ref currentY);
            //

            //
            // some cff end without closing the latest contour?

            if (scaleTx.IsContourOpened)
            {
                scaleTx.CloseContour();
            }

            scaleTx.EndRead();

        }

        private void Run(IGlyphRenderer renderer, Type2Instruction[] instructionList, ref double currentX, ref double currentY)
        {
            // recursive ***

            Type2EvaluationStack evalStack = this.GetFreeEvalStack(); // **
#if DEBUG
            // evalStack.dbugGlyphIndex = instructionList.dbugGlyphIndex;
#endif
            evalStack._currentX = currentX;
            evalStack._currentY = currentY;
            evalStack.GlyphRenderer = renderer;

            for (int i = 0; i < instructionList.Length; ++i)
            {
                Type2Instruction inst = instructionList[i];

                // ----------
                // this part is our extension to the original
                int mergeFlags = inst.Op >> 6; // upper 2 bits is our extension flags
                switch (mergeFlags)
                {
                    case 0: // nothing
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

                // we use only 6 lower bits for op_name
                switch ((OperatorName)(inst.Op & 0b111111))
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
                    // 
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
                    //-------------------------------------------------------------------                     
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
                    //--------------------------
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

                        // don't forget to return _evalStack's currentX, currentY to prev evl context
                        currentX = evalStack._currentX;
                        currentY = evalStack._currentY;
                        evalStack.Ret();
                        break;

                    // should not occur!-> since we replace this in parsing step
                    case OperatorName.callgsubr:
                    case OperatorName.callsubr:
                        throw new NotSupportedException();
                }
            }

            this.ReleaseEvalStack(evalStack);// ****
        }

        private Type2EvaluationStack GetFreeEvalStack()
        {
            if (this.evaluationStackPool.Count > 0)
            {
                return this.evaluationStackPool.Pop();
            }
            else
            {
                return new Type2EvaluationStack();
            }
        }

        private void ReleaseEvalStack(Type2EvaluationStack evalStack)
        {
            evalStack.Reset();
            this.evaluationStackPool.Push(evalStack);
        }
    }
}
