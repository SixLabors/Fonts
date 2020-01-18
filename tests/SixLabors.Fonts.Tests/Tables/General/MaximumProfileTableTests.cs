using SixLabors.Fonts.Exceptions;
using SixLabors.Fonts.Tables.General;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class MaximumProfileTableTests
    {
        [Fact]
        public void ShouldThrowExceptionWhenTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using (System.IO.MemoryStream stream = writer.GetStream())
            {
                InvalidFontTableException exception = Assert.Throws<InvalidFontTableException>(() => MaximumProfileTable.Load(new FontReader(stream)));

                Assert.Equal("maxp", exception.Table);
            }
        }
    }
}
