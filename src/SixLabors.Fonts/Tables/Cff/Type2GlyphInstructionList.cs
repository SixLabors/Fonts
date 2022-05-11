// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Tables.Cff
{
    internal class Type2GlyphInstructionList
    {
        List<Type2Instruction> _insts;

        public Type2GlyphInstructionList()
        {
            _insts = new List<Type2Instruction>();
        }

        public Type2Instruction RemoveLast()
        {
            int last = _insts.Count - 1;
            Type2Instruction _lastInst = _insts[last];
            _insts.RemoveAt(last);
            return _lastInst;
        }
        //
        public void AddInt(int intValue)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(OperatorName.LoadInt, intValue));
        }
        public void AddFloat(int float1616Fmt)
        {
#if DEBUG
            debugCheck();
            //var test = new Type2Instruction(OperatorName.LoadFloat, float1616Fmt);
            //string str = test.ToString();
#endif 
            _insts.Add(new Type2Instruction(OperatorName.LoadFloat, float1616Fmt));
        }
        public void AddOp(OperatorName opName)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(opName));
        }

        public void AddOp(OperatorName opName, int value)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(opName, value));
        }
        public int Count => _insts.Count;
        internal void ChangeFirstInstToGlyphWidthValue()
        {
            //check the first element must be loadint
            if (_insts.Count == 0)
                return;

            Type2Instruction firstInst = _insts[0];
            if (!firstInst.IsLoadInt)
            { throw new NotSupportedException(); }
            //the replace
            _insts[0] = new Type2Instruction(OperatorName.GlyphWidth, firstInst.Value);
        }




        internal List<Type2Instruction> InnerInsts => _insts;

#if DEBUG
        void debugCheck()
        {
            if (_dbugMark == 5 && _insts.Count > 50)
            {

            }
        }
        public int dbugInstCount => _insts.Count;
        int _dbugMark;

        public ushort dbugGlyphIndex;

        public int dbugMark
        {
            get => _dbugMark;
            set
            {
                _dbugMark = value;
            }
        }

        public void dbugDumpInstructionListToFile(string filename)
        {
            //dbugCffInstHelper.dbugDumpInstructionListToFile(_insts, filename);
        }
#endif
    }
}
