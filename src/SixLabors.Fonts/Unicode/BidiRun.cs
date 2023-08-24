// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Collections.Generic;

namespace SixLabors.Fonts.Unicode
{
    internal readonly struct BidiRun : IEquatable<BidiRun>
    {
        public BidiRun(BidiCharacterType direction, int level, int start, int length)
        {
            this.Direction = direction;
            this.Level = level;
            this.Start = start;
            this.Length = length;
        }

        public BidiCharacterType Direction { get; }

        public int Level { get; }

        public int Start { get; }

        public int Length { get; }

        public int End => this.Start + this.Length;

        public static bool operator ==(BidiRun left, BidiRun right) => left.Equals(right);

        public static bool operator !=(BidiRun left, BidiRun right) => !(left == right);

        public override string ToString() => $"{this.Start} - {this.End} - {this.Direction}";

        public static IEnumerable<BidiRun> CoalesceLevels(ReadOnlyArraySlice<sbyte> levels)
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
                direction = (runLevel & 0x01) == 0 ? BidiCharacterType.LeftToRight : BidiCharacterType.RightToLeft;
                yield return new BidiRun(direction, runLevel, startRun, i - startRun);

                // Move to next run
                startRun = i;
                runLevel = levels[i];
            }

            direction = (runLevel & 0x01) == 0 ? BidiCharacterType.LeftToRight : BidiCharacterType.RightToLeft;
            yield return new BidiRun(direction, runLevel, startRun, levels.Length - startRun);
        }

        public override bool Equals(object? obj)
            => obj is BidiRun run && this.Equals(run);

        public bool Equals(BidiRun other)
            => this.Direction == other.Direction
            && this.Level == other.Level
            && this.Start == other.Start
            && this.Length == other.Length
            && this.End == other.End;

        public override int GetHashCode()
            => HashCode.Combine(this.Direction, this.Level, this.Start, this.Length, this.End);
    }
}
