// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    internal readonly struct BidiRun
    {
        public BidiRun(BidiCharacterType direction, int start, int length)
        {
            this.Direction = direction;
            this.Start = start;
            this.Length = length;
        }

        public BidiCharacterType Direction { get; }

        public int Start { get; }

        public int Length { get; }

        public int End => this.Start + this.Length;

        public override string ToString() => $"{this.Start} - {this.End} - {this.Direction}";

        public static IEnumerable<BidiRun> CoalescLevels(ArraySlice<sbyte> levels)
        {
            if (levels.Length == 0)
            {
                yield break;
            }

            int startRun = 0;
            sbyte runLevel = levels[0];
            BidiCharacterType direction;
            for (int i = 1; i < levels.Length; i++)
            {
                if (levels[i] == runLevel)
                {
                    continue;
                }

                // End of this run
                direction = (runLevel & 0x01) == 0 ? BidiCharacterType.L : BidiCharacterType.R;
                yield return new BidiRun(direction, startRun, i - startRun);

                // Move to next run
                startRun = i;
                runLevel = levels[i];
            }

            direction = (runLevel & 0x01) == 0 ? BidiCharacterType.L : BidiCharacterType.R;
            yield return new BidiRun(direction, startRun, levels.Length - startRun);
        }
    }
}
