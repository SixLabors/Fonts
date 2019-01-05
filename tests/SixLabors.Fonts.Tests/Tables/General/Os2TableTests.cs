using SixLabors.Fonts.Tables.General;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class OS2TableTests
    {
        [Fact]
        public void ShouldReturnNullWhenTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using (var stream = writer.GetStream())
            {
                Assert.Null(OS2Table.Load(new FontReader(stream)));
            }
        }
    }
}
