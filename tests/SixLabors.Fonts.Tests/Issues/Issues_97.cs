using System.IO;
using Xunit;

namespace SixLabors.Fonts.Tests.Issues
{
    public class Issues_97
    {
        [Fact]
        public void ShouldNotThrowNullReferenceExceptionWhenReaderCannotBeCreatedForTable()
        {
            FontDescription.LoadDescription(TestFonts.Issues.Issue97File);
        }
    }
}
