// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm. UAX:14
    /// <see href="https://www.unicode.org/reports/tr14/tr14-37.html"/>
    /// </summary>
    internal ref struct LineBreakAlgorithm
    {
        private readonly ReadOnlySpan<char> source;
        private int charPosition;
        private readonly int pointsLength;
        private int position;
        private int lastPosition;
        private LineBreakClass currentClass;
        private LineBreakClass nextClass;
        private bool first;
        private bool lb8a;
        private bool lb21a;
        private int lb30a;

        public LineBreakAlgorithm(ReadOnlySpan<char> source)
            : this()
        {
            this.source = source;
            this.pointsLength = CodePoint.GetCodePointCount(source);
            this.charPosition = 0;
            this.position = 0;
            this.lastPosition = 0;
            this.currentClass = LineBreakClass.XX;
            this.nextClass = LineBreakClass.XX;
            this.first = true;
            this.lb8a = false;
            this.lb21a = false;
            this.lb30a = 0;
        }

        /// <summary>
        /// Returns the line break from the current source if one is found.
        /// </summary>
        /// <param name="lineBreak">
        /// When this method returns, contains the value associate with the break;
        /// otherwise, the default value.
        /// This parameter is passed uninitialized.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryGetNextBreak(out LineBreak lineBreak)
        {
            // Get the first char if we're at the beginning of the string.
            if (this.first)
            {
                this.first = false;
                LineBreakClass firstClass = this.NextCharClass();
                this.currentClass = this.MapFirst(firstClass);
                this.nextClass = firstClass;
                this.lb8a = firstClass == LineBreakClass.ZWJ;
                this.lb30a = 0;
            }

            while (this.position < this.pointsLength)
            {
                this.lastPosition = this.position;
                LineBreakClass lastClass = this.nextClass;
                this.nextClass = this.NextCharClass();

                // explicit newline
                if ((this.currentClass == LineBreakClass.BK) || ((this.currentClass == LineBreakClass.CR) && (this.nextClass != LineBreakClass.LF)))
                {
                    this.currentClass = this.MapFirst(this.MapClass(this.nextClass));
                    lineBreak = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, true);
                    return true;
                }

                bool? shouldBreak = this.GetSimpleBreak() ?? (bool?)this.GetPairTableBreak(lastClass);

                // Rule LB8a
                this.lb8a = this.nextClass == LineBreakClass.ZWJ;

                if (shouldBreak.Value)
                {
                    lineBreak = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, false);
                    return true;
                }
            }

            if (this.lastPosition < this.pointsLength)
            {
                this.lastPosition = this.pointsLength;
                bool required = (this.currentClass == LineBreakClass.BK) || ((this.currentClass == LineBreakClass.CR) && (this.nextClass != LineBreakClass.LF));
                lineBreak = new LineBreak(this.FindPriorNonWhitespace(this.pointsLength), this.lastPosition, required);
                return true;
            }
            else
            {
                lineBreak = default;
                return false;
            }
        }

        private LineBreakClass MapClass(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.AI:
                case LineBreakClass.SA:
                case LineBreakClass.SG:
                case LineBreakClass.XX:
                    return LineBreakClass.AL;

                case LineBreakClass.CJ:
                    return LineBreakClass.NS;

                default:
                    return c;
            }
        }

        private LineBreakClass MapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    return LineBreakClass.BK;

                case LineBreakClass.SP:
                    return LineBreakClass.WJ;

                default:
                    return c;
            }
        }

        // Get the next character class
        private LineBreakClass NextCharClass()
        {
            var cp = CodePoint.DecodeFromUtf16At(this.source, this.charPosition, out int count);
            this.charPosition += count;
            this.position++;

            return this.MapClass(CodePoint.GetLineBreakClass(cp));
        }

        private bool? GetSimpleBreak()
        {
            // handle classes not handled by the pair table
            switch (this.nextClass)
            {
                case LineBreakClass.SP:
                    return false;

                case LineBreakClass.BK:
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    this.currentClass = LineBreakClass.BK;
                    return false;

                case LineBreakClass.CR:
                    this.currentClass = LineBreakClass.CR;
                    return false;
            }

            return null;
        }

        private bool GetPairTableBreak(LineBreakClass lastClass)
        {
            // if not handled already, use the pair table
            bool shouldBreak = false;
            switch (LineBreakPairTable.Table[(int)this.currentClass][(int)this.nextClass])
            {
                case LineBreakPairTable.DIBRK: // Direct break
                    shouldBreak = true;
                    break;

                case LineBreakPairTable.INBRK: // possible indirect break
                    shouldBreak = lastClass == LineBreakClass.SP;
                    break;

                case LineBreakPairTable.CIBRK:
                    shouldBreak = lastClass == LineBreakClass.SP;
                    if (!shouldBreak)
                    {
                        return false;
                    }

                    break;

                case LineBreakPairTable.CPBRK: // prohibited for combining marks
                    if (lastClass != LineBreakClass.SP)
                    {
                        return shouldBreak;
                    }

                    break;

                case LineBreakPairTable.PRBRK:
                    break;
            }

            if (this.lb8a)
            {
                shouldBreak = false;
            }

            // Rule LB21a
            if (this.lb21a && (this.currentClass == LineBreakClass.HY || this.currentClass == LineBreakClass.BA))
            {
                shouldBreak = false;
                this.lb21a = false;
            }
            else
            {
                this.lb21a = this.currentClass == LineBreakClass.HL;
            }

            // Rule LB30a
            if (this.currentClass == LineBreakClass.RI)
            {
                this.lb30a++;
                if (this.lb30a == 2 && (this.nextClass == LineBreakClass.RI))
                {
                    shouldBreak = true;
                    this.lb30a = 0;
                }
            }
            else
            {
                this.lb30a = 0;
            }

            this.currentClass = this.nextClass;

            return shouldBreak;
        }

        private int FindPriorNonWhitespace(int from)
        {
            if (from > 0)
            {
                var cp = CodePoint.DecodeFromUtf16At(this.source, from - 1, out int count);
                LineBreakClass cls = CodePoint.GetLineBreakClass(cp);

                if (cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR)
                {
                    from -= count;
                }
            }

            while (from > 0)
            {
                var cp = CodePoint.DecodeFromUtf16At(this.source, from - 1, out int count);
                LineBreakClass cls = CodePoint.GetLineBreakClass(cp);

                if (cls == LineBreakClass.SP)
                {
                    from -= count;
                }
                else
                {
                    break;
                }
            }

            return from;
        }
    }
}
