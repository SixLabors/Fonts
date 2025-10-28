// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Runtime.InteropServices;
using SixLabors.Fonts.Native;

namespace SixLabors.Fonts.Tests.Native;

public class MacSystemFontsEnumeratorTests
{
    [Fact]
    public void TestReset()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        using MacSystemFontsEnumerator enumerator = new();
        HashSet<string> fonts1 = new(enumerator);
        Assert.NotEmpty(fonts1);

        enumerator.Reset();
        HashSet<string> fonts2 = new(enumerator);
        Assert.Empty(fonts1.Except(fonts2));
    }
}
