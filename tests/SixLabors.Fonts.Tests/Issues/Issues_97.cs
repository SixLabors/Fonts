using SixLabors.Fonts.Exceptions;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_97
    {
        [Fact]
        public void ShouldNotThrowNullReferenceExceptionWhenReaderCannotBeCreatedForTable()
        {
            Assert.Throws<InvalidFontTableException>(() => FontDescription.LoadDescription(TestFonts.Issues.Issue97File));
        }
    }
}
