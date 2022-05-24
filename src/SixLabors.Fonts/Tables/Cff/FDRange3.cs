// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

namespace SixLabors.Fonts.Tables.Cff
{
    internal readonly struct FDRange3
    {
        public readonly ushort first;
        public readonly byte fd;

        public FDRange3(ushort first, byte fd)
        {
            this.first = first;
            this.fd = fd;
        }

#if DEBUG
        public override string ToString() => "first:" + this.first + ",fd:" + this.fd;
#endif
    }
}
