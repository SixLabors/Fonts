// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Type2GlyphInstructionCollection
    {
        private readonly List<Type2Instruction> instructions;

        public Type2GlyphInstructionCollection()
            => this.instructions = new List<Type2Instruction>();

        public int Count => this.instructions.Count;

        public Type2Instruction RemoveLast()
        {
            int last = this.instructions.Count - 1;
            Type2Instruction lastInstructions = this.instructions[last];
            this.instructions.RemoveAt(last);
            return lastInstructions;
        }

        public void AddInt(int value)
            => this.instructions.Add(new Type2Instruction(Type2InstructionKind.LoadInt, value));

        public void AddFloatFixed1616(int value)
            => this.instructions.Add(new Type2Instruction(Type2InstructionKind.LoadFloat, value));

        public void AddOperator(Type2InstructionKind opName)
            => this.instructions.Add(new Type2Instruction(opName));

        public void AddOperator(Type2InstructionKind opName, int value)
            => this.instructions.Add(new Type2Instruction(opName, value));

        internal void ChangeFirstInstToGlyphWidthValue()
        {
            // Check the first element must be loadint
            if (this.instructions.Count == 0)
            {
                return;
            }

            Type2Instruction instruction = this.instructions[0];
            if (!instruction.IsLoadInt)
            {
                throw new NotSupportedException();
            }

            this.instructions[0] = new Type2Instruction(Type2InstructionKind.GlyphWidth, instruction.Value);
        }

        public Type2Instruction[] ToArray() => this.instructions.ToArray();
    }
}
