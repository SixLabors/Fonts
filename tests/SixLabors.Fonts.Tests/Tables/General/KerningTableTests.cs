using SixLabors.Fonts.Tables.General;
using Xunit;

namespace SixLabors.Fonts.Tests.Tables.General
{
    public class KerningTableTests
    {
        [Fact]
        public void ShouldReturnDefaultValueWhenTableCouldNotBeFound()
        {
            var writer = new BinaryWriter();
            writer.WriteTrueTypeFileHeader();

            using (var stream = writer.GetStream())
            {
                var table = KerningTable.Load(new FontReader(stream));
                Assert.NotNull(table);
            }
        }
    }
}
