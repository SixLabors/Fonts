// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SixLabors.Fonts.Native;
using Xunit;

namespace SixLabors.Fonts.Tests.Native
{
    public class MacSystemFontsEnumeratorTests
    {
        [Fact]
        public void TestReset()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            using var enumerator = new MacSystemFontsEnumerator();
            var fonts1 = new HashSet<string>(enumerator);
            Assert.NotEmpty(fonts1);

            enumerator.Reset();
            var fonts2 = new HashSet<string>(enumerator);
            Assert.Empty(fonts1.Except(fonts2));
        }
    }
}
