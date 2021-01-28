// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Text;

namespace SixLabors.Fonts.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm. UAX:14
    /// <see href="https://www.unicode.org/reports/tr14/tr14-37.html"/>
    /// </summary>
    internal class LineBreakAlgorithm
    {
        private ReadOnlyMemory<int> codePoints = Array.Empty<int>();
        private bool first = true;
        private int position;
        private int lastPosition;
        private LineBreakClass currentClass;
        private LineBreakClass nextClass;
        private bool lb8a;
        private bool lb21a;
        private int lb30a;

        /// <summary>
        /// Reset this line breaker.
        /// </summary>
        /// <param name="value">The string to be broken.</param>
        public void Reset(string value) => this.Reset(ToUtf32(value.AsSpan()));

        /// <summary>
        /// Reset this line breaker.
        /// </summary>
        /// <param name="codePoints">The code points of the string to be broken.</param>
        public void Reset(ReadOnlyMemory<int> codePoints)
        {
            this.codePoints = codePoints;
            this.first = true;
            this.position = 0;
            this.lastPosition = 0;
            this.lb8a = false;
            this.lb21a = false;
            this.lb30a = 0;
        }

        /// <summary>
        /// Enumerates all line breaks returning the result.
        /// </summary>
        /// <returns>The <see cref="List{LineBreak}"/>.</returns>
        public List<LineBreak> GetBreaks(bool mandatoryOnly = false)
        {
            var list = new List<LineBreak>();
            if (mandatoryOnly)
            {
                list.AddRange(this.FindMandatoryBreaks());
            }
            else
            {
                while (this.TryGetNextBreak(out LineBreak lb))
                {
                    list.Add(lb);
                }
            }

            return list;
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
            => this.MapClass(UnicodeData.GetLineBreakClass(this.codePoints.Span[this.position++]));

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

        /// <summary>
        /// Returns the line break from the current code points if one is found.
        /// </summary>
        /// <param name="lineBreak">
        /// When this method returns, contains the value associate with the break;
        /// otherwise, the default value.
        /// This parameter is passed uninitialized.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool TryGetNextBreak(out LineBreak lineBreak)
        {
            // get the first char if we're at the beginning of the string
            if (this.first)
            {
                this.first = false;
                LineBreakClass firstClass = this.NextCharClass();
                this.currentClass = this.MapFirst(firstClass);
                this.nextClass = firstClass;
                this.lb8a = firstClass == LineBreakClass.ZWJ;
                this.lb30a = 0;
            }

            while (this.position < this.codePoints.Length)
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

                bool? shouldBreak = this.GetSimpleBreak();

                if (!shouldBreak.HasValue)
                {
                    shouldBreak = this.GetPairTableBreak(lastClass);
                }

                // Rule LB8a
                this.lb8a = this.nextClass == LineBreakClass.ZWJ;

                if (shouldBreak.Value)
                {
                    lineBreak = new LineBreak(this.FindPriorNonWhitespace(this.lastPosition), this.lastPosition, false);
                    return true;
                }
            }

            if (this.lastPosition < this.codePoints.Length)
            {
                this.lastPosition = this.codePoints.Length;
                bool required = (this.currentClass == LineBreakClass.BK) || ((this.currentClass == LineBreakClass.CR) && (this.nextClass != LineBreakClass.LF));
                lineBreak = new LineBreak(this.FindPriorNonWhitespace(this.codePoints.Length), this.lastPosition, required);
                return true;
            }
            else
            {
                lineBreak = default;
                return false;
            }
        }

        /// <summary>
        /// Finds all mandatory breaks within the current codepoints.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{LineBreak}"/>.</returns>
        public IEnumerable<LineBreak> FindMandatoryBreaks()
        {
            for (int i = 0; i < this.codePoints.Span.Length; i++)
            {
                switch (UnicodeData.GetLineBreakClass(this.codePoints.Span[i]))
                {
                    case LineBreakClass.BK:
                        yield return new LineBreak(i, i + 1, true);
                        break;

                    case LineBreakClass.CR:
                        if (i + 1 < this.codePoints.Length && UnicodeData.GetLineBreakClass(this.codePoints.Span[i + 1]) == LineBreakClass.LF)
                        {
                            yield return new LineBreak(i, i + 2, true);
                        }
                        else
                        {
                            yield return new LineBreak(i, i + 1, true);
                        }

                        break;

                    case LineBreakClass.LF:
                        yield return new LineBreak(i, i + 1, true);
                        break;
                }
            }
        }

        private int FindPriorNonWhitespace(int from)
        {
            ReadOnlySpan<int> codePointSpan = this.codePoints.Span;
            if (from > 0)
            {
                LineBreakClass cls = UnicodeData.GetLineBreakClass(codePointSpan[from - 1]);
                if (cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR)
                {
                    from--;
                }
            }

            while (from > 0)
            {
                LineBreakClass cls = UnicodeData.GetLineBreakClass(codePointSpan[from - 1]);
                if (cls == LineBreakClass.SP)
                {
                    from--;
                }
                else
                {
                    break;
                }
            }

            return from;
        }

        public static Memory<int> ToUtf32(ReadOnlySpan<char> text)
        {
            unsafe
            {
                fixed (char* pstr = text)
                {
                    // Get required byte count
                    int byteCount = Encoding.UTF32.GetByteCount(pstr, text.Length);

                    // Allocate buffer
                    int[] utf32 = new int[byteCount / sizeof(int)];
                    fixed (int* putf32 = utf32)
                    {
                        // Convert
                        Encoding.UTF32.GetBytes(pstr, text.Length, (byte*)putf32, byteCount);

                        // Done
                        return utf32;
                    }
                }
            }
        }
    }
}
