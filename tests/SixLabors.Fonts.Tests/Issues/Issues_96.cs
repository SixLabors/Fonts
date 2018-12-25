using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_96
    {
        [Fact]
        public void ShouldNotThrowExceptionWhenFontContainsDuplicateTables()
        {
            FontDescription.LoadDescription(TestFonts.Issues.Issue96File);
        }
    }
}
