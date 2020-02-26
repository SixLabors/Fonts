using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts;
using Xunit;
using Xunit.Abstractions;

namespace SixLabors.Fonts.Tests
{
    // The fonts which I want to test are preinstalled on Windows, I'm not sure if they are redistributable
    // So instead of putting those font files inside the repository, we test them in SystemFonts.
    public class SystemFontsTests
    {
        private ITestOutputHelper output;

        public SystemFontsTests(ITestOutputHelper outputHelper)
        {
            this.output = outputHelper;
        }

        [Theory]
        [InlineData("simhei", "黑体")] // Test Chinese Sans-Serif font SimHei (黑体)
        public void FindingFontByNamesInMultipleLanguages(params string[] fontFamilyNames)
        {
            IEnumerable<FontFamily> families = fontFamilyNames.Select(fontFamily =>
            {
                bool found = SystemFonts.TryFind(fontFamily, null, out FontFamily family);
                // If a family can be found, then it shouldn't be null
                Assert.Equal(found, family != null);
                return family;
            });

            if (families.All(family => family == null))
            {
                this.output.WriteLine($"Font [{string.Join(" aka. ", fontFamilyNames)}] is not installed on the current machine, test skipped.");
                return;
            }

            // Assert all loaded font families using names in different languages are actually the same
            foreach (FontFamily family in families)
            {
                Assert.Same(family, families.First());
            }
        }
    }
}
