// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    // TODO: We might actually be able to dump this whole class. See the comments in the caller.
    internal class Type2InstructionCompacter
    {
        // This is our extension
        //-----------------------
        private List<Type2Instruction> step1List = new();
        private List<Type2Instruction> step2List = new();

        private void CompactStep1OnlyLoadInt(List<Type2Instruction> insts)
        {
            int j = insts.Count;
            CompactRange latestCompactRange = CompactRange.None;
            int start = -1;
            int count = 0;
            void FlushWaitingNumbers()
            {
                // Nested method
                // flush waiting integer
                if (latestCompactRange == CompactRange.Short)
                {
                    switch (count)
                    {
                        default:
                            throw new NotSupportedException();
                        case 0:
                            break; // nothing
                        case 2:
                            this.step1List.Add(
                                new Type2Instruction(OperatorName.LoadShort2, (((ushort)insts[start].Value) << 16) | ((ushort)insts[start + 1].Value)));
                            start += 2;
                            count -= 2;
                            break;
                        case 1:
                            this.step1List.Add(insts[start]);
                            start += 1;
                            count -= 1;
                            break;
                    }
                }
                else
                {
                    switch (count)
                    {
                        default:
                            throw new NotSupportedException();
                        case 0:
                            break; // nothing
                        case 4:

                            this.step1List.Add(
                                new Type2Instruction(
                                    OperatorName.LoadSbyte4,
                                    (((byte)insts[start].Value) << 24) | (((byte)insts[start + 1].Value) << 16) | (((byte)insts[start + 2].Value) << 8) | (((byte)insts[start + 3].Value) << 0)));
                            start += 4;
                            count -= 4;
                            break;
                        case 3:
                            this.step1List.Add(new Type2Instruction(
                                OperatorName.LoadSbyte3,
                                (((byte)insts[start].Value) << 24) | (((byte)insts[start + 1].Value) << 16) | (((byte)insts[start + 2].Value) << 8)));
                            start += 3;
                            count -= 3;
                            break;
                        case 2:
                            this.step1List.Add(new Type2Instruction(
                                OperatorName.LoadShort2,
                                (((ushort)insts[start].Value) << 16) | ((ushort)insts[start + 1].Value)));
                            start += 2;
                            count -= 2;
                            break;
                        case 1:
                            this.step1List.Add(insts[start]);
                            start += 1;
                            count -= 1;
                            break;
                    }
                }

                start = -1;
                count = 0;
            }

            for (int i = 0; i < j; ++i)
            {
                Type2Instruction inst = insts[i];
                if (inst.IsLoadInt)
                {
                    // check waiting data in queue
                    // get compact range
                    CompactRange c1 = GetCompactRange(inst.Value);
                    switch (c1)
                    {
                        default:
                            throw new NotSupportedException();
                        case CompactRange.None:
                        {
                            if (count > 0)
                            {
                                FlushWaitingNumbers();
                            }

                            this.step1List.Add(inst);
                            latestCompactRange = CompactRange.None;
                            break;
                        }

                        case CompactRange.SByte:
                        {
                            if (latestCompactRange == CompactRange.Short)
                            {
                                FlushWaitingNumbers();
                                latestCompactRange = CompactRange.SByte;
                            }

                            switch (count)
                            {
                                default:
                                    throw new NotSupportedException();
                                case 0:
                                    start = i;
                                    latestCompactRange = CompactRange.SByte;
                                    break;
                                case 1:
                                    break;
                                case 2:
                                    break;
                                case 3:
                                    // we already have 3 bytes
                                    // so this is 4th byte
                                    count++;
                                    FlushWaitingNumbers();
                                    continue;
                            }

                            count++;
                            break;
                        }

                        case CompactRange.Short:
                        {
                            if (latestCompactRange == CompactRange.SByte)
                            {
                                FlushWaitingNumbers();
                                latestCompactRange = CompactRange.Short;
                            }

                            switch (count)
                            {
                                default:
                                    throw new NotSupportedException();
                                case 0:
                                    start = i;
                                    latestCompactRange = CompactRange.Short;
                                    break;
                                case 1:
                                    // we already have 1 so this is 2nd
                                    count++;
                                    FlushWaitingNumbers();
                                    continue;
                            }

                            count++;
                            break;
                        }
                    }
                }
                else
                {
                    // other cmds
                    // flush waiting cmd
                    if (count > 0)
                    {
                        FlushWaitingNumbers();
                    }

                    this.step1List.Add(inst);
                    latestCompactRange = CompactRange.None;
                }
            }
        }

        private static byte IsLoadIntOrMergeableLoadIntExtension(OperatorName opName) => opName switch
        {
            // case OperatorName.LoadSbyte3://except LoadSbyte3 ***
            // merge-able
            OperatorName.LoadInt => 1,

            // merge-able
            OperatorName.LoadShort2 => 2,

            // merge-able
            OperatorName.LoadSbyte4 => 3,
            _ => 0,
        };

        private void CompactStep2MergeLoadIntWithNextCommand()
        {
            // a second pass
            // check if we can merge some load int( LoadInt, LoadSByte4, LoadShort2) except LoadSByte3
            // to next instruction command or not
            int j = this.step1List.Count;
            for (int i = 0; i < j; ++i)
            {
                Type2Instruction i0 = this.step1List[i];

                if (i + 1 < j)
                {
                    // has next cmd
                    byte merge_flags = IsLoadIntOrMergeableLoadIntExtension((OperatorName)i0.Op);
                    if (merge_flags > 0)
                    {
                        Type2Instruction i1 = this.step1List[i + 1];

                        // check i1 has empty space for i0 or not
                        bool canbe_merged = false;
                        switch ((OperatorName)i1.Op)
                        {
                            case OperatorName.LoadInt:
                            case OperatorName.LoadShort2:
                            case OperatorName.LoadSbyte4:
                            case OperatorName.LoadSbyte3:
                            case OperatorName.LoadFloat:

                            case OperatorName.Hintmask1:
                            case OperatorName.Hintmask2:
                            case OperatorName.Hintmask3:
                            case OperatorName.Hintmask4:
                            case OperatorName.Hintmask_bits:
                            case OperatorName.Cntrmask1:
                            case OperatorName.Cntrmask2:
                            case OperatorName.Cntrmask3:
                            case OperatorName.Cntrmask4:
                            case OperatorName.Cntrmask_bits:
                                break;
                            default:
                                canbe_merged = true;
                                break;
                        }

                        if (canbe_merged)
                        {
#if DEBUG
                            if (merge_flags > 3)
                            {
                                throw new NotSupportedException();
                            }
#endif

                            this.step2List.Add(new Type2Instruction((byte)((merge_flags << 6) | i1.Op), i0.Value));
                            i += 1;
                        }
                        else
                        {
                            this.step2List.Add(i0);
                        }
                    }
                    else
                    {
                        // this is the last one
                        this.step2List.Add(i0);
                    }
                }
                else
                {
                    // this is the last one
                    this.step2List.Add(i0);
                }
            }
        }

        public Type2Instruction[] Compact(List<Type2Instruction> insts)
        {
            // for simpicity
            // we have 2 passes
            // 1. compact consecutive numbers
            // 2. compact other cmd

            // reset
            this.step1List.Clear();
            this.step2List.Clear();
            this.CompactStep1OnlyLoadInt(insts);
            this.CompactStep2MergeLoadIntWithNextCommand();
#if DEBUG

            // you can check/compare the compact form and the original form
            this.DbugReExpandAndCompare_ForStep1(this.step1List, insts);
            this.DbugReExpandAndCompare_ForStep2(this.step2List, insts);
#endif
            return this.step2List.ToArray();

            // return _step1List.ToArray();
        }

#if DEBUG
        private void DbugReExpandAndCompare_ForStep1(List<Type2Instruction> step1, List<Type2Instruction> org)
        {
            List<Type2Instruction> expand1 = new(org.Count);
            {
                int j = step1.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst = step1[i];
                    switch ((OperatorName)inst.Op)
                    {
                        case OperatorName.LoadSbyte4:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)inst.Value));
                            break;
                        case OperatorName.LoadSbyte3:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            break;
                        case OperatorName.LoadShort2:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (short)inst.Value));
                            break;
                        default:
                            expand1.Add(inst);
                            break;
                    }
                }
            }

            //--------------------------------------------
            if (expand1.Count != org.Count)
            {
                // ERR=> then find first diff
                int min = Math.Min(expand1.Count, org.Count);
                for (int i = 0; i < min; ++i)
                {
                    Type2Instruction inst_exp = expand1[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
            else
            {
                // compare command-by-command
                int j = step1.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst_exp = expand1[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }

        private void DbugReExpandAndCompare_ForStep2(List<Type2Instruction> step2, List<Type2Instruction> org)
        {
            List<Type2Instruction> expand2 = new(org.Count);
            {
                int j = step2.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst = step2[i];

                    // we use upper 2 bits to indicate that this is merged cmd or not
                    byte merge_flags = (byte)(inst.Op >> 6);

                    // lower 6 bits is actual cmd
                    var onlyOpName = (OperatorName)(inst.Op & 0b111111);
                    switch (onlyOpName)
                    {
                        case OperatorName.LoadSbyte4:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)inst.Value));
                            break;
                        case OperatorName.LoadSbyte3:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            break;
                        case OperatorName.LoadShort2:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)inst.Value));
                            break;
                        default:
                        {
                            switch (merge_flags)
                            {
                                case 0:
                                    expand2.Add(inst);
                                    break;
                                case 1:
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, inst.Value));
                                    expand2.Add(new Type2Instruction(onlyOpName, 0));
                                    break;
                                case 2:
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)inst.Value));
                                    expand2.Add(new Type2Instruction(onlyOpName, 0));
                                    break;
                                case 3:
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                                    expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)inst.Value));
                                    expand2.Add(new Type2Instruction(onlyOpName, 0));
                                    break;
                            }

                            break;
                        }
                    }
                }
            }

            //--------------------------------------------
            if (expand2.Count != org.Count)
            {
                throw new NotSupportedException();
            }
            else
            {
                // compare command-by-command
                int j = step2.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst_exp = expand2[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
#endif

#pragma warning disable SA1201 // Elements should appear in the correct order
        private enum CompactRange
#pragma warning restore SA1201 // Elements should appear in the correct order
        {
            None,
            SByte,
            Short,
        }

        private static CompactRange GetCompactRange(int value)
        {
            if (value is > sbyte.MinValue and < sbyte.MaxValue)
            {
                return CompactRange.SByte;
            }
            else if (value is > short.MinValue and < short.MaxValue)
            {
                return CompactRange.Short;
            }
            else
            {
                return CompactRange.None;
            }
        }
    }
}
