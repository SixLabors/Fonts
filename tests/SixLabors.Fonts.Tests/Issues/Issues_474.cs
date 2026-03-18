// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System.Text;

namespace SixLabors.Fonts.Tests.Issues;

public class Issues_474
{
    [Fact]
    public void Test_Issue_474()
    {
        StringBuilder stringBuilder = new();
        stringBuilder.AppendLine("Latin: The quick brown fox jumps over the lazy dog.");
        string text = stringBuilder.ToString();

        Font mainFont = TestFonts.GetFont(TestFonts.ServiceNowWoff2, 30);

        // There are too many metrics to validate here so we just ensure no exceptions are thrown
        // and the rendering looks correct by inspecting the snapshot.
        TextLayoutTestUtilities.TestLayout(
            text,
            new TextOptions(mainFont),
            includeGeometry: false);
    }
}
