// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Type2GlyphInstructionList
    {
        public Type2GlyphInstructionList() => this.Instructions = new List<Type2Instruction>();

        public int Count => this.Instructions.Count;

        public List<Type2Instruction> Instructions { get; }

        public int DbugInstCount => this.Instructions.Count;

        public ushort DbugGlyphIndex { get; set; }

        public int DbugMark { get; set; }

        public Type2Instruction RemoveLast()
        {
            int last = this.Instructions.Count - 1;
            Type2Instruction lastInstructions = this.Instructions[last];
            this.Instructions.RemoveAt(last);
            return lastInstructions;
        }

        public void AddInt(int intValue)
        {
#if DEBUG
            this.DebugCheck();
#endif
            this.Instructions.Add(new Type2Instruction(OperatorName.LoadInt, intValue));
        }

        public void AddFloat(int float1616Fmt)
        {
#if DEBUG
            this.DebugCheck();

            // var test = new Type2Instruction(OperatorName.LoadFloat, float1616Fmt);
            // string str = test.ToString();
#endif
            this.Instructions.Add(new Type2Instruction(OperatorName.LoadFloat, float1616Fmt));
        }

        public void AddOp(OperatorName opName)
        {
#if DEBUG
            this.DebugCheck();
#endif
            this.Instructions.Add(new Type2Instruction(opName));
        }

        public void AddOp(OperatorName opName, int value)
        {
#if DEBUG
            this.DebugCheck();
#endif
            this.Instructions.Add(new Type2Instruction(opName, value));
        }

        internal void ChangeFirstInstToGlyphWidthValue()
        {
            // check the first element must be loadint
            if (this.Instructions.Count == 0)
            {
                return;
            }

            Type2Instruction firstInst = this.Instructions[0];
            if (!firstInst.IsLoadInt)
            {
                throw new NotSupportedException();
            }

            // then replace
            this.Instructions[0] = new Type2Instruction(OperatorName.GlyphWidth, firstInst.Value);
        }

#if DEBUG
        private void DebugCheck()
        {
            if (this.DbugMark == 5 && this.Instructions.Count > 50)
            {
            }
        }
#endif
    }
}
